#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Users.DTOs;
using AutoMapper;
using MediatR;
#endregion

namespace Asset.Application.Features.Users.Queries.GetUsers;
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserListItemDto>>
{
    #region Fields
    private readonly IUserRepository _users;
    private readonly IMapper _mapper;
    #endregion

    #region Constructor
    public GetUsersQueryHandler(IUserRepository users, IMapper mapper)
    {
        _users = users;
        _mapper = mapper;
    }
    #endregion

    public async Task<PagedResult<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var result = await _users.GetPagedAsync(request.Search,request.Role,request.IsActive, request.PageNumber,request.PageSize,cancellationToken);
        var items = result.Items;
        var totalCount = result.TotalCount;

        return new PagedResult<UserListItemDto>
        {
            Items = _mapper.Map<List<UserListItemDto>>(items),
            TotalCount = totalCount,
            Page = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}