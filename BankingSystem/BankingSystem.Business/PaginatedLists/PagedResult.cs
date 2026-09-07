namespace BankingSystem.Business.PaginatedLists
{
    public class PagedResult<T>
    {
        public ICollection<T> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages =>
            (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
