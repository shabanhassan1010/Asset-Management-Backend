#region
using Asset.Application.Bases;
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Category.Commands.CommandModels;
using Asset.Application.Features.Category.Commands.CommandResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Resoures;
using Asset.Domain.Exceptions;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using cateogryEntity = Asset.Domain.Models.Category;
#endregion

namespace Asset.Application.Features.Category.Commands.CommandHandlers
{
    public class CreateCategoryCommandHandler : BaseResponseHandler,
                                                IRequestHandler<CreateCategoryCommandModel, ApiResponse<CreateCategoryResponseDto>>,
                                                IRequestHandler<UpdateCategoryCommandModel, ApiResponse<UpdateCategoryResponseDto>>,
                                                IRequestHandler<DeleteCategoryCommandModel, ApiResponse<string>>
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService cache;
        #endregion

        #region Constructor
        public CreateCategoryCommandHandler(IUnitOfWork unitOfWork,IMapper mapper, ICacheService cache,
                                            IStringLocalizer<SharedResources> localizer) : base(localizer)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            this.cache = cache;
        }
        #endregion

        #region handlers
        public async Task<ApiResponse<CreateCategoryResponseDto>> Handle(CreateCategoryCommandModel request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<cateogryEntity>(request);
            entity.IsActive = true;

            await _unitOfWork.Categories.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Clearing before would let a concurrent read re-populate the cache with the old data, and a failed save would have cleared the cache for nothing.
            await cache.RemoveAsync(CacheKeys.CategoryList, cancellationToken);
            return new ApiResponse<CreateCategoryResponseDto>
            {
                data = _mapper.Map<CreateCategoryResponseDto>(entity),
                Success = true,
                Message = "Category Created Successfully"
            };
        }

        public async Task<ApiResponse<UpdateCategoryResponseDto>> Handle(UpdateCategoryCommandModel request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null)
                throw new NotFoundException($"Category {request.Id} was not found.");

            _mapper.Map(request, entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await cache.RemoveAsync(CacheKeys.CategoryById(request.Id), cancellationToken);
            await cache.RemoveAsync(CacheKeys.CategoryList, cancellationToken);

            return new ApiResponse<UpdateCategoryResponseDto>
            {
                data = _mapper.Map<UpdateCategoryResponseDto>(entity),
                Success = true,
                Message = "Category Updated Successfully"
            };
        }


        public async Task<ApiResponse<string>> Handle(DeleteCategoryCommandModel request, CancellationToken cancellationToken)
        {
            // Get Category Which i went to Delete It 
            var entity = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null)
                throw new NotFoundException($"Category {request.Id} was not found.");

            // Check if this category Has Any Assets
            var hasAssets = await _unitOfWork.Categories.HasAssetsAsync(request.Id, cancellationToken);
            if (hasAssets)
            {
                return new ApiResponse<string>
                {
                    data = $"Category {request.Id} has linked assets. Please remove or reassign them before deleting this category.",
                    Success = false,
                    Message = "Category Has Linked Assets"
                };
            }

            _unitOfWork.Categories.Remove(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await cache.RemoveAsync(CacheKeys.CategoryById(request.Id), cancellationToken);
            await cache.RemoveAsync(CacheKeys.CategoryList, cancellationToken);

            return new ApiResponse<string>
            {
                data = $"Category {request.Id} was deleted.",
                Success = true,
                Message = "CategoryDeletedSuccessfully"
            };
        }
        #endregion
    }
}