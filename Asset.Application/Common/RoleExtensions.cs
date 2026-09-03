using Asset.Domain.Enum;
namespace Asset.Application.Common
{
    public static class RoleExtensions
    {
        public static Role ToRole(this string? roleName)
        {
            return Enum.TryParse<Role>(roleName, out var role) ? role : Role.User;
        }           
    }
}