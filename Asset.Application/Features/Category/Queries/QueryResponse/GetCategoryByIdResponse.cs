namespace Asset.Application.Features.Category.Queries.QueryResponse
{
    public class GetCategoryByIdResponse
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
