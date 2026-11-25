#region
using Asset.Application.Bases;
using Asset.Application.Common;
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Category.Commands.CommandModels;
using Asset.Application.Features.Category.Commands.CommandResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Resources;
using Asset.Domain.Exceptions;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using System.Security.AccessControl;
using cateogryEntity = Asset.Domain.Models.Category;
#endregion

namespace Asset.Application.Features.Category.Commands.CommandHandlers
{
    public class CategoryCommandHandler : BaseResponseHandler,
                                                IRequestHandler<CreateCategoryCommandModel, ApiResponse<CreateCategoryResponseDto>>,
                                                IRequestHandler<UpdateCategoryCommandModel, ApiResponse<UpdateCategoryResponseDto>>,
                                                IRequestHandler<DeleteCategoryCommandModel, ApiResponse<string>>,
                                            IRequestHandler<ActivateCategoryCommandModel, ApiResponse<string>>
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService cache;
        #endregion

        #region Constructor
        public CategoryCommandHandler(IUnitOfWork unitOfWork,IMapper mapper, ICacheService cache,
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
            var name = request.CategoryName.Trim();
            if(await _unitOfWork.Categories.CategoryNameExistsAsync(name,null,cancellationToken))
                throw new BusinessException("A Category with this name already exists.");

            var nameAr = LocalizedName.TrimOrNull(request.CategoryNameAr);
            if (nameAr is not null && await _unitOfWork.Categories.CategoryNameArExistsAsync(nameAr, null, cancellationToken))
                throw new BusinessException("Arabic category name '{0}' is already in use.", nameAr);

            var entity = _mapper.Map<cateogryEntity>(request);
            entity.CategoryName = name;
            entity.CategoryNameAr = nameAr;
            entity.IsActive = true;

            _unitOfWork.Categories.Add(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();

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
            if (entity is null || !entity.IsActive)
                throw new NotFoundException("Category {0} was not found.", request.Id);

            var name = request.CategoryName.Trim();                                              

            if (await _unitOfWork.Categories.CategoryNameExistsAsync(name, request.Id, cancellationToken))  
                throw new BusinessException("A Category with this name already exists.");

            var nameAr = LocalizedName.TrimOrNull(request.CategoryNameAr);
            if (nameAr is not null && await _unitOfWork.Categories.CategoryNameArExistsAsync(nameAr, request.Id, cancellationToken))
                throw new BusinessException("Arabic category name '{0}' is already in use.", nameAr);

            _mapper.Map(request, entity);
            entity.CategoryName = name; 
            entity.CategoryNameAr = nameAr;      // "" from the form is saved as NULL

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();

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
            if (entity is null || !entity.IsActive)
                throw new NotFoundException("Category {0} was not found.", request.Id);

            // Check if this category Has Any Assets
            var hasAssets = await _unitOfWork.Categories.HasAssetsAsync(request.Id, cancellationToken);
            if (hasAssets)
                throw new BusinessException("This Category has linked assets. Please remove or reassign them before deleting this category.");

            _unitOfWork.Categories.DeActivate(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();


            return new ApiResponse<string>
            {
                data = $"Category {request.Id} was deleted.",
                Success = true,
                Message = "Category Deactivated Successfully"
            };
        }
        #endregion

        public async Task<ApiResponse<string>> Handle(ActivateCategoryCommandModel request, CancellationToken cancellationToken)
        {
            // GetByIdAsync returns inactive rows too (it is a plain find by id), and the entity is tracked,
            // so changing IsActive and calling SaveChanges is enough.
            var entity = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null)
                throw new NotFoundException("Category {0} was not found.", request.Id);

            // Nothing to do: answer OK instead of an error, so clicking twice is harmless.
            if (entity.IsActive)
                return new ApiResponse<string>
                {
                    data = entity.CategoryName,
                    Success = true,
                    Message = "Category is already active"
                };

            // The names are UNIQUE among ACTIVE rows only (filtered indexes ..._Active).
            // While this row was inactive someone may have created a new one with the same name.
            // Turning this one back on would then break the index, so we refuse with a clear message.
            if (await _unitOfWork.Categories.CategoryNameExistsAsync(entity.CategoryName, entity.Id, cancellationToken))
                throw new ConflictException("Another active category is already named '{0}'. Rename one of them first.", entity.CategoryName);

            if (entity.CategoryNameAr is not null &&
                await _unitOfWork.Categories.CategoryNameArExistsAsync(entity.CategoryNameAr, entity.Id, cancellationToken))
                throw new ConflictException("Another active category already uses the Arabic name '{0}'. Rename one of them first.", entity.CategoryNameAr);

            entity.IsActive = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();     // the dropdowns and every page of the list must show it again

            return new ApiResponse<string>
            {
                data = entity.CategoryName,
                Success = true,
                Message = "Category Activated Successfully"
            };
        }

        #region Pricate Methods
        private async Task InvalidateAsync()
        {
            await cache.RemoveAsync(CacheKeys.CategoryList);              // full list (dropdowns)
            await cache.BumpVersionAsync(CacheKeys.CategoriesVersion);    // every page of the paged list
            await cache.BumpVersionAsync(CacheKeys.AssetsVersion);
        }
        #endregion
    }
}