using Asset.Application.Common.Models;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Users.DTOs;
using Asset.Domain.Enum;
using Asset.Domain.Identity;
using MediatR;

namespace Asset.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Server-side paging and filtering, same shape as the asset list.
/// The defaults mean GET /api/users with no query string still returns page 1.
/// </summary>
public record GetUsersQuery(
    string? Search = null,
    Role? Role = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<UserListItemDto>>;
