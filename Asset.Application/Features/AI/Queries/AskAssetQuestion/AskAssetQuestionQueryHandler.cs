#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Responses;
using Asset.Application.Features.AI.Enums;
using Asset.Application.Features.AI.Enums.DTos;
using Asset.Application.Features.AI.Interfases;
using Asset.Application.Features.AI.ServiceImplementation;
using Asset.Application.Features.Assets.DTOs;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
using Asset.Domain.Enum;
using AssetEntity = Asset.Domain.Models.Asset;
using MediatR;
#endregion
namespace Asset.Application.Features.AI.Queries.AskAssetQuestion
{
    public class AskAssetQuestionQueryHandler : IRequestHandler<AskAssetQuestionQuery, ApiResponse<AssetQuestionResponse>>
    {
        #region Fields
        private readonly IAssetQuestionParser _parser;
        private readonly IAssetRepository _assetRepository;
        private readonly IAiLookupRepository _lookupRepository;
        private readonly ICurrentUserService _currentUser;
        private const int MaxRows = 20;

        #endregion

        #region Constructor
        public AskAssetQuestionQueryHandler(IAssetQuestionParser parser,
                                            IAssetRepository assetRepository,
                                            IAiLookupRepository lookupRepository,
                                            ICurrentUserService currentUser)
        {
            _parser = parser;
            _assetRepository = assetRepository;
            _lookupRepository = lookupRepository;
            _currentUser = currentUser;
        }

        #endregion

        #region Private Methods
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
                AssetType = asset.AssetType?.TypeName,
                Category = asset.Category?.CategoryName,
                Status = ((AssetStatus)asset.Status).ToString(),
                EmployeeName = asset.AssignedEmployee?.FullName,
                DepartmentName = asset.Department?.DepartmentName,
                LocationName = asset.Location?.LocationName,
                PurchaseCost = includeCost ? asset.PurchaseCost : null
            };
        }

        private static ApiResponse<AssetQuestionResponse> Answer(string question, string answer, IReadOnlyList<AssetQuestionResultDto>? rows = null,
            int totalCount = 0 , IReadOnlyList<string>? suggestions = null)
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
        public async Task<ApiResponse<AssetQuestionResponse>> Handle( AskAssetQuestionQuery request,CancellationToken cancellationToken)
        {
            var parsed = await _parser.ParseAsync(request.Question, cancellationToken);

            if (parsed.Intent == AssetQuestionIntent.Greeting)
            {
                return Answer(request.Question,AssetAnswerBuilder.Greeting(request.Question.ToLowerInvariant()), suggestions: AssetAnswerBuilder.StarterQuestions());
            }
            // Out of scope, including every write request - the intent enum has no
            // member that could represent one.
            if (parsed.Intent == AssetQuestionIntent.Unsupported || !parsed.HasAnyFilter)
            {
                return Answer(request.Question, AssetAnswerBuilder.OutOfScope(), suggestions: AssetAnswerBuilder.StarterQuestions());
            }

            var filter = new AssetFilter
            {
                Page = 1,
                PageSize = parsed.Intent == AssetQuestionIntent.CountAssets ? 1 : MaxRows,
                Manufacturer = parsed.Manufacturer,
                StatusId = parsed.Status.HasValue ? (byte)parsed.Status.Value : null,

                // Retired assets stay hidden unless the question is about them.
                IncludeRetired = parsed.Status == AssetStatus.Retired
            };

            // ---- Resolve the names the user typed into real ids -------------------
            // Each miss returns a sentence, not an exception (R4.5).

            if (parsed.AssetTypeName is not null)
            {
                var assetTypeId = await _lookupRepository
                    .GetAssetTypeIdByNameAsync(parsed.AssetTypeName, cancellationToken);

                if (assetTypeId is null)
                    return Answer(request.Question, AssetAnswerBuilder.UnknownAssetType(parsed.AssetTypeName));

                filter.AssetTypeId = assetTypeId;
            }

            if (parsed.DepartmentName is not null)
            {
                var departmentId = await _lookupRepository
                    .GetDepartmentIdByNameAsync(parsed.DepartmentName, cancellationToken);

                if (departmentId is null)
                    return Answer(request.Question, AssetAnswerBuilder.UnknownDepartment(parsed.DepartmentName));

                filter.DepartmentId = departmentId;
            }

            // ---- Authorization ----------------------------------------------------
            // Runs AFTER parsing and BEFORE the query, and reads only from the token.
            // Nothing the person typed has a vote in this block.
            //
            // The rule being enforced is R4.3 / R2.6: the restriction is on the COST
            // FIELD, not on which rows exist. An asset catalogue is not confidential
            // inside a company; its purchase prices are. So a non-admin sees the same
            // rows an admin sees, with PurchaseCost stripped.

            if (parsed.IsAboutSelf)
            {
                // "my assets" means the caller, for an admin exactly as for a user.
                // The word "me" carried no identity across the wire - it only told us
                // to look at the token, which is the only thing that knows who asked.
                if (_currentUser.EmployeeId is null)
                    return Answer(request.Question, AssetAnswerBuilder.NoEmployeeLink());

                filter.EmployeeId = _currentUser.EmployeeId;
            }
            else if (parsed.EmployeeName is not null)
            {
                if (_currentUser.IsAdmin)
                {
                    var matches = await _lookupRepository
                        .FindEmployeesByNameAsync(parsed.EmployeeName, cancellationToken);

                    if (matches.Count == 0)
                        return Answer(request.Question, AssetAnswerBuilder.UnknownEmployee(parsed.EmployeeName));

                    if (matches.Count > 1)
                        return Answer(request.Question, AssetAnswerBuilder.AmbiguousEmployee(parsed.EmployeeName, matches));

                    filter.EmployeeId = matches[0].Id;
                }
                else
                {
                    // A non-admin asking about a named colleague is narrowed to
                    // themselves. Unconditional assignment: we never compare the typed
                    // name to the caller's own name, because comparing would mean the
                    // text had influenced the outcome.
                    if (_currentUser.EmployeeId is null)
                        return Answer(request.Question, AssetAnswerBuilder.NoEmployeeLink());

                    filter.EmployeeId = _currentUser.EmployeeId;
                }
            }

            // ---- The single read path --------------------------------------------
            // The same method the assets list screen calls. No AI-specific query,
            // no raw SQL, nothing this feature can reach that the UI cannot.

            var page = await _assetRepository.GetPaginationAsync(filter, cancellationToken);

            if (parsed.Intent == AssetQuestionIntent.CountAssets)
            {
                return Answer(
                    request.Question,
                    AssetAnswerBuilder.ForCount(page.TotalCount),
                    totalCount: page.TotalCount);
            }

            var rows = page.Items
                .Select(asset => MapAsset(asset, includeCost: _currentUser.IsAdmin))
                .ToList();

            return Answer(
                request.Question,
                AssetAnswerBuilder.ForList(rows.Count, page.TotalCount),
                rows,
                page.TotalCount);
        }
        #endregion
    }
}