using AutoMapper;

namespace Asset.Application.Mapping.AssetDto

{
    public partial class AssetProfile : Profile
    {
        public AssetProfile()
        {
            GetAssetByIdQueryMapping();
            GetAssetListQueryMapping();
            GetAssetPaginatedListQueryMapping();
            CreateAssetQueryMapping();
            UpdateAssetCommandMapping();
        }    
    }
}
