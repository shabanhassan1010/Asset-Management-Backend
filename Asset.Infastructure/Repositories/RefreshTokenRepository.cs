#region
using Asset.Application.Common.Interfaces;
using Asset.Domain.Identity;
using Asset.Infastructure.DBContext.Identity;
using Microsoft.EntityFrameworkCore;
#endregion

namespace Asset.Infastructure.Repositories;
public class RefreshTokenRepository : IRefreshTokenRepository
{
    #region Fields
    private readonly AppIdentityDbContext _context;
    #endregion

    #region Constructor
    public RefreshTokenRepository(AppIdentityDbContext context) => _context = context;
    #endregion

    #region Logout & Refresh Token Method
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }
    #endregion

    #region Refresh Token
    public async Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken)
    {
        var live = await _context.RefreshTokens.Where(t => 
                                                      t.UserId == userId &&  // Get Refresh token related to this user
                                                     !t.IsRevoked &&         // and get which   IsRevoked = false   
                                                      t.ExpiresAt > DateTime.UtcNow)  // get Token which is not Expire 
                                               .ToListAsync(cancellationToken);

        foreach (var token in live)
            token.IsRevoked = true;
    }
    #endregion 

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
         await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }
}