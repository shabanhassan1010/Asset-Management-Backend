namespace Asset.Application.Features.Auth.DTOs
{
    // Why I Use Record Here?
    // LoginCommand => is Just Data coming from the client describing an operation. 
    // So I do not use Big Behivor inside it he is just [UserName + Password]
    public record RefreshTokenResult(string Token, DateTime ExpiresAtUtc);
}

// Record = immutable is not Accurate 100%
// Record => generate init properties so after create it i can not update (In Constructor)