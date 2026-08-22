namespace Asset.Application.Common.Interfaces;

/// <summary>
/// The transaction boundary for the identity database.
///
/// Named for its context on purpose: there are two DbContexts in this solution
/// (identity and the scaffolded asset model), so there are two boundaries. One
/// nameless IUnitOfWork would hide which one a handler is committing.
/// </summary>
public interface IIdentityUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs the work inside one explicit transaction and commits it.
    ///
    /// Needed because UserManager saves as soon as it is called. Changing a role
    /// and revoking the user's sessions are two separate saves, and a crash
    /// between them would leave the new role active with the old sessions still
    /// alive - exactly the hole the rule exists to close.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken);
}
