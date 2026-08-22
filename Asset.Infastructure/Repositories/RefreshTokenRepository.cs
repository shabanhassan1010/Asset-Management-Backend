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

    #region Methods
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)
    {
        // Tracked, because the caller flips IsRevoked on the row it gets back.
        // Hits the unique index configured on Token.
        return _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }       
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        // Staged only. The handler commits through the unit of work, so this
        // insert and everything else in the request succeed or fail together.
         await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }
    public async Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken)
    {
        var live = await _context.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
                                               .ToListAsync(cancellationToken);

        foreach (var token in live)
            token.IsRevoked = true;
    }
    #endregion
}
