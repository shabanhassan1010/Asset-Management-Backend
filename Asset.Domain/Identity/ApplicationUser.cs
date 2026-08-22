using Microsoft.AspNetCore.Identity;
namespace Asset.Domain.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        // The bridge to Employees. int? is fine; it's not a navigation property,
        // because Employees is in a different DbContext (AssetManagementDbContext).
        // EF won't be able to perform an Include across these boundaries.
        public int? EmployeeId { get; set; }
    }
}
