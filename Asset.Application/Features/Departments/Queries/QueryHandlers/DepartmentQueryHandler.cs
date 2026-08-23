#region
using Asset.Application.Common.Responses;
using Asset.Application.Features.Departments.Queries.QueryModels;
using Asset.Application.Features.Departments.Queries.QueryResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Exceptions;
using AutoMapper;
using MediatR;
#endregion

namespace Asset.Application.Features.Departments.Queries.QueryHandlers
{
    public class DepartmentQueryHandler :
                                        IRequestHandler<GetDepartmentListQueryModel, ApiResponse<IReadOnlyList<GetDepartmentListResponse>>>,
                                        IRequestHandler<GetDepartmentByIdQueryModel, ApiResponse<GetDepartmentByIdResponse>>
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public DepartmentQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        #endregion

        #region Handlers
        public async Task<ApiResponse<IReadOnlyList<GetDepartmentListResponse>>> Handle(GetDepartmentListQueryModel request, CancellationToken cancellationToken)
        {
            var list = await _unitOfWork.Departments.GetAllProjectedAsync(cancellationToken);

            return new ApiResponse<IReadOnlyList<GetDepartmentListResponse>>
            {
                data = list,
                Success = true,
                Message = "Departments Retrieved Successfully"
            };
        }

        public async Task<ApiResponse<GetDepartmentByIdResponse>> Handle(GetDepartmentByIdQueryModel request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Departments.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null || entity.IsActive == false)
                throw new NotFoundException($"Department {request.Id} does not exist.");

            return new ApiResponse<GetDepartmentByIdResponse>
            {
                data = _mapper.Map<GetDepartmentByIdResponse>(entity),
                Success = true,
                Message = "Department Retrieved Successfully"
            };
        }
        #endregion
    }
}
