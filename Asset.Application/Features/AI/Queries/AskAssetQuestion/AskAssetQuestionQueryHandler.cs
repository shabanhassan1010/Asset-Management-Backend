#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Responses;
using Asset.Application.Features.AI.Enums;
using Asset.Application.Features.AI.Enums.DTos;
using Asset.Application.Features.AI.Interfases;
using Asset.Application.Features.AI.ServiceImplementation;
using Asset.Application.Features.Assets.DTOs;
using Asset.Domain.Enum;
using AssetEntity = Asset.Domain.Models.Asset;
using MediatR;
using Asset.Application.Interfaces.Comman;
#endregion
namespace Asset.Application.Features.AI.Queries.AskAssetQuestion
{
    public class AskAssetQuestionQueryHandler : IRequestHandler<AskAssetQuestionQuery, ApiResponse<AssetQuestionResponse>>
    {
        #region Fields
        private readonly IAssetQuestionParser _parser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private const int MaxRows = 20;

        #endregion

        #region Constructor
        public AskAssetQuestionQueryHandler(IAssetQuestionParser parser,
                                            IUnitOfWork unitOfWork,
                                            ICurrentUserService currentUser)
        {
            _parser = parser;
            _unitOfWork = unitOfWork;
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
                if (_currentUser.EmployeeId is null)
                    return Answer(request.Question, AssetAnswerBuilder.NoEmployeeLink());

                filter.EmployeeId = _currentUser.EmployeeId;
            }
            else if (parsed.EmployeeName is not null)
            {
                if (_currentUser.IsAdmin)
                {
                    var matches = await _unitOfWork.AiLookup.FindEmployeesByNameAsync(parsed.EmployeeName, cancellationToken);

                    if (matches.Count == 0)
                        return Answer(request.Question, AssetAnswerBuilder.UnknownEmployee(parsed.EmployeeName));

                    if (matches.Count > 1)
                        return Answer(request.Question, AssetAnswerBuilder.AmbiguousEmployee(parsed.EmployeeName, matches));

                    filter.EmployeeId = matches[0].Id;
                }
                else
                {
                    if (_currentUser.EmployeeId is null)
                        return Answer(request.Question, AssetAnswerBuilder.NoEmployeeLink());

                    filter.EmployeeId = _currentUser.EmployeeId;
                }
            }

            var page = await _unitOfWork.Assets.GetPaginationAsync(filter, cancellationToken);

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