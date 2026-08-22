namespace Asset.Application.Features.Category.Commands.CommandResponse
{
    public class UpdateCategoryResponseDto
    {
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
