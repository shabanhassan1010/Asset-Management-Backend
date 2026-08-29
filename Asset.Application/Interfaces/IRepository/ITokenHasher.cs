namespace Asset.Application.Interfaces.IRepository
{
    public interface ITokenHasher
    {
        string Hash(string token);
    }
}
