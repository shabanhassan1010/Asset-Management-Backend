#region
using Asset.Application.Common;
using Asset.Application.Common.Responses;
using Asset.Application.Features.AI.Enums;
using Asset.Application.Features.AI.Interfases;
using Asset.Application.Features.Assets.DTOs;
using Asset.Domain.Enum;
using AssetEntity = Asset.Domain.Models.Asset;
using MediatR;
using Asset.Application.Resources;
using Microsoft.Extensions.Localization;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Features.AI.Builders;
using Asset.Application.Features.AI.IService;
using Asset.Application.Features.AI.Queries.QueryResponse;
using Asset.Application.Features.AI.Queries.QueryModel;
using Asset.Application.Features.AI.DTos;
#endregion

namespace Asset.Application.Features.AI.Queries.QueryHandler
{
    // The flow of one question:
    //   1. load the chat history          (so the question can refer to anything said before)
    //   2. AI understands the question    (question + history -> ParsedAssetQuestion)
    //   3. CODE checks permissions and queries the database
    //   4. AI writes the reply            (only from the rows this user is allowed to see)
    //   5. save the turn to the history
    public class AskAssetQuestionQueryHandler : IRequestHandler<AskAssetQuestionQuery, ApiResponse<AssetQuestionResponse>>
    {
        #region Fields
        private readonly IAssetQuestionParserService _parser;
        private readonly IAssetAnswerWriterService _answerWriter;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IConversationStoreService _conversationStore;
        private readonly IStringLocalizer<SharedResources> _localizer;   // template answers in the request's language
        private const int MaxRows = 20;
        #endregion

        #region Constructor
        public AskAssetQuestionQueryHandler(IAssetQuestionParserService parser,
                                            IAssetAnswerWriterService answerWriter,
                                            IUnitOfWork unitOfWork,
                                            ICurrentUserService currentUser,
                                            IConversationStoreService conversationStore,
                                            IStringLocalizer<SharedResources> localizer)
        {
            _localizer = localizer;
            _parser = parser;
            _answerWriter = answerWriter;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _conversationStore = conversationStore;
        }
        #endregion

