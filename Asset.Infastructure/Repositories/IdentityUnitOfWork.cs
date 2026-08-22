#region
using Asset.Application.Common.Interfaces;
using Asset.Infastructure.DBContext.Identity;
#endregion

/// <summary>
/// EF Core's change tracker already is a unit of work; this is what lets the
/// application layer commit without seeing DbContext.
/// </summary>
/// 
namespace Asset.Infastructure.Repositories;
public class IdentityUnitOfWork : IIdentityUnitOfWork
{
    #region Fields
    private readonly AppIdentityDbContext _context;
    #endregion

    #region Constructor
    public IdentityUnitOfWork(AppIdentityDbContext context)=> _context = context;
    #endregion

    #region Methods
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
         return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        // An explicit transaction is only needed when two saves must be joined.
        // UserManager saves on its own, so a role change plus a token revocation
        // is two saves - and both must land or neither.
        //
        // UserManager and the repositories share the same scoped
        // AppIdentityDbContext, so they enlist in this transaction automatically.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await action();
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        // No rollback call: leaving the using block without committing rolls
        // back, so an exception thrown inside action() is already handled.
    }
    #endregion
}
