using Asset.Domain.Identity;

namespace Asset.Domain.Models;
    public partial class AssetTransfer
    {
        public virtual ApplicationUser? TransferredByUser { get; set; }
    }
