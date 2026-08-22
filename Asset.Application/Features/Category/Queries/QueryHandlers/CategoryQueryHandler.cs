#region
using Asset.Application.Bases;
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Category.Queries.QueryModels;
using Asset.Application.Features.Category.Queries.QueryResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Resoures;
using Asset.Domain.Exceptions;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
#endregion

namespace Asset.Application.Features.Category.Queries.QueryHandlers
{
    public class CategoryQueryHandler : BaseResponseHandler,
                                        IRequestHandler<GetCategoryListQueryModel, ApiResponse<List<GetCategoryListResponse>>>,
                                        IRequestHandler<GetCategoryByIdQueryModel, ApiResponse<GetCategoryByIdResponse>>
    {
        #region Fields
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        #endregion

        #region Constructor
        public CategoryQueryHandler(IMapper mapper, IUnitOfWork unitOfWork, IStringLocalizer<SharedResources> localizer) : base(localizer)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Handlers
        public async Task<ApiResponse<List<GetCategoryListResponse>>> Handle(GetCategoryListQueryModel request, CancellationToken cancellationToken)
        {
            var categories = await _unitOfWork.Categories.GetAllProjectedAsync(cancellationToken);

            return new ApiResponse<List<GetCategoryListResponse>>
            {
                data = _mapper.Map<List<GetCategoryListResponse>>(categories),
                Success = true,
                Message = "Categories Retrieved Successfully"
            };
        }

        public async Task<ApiResponse<GetCategoryByIdResponse>> Handle(GetCategoryByIdQueryModel request, CancellationToken cancellationToken)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);

            if (category == null || category.IsActive == false)
                throw new NotFoundException($"Category {request.Id} was not found.");


            return new ApiResponse<GetCategoryByIdResponse>
            {
                data = _mapper.Map<GetCategoryByIdResponse>(category),
                Success = true,
                Message = "Category Retrieved Successfully"
            };
        }
        #endregion
    }
}
