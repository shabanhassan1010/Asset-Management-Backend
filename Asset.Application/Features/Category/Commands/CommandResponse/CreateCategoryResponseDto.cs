namespace Asset.Application.Features.Category.Commands.CommandResponse
{
    public class CreateCategoryResponseDto
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
