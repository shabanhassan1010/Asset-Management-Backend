using Asset.Application.Interfaces.Comman;
using System.Security.Claims;

namespace Asset.API.Services
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _accessor;
        public CurrentUser(IHttpContextAccessor httpContext)
        {
            _accessor = httpContext;
        }
        private ClaimsPrincipal? User => _accessor.HttpContext?.User;

        public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public bool IsAdmin => User?.IsInRole("Admin") ?? false;
    }
}