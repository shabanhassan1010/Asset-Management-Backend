using Asset.Application.Interfaces.IRepository;
using System.Security.Cryptography;
using System.Text;
namespace Asset.Infastructure.Security
{
    public class TokenHasher : ITokenHasher
    {
        public string Hash(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);

            var hash = SHA256.HashData(bytes);

            return Convert.ToBase64String(hash);
        }
    }
}