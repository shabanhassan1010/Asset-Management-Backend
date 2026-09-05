namespace Asset.Application.Interfaces.Comman;
public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    //string? Role { get; }
    int? EmployeeId { get; }
    bool IsAuthenticated { get; }
    public bool IsAdmin { get; }
}
