#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Employees.Queries.QueryModels;
using Asset.Application.Features.Employees.Queries.QueryResponses;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using MediatR;
#endregion

namespace Asset.Application.Features.Employees.Queries.QueryHandlers
{
    public class GetEmployeeListQueryHandler :  IRequestHandler<GetAvailableEmployeesQueryModel, IReadOnlyList<AvailableEmployeeDto>>,
                                                IRequestHandler<GetEmployeesPaginatedQuery, ApiResponse<PagedResult<GetEmployeeListQueryResponse>>>,
                                                IRequestHandler<GetEmployeeByIdQueryModel, ApiResponse<GetEmployeeByIdResponse>> ,
                                                IRequestHandler<GetEmployeesLookupQueryModel, IReadOnlyList<AvailableEmployeeDto>>

    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRepository userRepository;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public GetEmployeeListQueryHandler(IUnitOfWork unitOfWork , IUserRepository userRepository, IMapper mapper)
        {
            this.userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        #endregion

        #region Handlers
        public async Task<IReadOnlyList<AvailableEmployeeDto>> Handle(GetAvailableEmployeesQueryModel request, CancellationToken cancellationToken)
        {
            var takenIds = await userRepository.GetLinkedEmployeeIdsAsync(cancellationToken);
            var available = await _unitOfWork.Employees.GetAvailableAsync(takenIds,request.departmentId, cancellationToken);

            return available.Select(e => new AvailableEmployeeDto
                            {
                                Id = e.Id,
                                EmployeeName = e.FullName,
                                DepartmentId = e.DepartmentId,
                                IsActive = e.IsActive,
                            }).ToList(); ;
        }

        public async Task<ApiResponse<PagedResult<GetEmployeeListQueryResponse>>> Handle(GetEmployeesPaginatedQuery request, CancellationToken cancellationToken)
        {
            var (employees, totalCount) = await _unitOfWork.Employees.GetPagedAsync(request.Search,request.DepartmentId,request.IsActive,
                                                                                  request.PageNumber,request.PageSize,cancellationToken);

            var result = new PagedResult<GetEmployeeListQueryResponse>
            {
                Items = _mapper.Map<List<GetEmployeeListQueryResponse>>(employees),
                TotalCount = totalCount,
                Page = request.PageNumber,
                PageSize = request.PageSize
            };

            return new ApiResponse<PagedResult<GetEmployeeListQueryResponse>>
            {
                data = result,
                Success = true,
                Message = "Employees Retrieved Successfully"
            };
        }

        public async Task<ApiResponse<GetEmployeeByIdResponse>> Handle(GetEmployeeByIdQueryModel request, CancellationToken cancellationToken)
        {
            var employee = await _unitOfWork.Employees.GetByIdWithDepartmentAsNoTrackingAsync(request.Id, cancellationToken);
            if (employee is null)
                throw new NotFoundException($"Employee {request.Id} does not exist.");

            return new ApiResponse<GetEmployeeByIdResponse>
            {
                data = _mapper.Map<GetEmployeeByIdResponse>(employee),
                Success = true,
                Message = "Employee Retrieved Successfully"
            };
        }

        public async Task<IReadOnlyList<AvailableEmployeeDto>> Handle(GetEmployeesLookupQueryModel request, CancellationToken cancellationToken)
        {
            var employees = await _unitOfWork.Employees.GetLookupAsync(cancellationToken);

            return employees.Select(e => new AvailableEmployeeDto
            {
                Id = e.Id,
                EmployeeName = e.FullName,
                DepartmentId = e.DepartmentId,
                IsActive = e.IsActive
            }).ToList();
        }
        #endregion
    }
}