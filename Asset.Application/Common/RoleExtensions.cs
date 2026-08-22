using Asset.Domain.Enum;
namespace Asset.Application.Common
{
    /// <summary>
    /// The enum is the code-side representation; Identity stores role names as
    /// strings in AspNetRoles.Name. This is the one place that crosses between
    /// them, so the fallback rule is written once.
    ///
    /// Going the other way needs no helper - role.ToString() is the name.
    /// </summary>
    public static class RoleExtensions
    {
        /// <summary>
        /// Falls back to User rather than throwing: a hand-edited database with an
        /// unknown role name should downgrade that account, not take the API down.
        /// The fallback is the least privileged value on purpose.
        /// </summary>
        public static Role ToRole(this string? roleName)
        {
            return Enum.TryParse<Role>(roleName, out var role) ? role : Role.User;
        }           
    }

}
