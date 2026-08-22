namespace Asset.Application.Common.Interfaces;

/// <summary>
/// The signed-in caller, read from the validated JWT.
///
/// This is the ONLY way a handler learns who is calling. Identity is never
/// read from the request body (R1.3), so a client cannot send
/// "role": "Admin" and be believed.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    //string? Role { get; }
    int? EmployeeId { get; }
    bool IsAuthenticated { get; }
    public bool IsAdmin { get; }
}
