#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Responses;
using Asset.Application.Features.AI.Enums;
using Asset.Application.Features.AI.Enums.DTos;
using Asset.Application.Features.AI.Interfases;
using Asset.Application.Features.Assets.DTOs;
using Asset.Domain.Enum;
using AssetEntity = Asset.Domain.Models.Asset;
using MediatR;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Features.AI.Builders;
using Asset.Application.Features.AI.IService;
#endregion
namespace Asset.Application.Features.AI.Queries.AskAssetQuestion
{
    public class AskAssetQuestionQueryHandler : IRequestHandler<AskAssetQuestionQuery, ApiResponse<AssetQuestionResponse>>
    {
        #region Fields
        private readonly IAssetQuestionParserService _parser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IConversationStoreService _conversationStore;
        private const int MaxRows = 20;

        #endregion

        #region Constructor
        public AskAssetQuestionQueryHandler(IAssetQuestionParserService parser,
                                            IUnitOfWork unitOfWork,
                                            ICurrentUserService currentUser ,
                                            IConversationStoreService conversationStore)
        {
            _parser = parser;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _conversationStore = conversationStore;
        }

        #endregion

        #region Private Methods

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
                AssetType = asset.AssetType?.TypeName,                // If AssetType is not null return its TypeName and if it is null return null
                Category = asset.Category?.CategoryName,             //  If Category is not null return its CategoryName and if it is null return null
                Status = ((AssetStatus)asset.Status).ToString(),     // Cast the Status byte to AssetStatus enum and convert it to string
                EmployeeName = asset.AssignedEmployee?.FullName, 
                DepartmentName = asset.Department?.DepartmentName,
                LocationName = asset.Location?.LocationName,
                PurchaseCost = includeCost ? asset.PurchaseCost : null  // If includeCost is true return PurchaseCost and if it is false return null 
            };
        }

        private static ApiResponse<AssetQuestionResponse> Answer(string question, string answer, 
            IReadOnlyList<AssetQuestionResultDto>? rows = null, int totalCount = 0 , IReadOnlyList<string>? suggestions = null)
        {
            return new ApiResponse<AssetQuestionResponse>
            {
                Success = true,
                Message = "Question answered successfully.",
                data = new AssetQuestionResponse
                {
                    Question = question,
                    Answer = answer,
                    Assets = rows ?? Array.Empty<AssetQuestionResultDto>(),
                    TotalCount = totalCount ,
                    Suggestions = suggestions ?? Array.Empty<string>()
                }
            };
        }

        #endregion

        #region Methods
        public async Task<ApiResponse<AssetQuestionResponse>> Handle(AskAssetQuestionQuery request,CancellationToken cancellationToken)
        {
            // Load the last question that ran a query, so follow-ups can reuse it.
            var previous = _conversationStore.GetLast(request.SessionId);

            var parsed = await _parser.ParseAsync(request.Question, previous, cancellationToken);

            if (parsed.Intent == AssetQuestionIntent.Greeting)
            {
                return Answer(request.Question,AssetAnswerBuilder.Greeting(request.Question.ToLowerInvariant()), suggestions: AssetAnswerBuilder.StarterQuestions());
            }

            if (parsed.Intent == AssetQuestionIntent.Unsupported)
            {
                return Answer(request.Question, AssetAnswerBuilder.OutOfScope(), suggestions: AssetAnswerBuilder.StarterQuestions());
            }

            // "How many assets do we have?" with no filter is a fair question.
            // An admin gets the org-wide total; anyone else gets their own count.
            if (!parsed.HasAnyFilter)
            {
                if (parsed.Intent != AssetQuestionIntent.CountAssets)
                {
                    return Answer(request.Question, AssetAnswerBuilder.OutOfScope(),suggestions: AssetAnswerBuilder.StarterQuestions());
                }

                if (!_currentUser.IsAdmin)
                {
                    if (_currentUser.EmployeeId is null)
                        return Answer(request.Question, AssetAnswerBuilder.NoEmployeeLink());

                    parsed = parsed with { IsAboutSelf = true };
                }
            }

            var filter = new AssetFilter
            {
                Page = 1,
                PageSize = parsed.Intent == AssetQuestionIntent.CountAssets ? 1 : MaxRows,
                Manufacturer = parsed.Manufacturer,
                StatusId = parsed.Status.HasValue ? (byte)parsed.Status.Value : null,
                IncludeRetired = parsed.Status == AssetStatus.Retired
            };


            if (parsed.AssetTypeName is not null)
            {
                var assetTypeId = await _unitOfWork.AiLookup.GetAssetTypeIdByNameAsync(parsed.AssetTypeName, cancellationToken);

                if (assetTypeId is null)
                    return Answer(request.Question, AssetAnswerBuilder.UnknownAssetType(parsed.AssetTypeName));

                filter.AssetTypeId = assetTypeId;
            }

            if (parsed.DepartmentName is not null)
            {
                var departmentId = await _unitOfWork.AiLookup.GetDepartmentIdByNameAsync(parsed.DepartmentName, cancellationToken);

                if (departmentId is null)
                    return Answer(request.Question, AssetAnswerBuilder.UnknownDepartment(parsed.DepartmentName));

                filter.DepartmentId = departmentId;
            }

            // ---- Authorization ----------------------------------------------------
            if (parsed.IsAboutSelf)
            {
                // in this case User is Authenticated but not linked to an Employee, so we cannot answer the question about their own assets.
                if (_currentUser.EmployeeId is null)
                    return Answer(request.Question, AssetAnswerBuilder.NoEmployeeLink());
                // in this case User is Authenticated and linked to an Employee, so we can answer the question about their own assets.
                filter.EmployeeId = _currentUser.EmployeeId;
            }
            else if (parsed.EmployeeName is not null)
            {
                if (_currentUser.IsAdmin)
                {
                    var matches = await _unitOfWork.AiLookup.FindEmployeesByNameAsync(parsed.EmployeeName, cancellationToken);

                    if (matches.Count == 0)
                        return Answer(request.Question, AssetAnswerBuilder.UnknownEmployee(parsed.EmployeeName));

                    // in this case, we have multiple employees with the same name, so we cannot answer the question about their assets.
                    if (matches.Count > 1) 
                        return Answer(request.Question, AssetAnswerBuilder.AmbiguousEmployee(parsed.EmployeeName, matches));

                    filter.EmployeeId = matches[0].Id;
                } 
                //else // User not Admin
                //{
                //    if (_currentUser.EmployeeId is null)
                //        return Answer(request.Question, AssetAnswerBuilder.NoEmployeeLink());

                //    filter.EmployeeId = _currentUser.EmployeeId;
                //}
            }

            var page = await _unitOfWork.Assets.GetPaginationAsync(filter, cancellationToken);
            // Save only questions that actually ran a query, so a follow-up has
            // real filters to fall back on.
            _conversationStore.SaveLast(request.SessionId, parsed);

            if (parsed.Intent == AssetQuestionIntent.CountAssets)
            {
                return Answer(request.Question, AssetAnswerBuilder.ForCount(page.TotalCount),totalCount: page.TotalCount);
            }

            var rows = page.Items.Select(asset => MapAsset(asset, includeCost: _currentUser.IsAdmin)).ToList();

            return Answer(request.Question,AssetAnswerBuilder.ForList(rows.Count, page.TotalCount),rows,page.TotalCount);
        }
        #endregion
    }
}