        #region Handle
        public async Task<ApiResponse<AssetQuestionResponse>> Handle(AskAssetQuestionQuery request, CancellationToken cancellationToken)
        {
            // One chat = one user + one session id. The user id is part of the key, so nobody can
            // read someone else's chat by sending their session id.
            var chatKey = $"{_currentUser.UserId}:{request.SessionId}";

            // The whole chat so far (not only the last question).
            var history = await _conversationStore.GetHistoryAsync(chatKey, cancellationToken);
            var previous = await _conversationStore.GetLastAsync(chatKey, cancellationToken);

            var parsed = await _parser.ParseAsync(request.Question, history, previous, cancellationToken);

            if (parsed.Intent == AssetQuestionIntent.Greeting)
            {
                return await GenerateReplyAsync(request, chatKey, history,
                                                AssetAnswerBuilder.Greeting(_localizer, request.Question.ToLowerInvariant()),
                                                cancellationToken,
                                                suggestions: AssetAnswerBuilder.StarterQuestions(_localizer));
            }

            if (parsed.Intent == AssetQuestionIntent.Unsupported)
            {
                return await GenerateReplyAsync(request, chatKey, history,
                                                AssetAnswerBuilder.OutOfScope(_localizer),
                                                cancellationToken,
                                                suggestions: AssetAnswerBuilder.StarterQuestions(_localizer));
            }

            // "Show me all assets" / "how many assets do we have?" with no filter are fair questions.
            // An admin gets the whole company (the list is still capped at MaxRows); anyone else gets only their own assets.
            if (!parsed.HasAnyFilter && !_currentUser.IsAdmin)
            {
                if (_currentUser.EmployeeId is null)
                    return await ReplyAsync(request, chatKey, AssetAnswerBuilder.NoEmployeeLink(_localizer));

                parsed = parsed with { IsAboutSelf = true };
            }

            var filter = new AssetFilter
            {
                Page = 1,
                PageSize = parsed.Intent == AssetQuestionIntent.CountAssets ? 1 : MaxRows,   // a count only needs TotalCount
                Manufacturer = parsed.Manufacturer,
                StatusId = parsed.Status.HasValue ? (byte)parsed.Status.Value : null,
                IncludeRetired = parsed.Status == AssetStatus.Retired
            };

            // Set only when the question names a specific employee.
            // Declared here (not inside the if) because it is used after the query.
            string? employeeFullName = null;

            if (parsed.AssetTypeName is not null)
            {
                var assetTypeId = await _unitOfWork.AiLookup.GetAssetTypeIdByNameAsync(parsed.AssetTypeName, cancellationToken);

                if (assetTypeId is null)
                    return await ReplyAsync(request, chatKey, AssetAnswerBuilder.UnknownAssetType(_localizer, parsed.AssetTypeName));

                filter.AssetTypeId = assetTypeId;
            }

            if (parsed.DepartmentName is not null)
            {
                var departmentId = await _unitOfWork.AiLookup.GetDepartmentIdByNameAsync(parsed.DepartmentName, cancellationToken);

                if (departmentId is null)
                    return await ReplyAsync(request, chatKey, AssetAnswerBuilder.UnknownDepartment(_localizer, parsed.DepartmentName));

                filter.DepartmentId = departmentId;
            }

            // ---- Authorization: decided by code, from the JWT, never by the AI ----
            if (parsed.IsAboutSelf)
            {
                // Authenticated, but the account is not linked to an employee record.
                if (_currentUser.EmployeeId is null)
                    return await ReplyAsync(request, chatKey, AssetAnswerBuilder.NoEmployeeLink(_localizer));

                filter.EmployeeId = _currentUser.EmployeeId;
            }
            else if (parsed.EmployeeName is not null)
            {
                if (!_currentUser.IsAdmin)
                    return await ReplyAsync(request, chatKey, AssetAnswerBuilder.CannotLookUpOthers(_localizer));

                var matches = await _unitOfWork.AiLookup.FindEmployeesByNameAsync(parsed.EmployeeName, cancellationToken);

                if (matches.Count == 0)
                    return await ReplyAsync(request, chatKey, AssetAnswerBuilder.UnknownEmployee(_localizer, parsed.EmployeeName));

                // An exact name wins: "Hesham" is not ambiguous just because "Hesham2" also contains it.
                // OrdinalIgnoreCase: "hesham" and "Hesham" count as the same name.
                // Either name counts: the user may type the English or the Arabic one.
                var exactMatches = matches.Where(m => string.Equals(m.FullName, parsed.EmployeeName, StringComparison.OrdinalIgnoreCase) ||
                                                      string.Equals(m.FullNameAr, parsed.EmployeeName, StringComparison.OrdinalIgnoreCase))
                                          .ToList();
                if (exactMatches.Count == 1)
                    matches = exactMatches;

                // Still more than one person: ask, with one clickable chip per person.
                // Each chip uses the FULL name, so clicking it hits the exact-match rule above.
                if (matches.Count > 1)
                    return await ReplyAsync(request, chatKey,
                                 AssetAnswerBuilder.AmbiguousEmployee(_localizer, parsed.EmployeeName, matches),
                                 suggestions: matches.Select(m => (string)_localizer["Show assets assigned to {0}", m.DisplayName]).ToList());

                filter.EmployeeId = matches[0].Id;
                employeeFullName = matches[0].DisplayName;
            }

            var page = await _unitOfWork.Assets.GetPaginationAsync(filter, cancellationToken);

            // The employee exists but owns nothing that matches (e.g. "Hesham2"): say it clearly.
            // Returned before SaveLast, so an empty search never becomes the base of a follow-up.
            if (page.TotalCount == 0 && employeeFullName is not null)
                return await GenerateReplyAsync(request, chatKey, history,
                                                AssetAnswerBuilder.NoAssetsForEmployee(_localizer, employeeFullName),
                                                cancellationToken);

            // Remember the exact filters of this search for the next question.
            await _conversationStore.SaveLastAsync(chatKey, parsed, cancellationToken);

            if (parsed.Intent == AssetQuestionIntent.CountAssets)
            {
                return await GenerateReplyAsync(request, chatKey, history,
                                                AssetAnswerBuilder.ForCount(_localizer, page.TotalCount),
                                                cancellationToken,
                                                totalCount: page.TotalCount);
            }

            // PurchaseCost is removed here for non-admins, BEFORE the rows reach the AI writer.
            var rows = page.Items.Select(asset => MapAsset(asset, includeCost: _currentUser.IsAdmin)).ToList();

            return await GenerateReplyAsync(request, chatKey, history,
                                            AssetAnswerBuilder.ForList(_localizer, rows.Count, page.TotalCount),
                                            cancellationToken,
                                            rows, page.TotalCount);
        }
        #endregion

