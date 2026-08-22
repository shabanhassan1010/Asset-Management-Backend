namespace Asset.Application.Features.Locations.Queries.QueryResponse
{
    public class GetLocationListResponse
    {
        public int Id { get; set; }
        public string LocationName { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
        public int AssetsCount { get; set; }
    }
}
