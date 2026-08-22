using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asset.Application.Features.Assets.Commands.CommandResponse
{
    public class RetireAssetResponseDto
    {
        public int AssetId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public int Status { get; set; }
        public DateTime RetiredAt { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }
}
