using Microsoft.AspNetCore.Identity;
namespace Asset.Domain.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public int? EmployeeId { get; set; }
    }
}