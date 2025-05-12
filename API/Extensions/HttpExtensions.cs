using System;
using System.Text.Json;
using API.Helpers;

namespace API.Extensions;

public static class HttpExtensions
{
    // This method adds pagination info to the response headers. 
    public static void AddPaginationHeader<T>(this HttpResponse response, 
        PagedList<T> data)
    {
        // Create the PaginationHeader object with the page data
        var paginationHeader = new PaginationHeader(data.CurrentPage, 
            data.PageSize, data.TotalCount, data.TotalPages);

        // Serialize the data to JSON
        var jsonOptions = new JsonSerializerOptions{PropertyNamingPolicy = JsonNamingPolicy.CamelCase};

        // Add the JSON string to the response headers
        response.Headers.Append("Pagination", JsonSerializer.Serialize(paginationHeader, jsonOptions));

        // Make a custom header accessible in th ebrowser
        response.Headers.Append("Access-Control-Expose-Headers", "Pagination");
    }
}
