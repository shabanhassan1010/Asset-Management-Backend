using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Users.DTOs;
using AutoMapper;
using MediatR;

namespace Asset.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// A read with no business rules, so it calls the repository directly - no
/// service layer in between for the sake of symmetry.
/// </summary>
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserListItemDto>>
{
    private readonly IUserRepository _users;
    private readonly IMapper _mapper;

    public GetUsersQueryHandler(IUserRepository users, IMapper mapper)
    {
        _users = users;
        _mapper = mapper;
    }

    public async Task<PagedResult<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _users.GetPagedAsync(
            request.Search,
            request.Role,
            request.IsActive,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return new PagedResult<UserListItemDto>
        {
            Items = _mapper.Map<List<UserListItemDto>>(items),
            TotalCount = totalCount,
            Page = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