        #region Private Methods
        // A reply written by the AI from the facts + rows. If the AI fails, the writer returns the facts.
        private async Task<ApiResponse<AssetQuestionResponse>> GenerateReplyAsync(AskAssetQuestionQuery request,
                                                                                   string chatKey,
                                                                                   IReadOnlyList<ChatTurn> history,
                                                                                   string facts,
                                                                                   CancellationToken cancellationToken,
                                                                                   IReadOnlyList<AssetQuestionResultDto>? rows = null,
                                                                                   int totalCount = 0,
                                                                                   IReadOnlyList<string>? suggestions = null)
        {
            var context = new AssetAnswerContext
            {
                Question = request.Question,
                History = history,
                Facts = facts,
                Assets = rows ?? Array.Empty<AssetQuestionResultDto>(),
                TotalCount = totalCount
            };

            var answer = await _answerWriter.WriteAnswerAsync(context, cancellationToken);

            return await ReplyAsync(request, chatKey, answer, rows, totalCount, suggestions);
        }

        // Every reply goes through here, so EVERY turn is saved to the chat history
        // (also refusals and "which one did you mean?", so "the first one" can be understood next).
        private async Task<ApiResponse<AssetQuestionResponse>> ReplyAsync(AskAssetQuestionQuery request,
                                                                          string chatKey,
                                                                          string answer,
                                                                          IReadOnlyList<AssetQuestionResultDto>? rows = null,
                                                                          int totalCount = 0,
                                                                          IReadOnlyList<string>? suggestions = null)
        {
            // CancellationToken.None: the answer is ready; if the user closes the tab now
            // the turn should still be saved, or the next question would miss it.
            await _conversationStore.AddTurnAsync(chatKey, request.Question, answer, CancellationToken.None);

            return new ApiResponse<AssetQuestionResponse>
            {
                Success = true,
                Message = "Question answered successfully.",
                data = new AssetQuestionResponse
                {
                    Question = request.Question,
                    Answer = answer,
                    Assets = rows ?? Array.Empty<AssetQuestionResultDto>(),
                    TotalCount = totalCount,
                    Suggestions = suggestions ?? Array.Empty<string>()
                }
            };
        }

        // Maps an AssetEntity to an AssetQuestionResultDto, optionally including the purchase cost.
        private static AssetQuestionResultDto MapAsset(AssetEntity asset, bool includeCost)
        {
            return new AssetQuestionResultDto
            {
                Id = asset.Id,
                AssetCode = asset.AssetCode,
                AssetName = asset.AssetName,
                SerialNumber = asset.SerialNumber,
                Manufacturer = asset.Manufacturer,
                Model = asset.Model,
                // This reply is NOT cached, so the names can be picked here for the request's language
                // (Arabic when it was filled in, otherwise English).
                AssetType = asset.AssetType is null ? null : LocalizedName.Pick(asset.AssetType.TypeName, asset.AssetType.TypeNameAr),
                Category = asset.Category is null ? null : LocalizedName.Pick(asset.Category.CategoryName, asset.Category.CategoryNameAr),
                Status = ((AssetStatus)asset.Status).ToString(),       // the Status byte -> its enum name
                EmployeeName = asset.AssignedEmployee is null ? null : LocalizedName.Pick(asset.AssignedEmployee.FullName, asset.AssignedEmployee.FullNameAr),
                DepartmentName = asset.Department is null ? null : LocalizedName.Pick(asset.Department.DepartmentName, asset.Department.DepartmentNameAr),
                LocationName = asset.Location is null ? null : LocalizedName.Pick(asset.Location.LocationName, asset.Location.LocationNameAr),
                PurchaseCost = includeCost ? asset.PurchaseCost : null  // hidden (and removed from the JSON) for non-admins
            };
        }
        #endregion
    }
}
