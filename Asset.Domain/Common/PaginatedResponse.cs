namespace Asset.Domain.Common
{
    public class PaginatedResponse<T>
    {
        public PaginatedResponse(int pageIndex , int pageSize , int totalItems , IEnumerable<T> data)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalItems = totalItems;
            Data = data;
        }
        public IEnumerable<T> Data { get; set; }
        public int TotalItems { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages =>(int)Math.Ceiling((double)TotalItems/PageSize);

    }
}
