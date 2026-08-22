namespace Asset.Application.Features.Category.Queries.QueryResponse
{
    public class GetCategoryListResponse
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public int AssetsCount { get; set; }
    }
}
