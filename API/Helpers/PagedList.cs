using System;
using Microsoft.EntityFrameworkCore;

namespace API.Helpers;

public class PagedList<T> : List<T>
{
    public PagedList(IEnumerable<T> items, int count, int pageNumber, int pageSize)
    {
        CurrentPage = pageNumber;
        TotalPages = (int) Math.Ceiling(count / (double) pageSize);
        PageSize = pageSize;
        TotalCount = count;
        AddRange(items);
    }

    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    public static async Task<PagedList<T>> CreatAsync(IQueryable<T> source, 
        int pageNumber, int pageSize)
    {
        var count = await source.CountAsync();

        // if on page #1, 1 -1 = 0. 0 * pageSize (in this case we're saying we will return 5 results per page)
        // 0 * 5 = 0. Then we'll take 5 so we'll get the first 5 results
        // If on page #2, 2-1 = 1. 1 * 5 = 5. So we skip the first five then take the next five
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedList<T>(items, count, pageNumber, pageSize);
    }
}
