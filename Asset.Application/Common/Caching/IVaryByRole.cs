namespace Asset.Application.Common.Caching
{
    // A "tag" interface: it has no members.
    // Add it to a cached query whose response is different for Admin and User
    // (e.g. PurchaseCost). CachingBehavior then adds the role to the cache key (R5.4).
    public interface IVaryByRole
    {
    }
}