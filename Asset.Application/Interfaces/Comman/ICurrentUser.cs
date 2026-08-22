namespace Asset.Application.Interfaces.Comman
{
    public interface ICurrentUser
    {
        string? UserId { get; }
        bool IsAdmin { get; }
    }
}
