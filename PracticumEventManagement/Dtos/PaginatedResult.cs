namespace PracticumEventManagement.Dtos;

public class PaginatedResult<T>
{
    public int TotalCount { get; set; }

    public IEnumerable<T> Items { get; set; } = [];

    public int Page { get; set; }

    public int Count { get; set; }
}