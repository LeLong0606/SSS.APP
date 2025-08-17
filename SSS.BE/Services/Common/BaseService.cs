using SSS.BE.Models.Employee;

namespace SSS.BE.Services.Common;

/// <summary>
/// Base service interface for common operations
/// </summary>
public interface IBaseService
{
    Task<ApiResponse<T>> HandleOperationAsync<T>(Func<Task<T>> operation, string operationName);
    Task<PagedResponse<T>> HandlePagedOperationAsync<T>(Func<Task<(List<T> data, int totalCount)>> operation, int pageNumber, int pageSize, string operationName);
}

/// <summary>
/// Base service implementation with common error handling
/// </summary>
public abstract class BaseService : IBaseService
{
    protected readonly ILogger _logger;

    protected BaseService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<ApiResponse<T>> HandleOperationAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            var result = await operation();
            return new ApiResponse<T>
            {
                Success = true,
                Message = $"{operationName} completed successfully",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {OperationName}", operationName);
            return new ApiResponse<T>
            {
                Success = false,
                Message = $"An error occurred during {operationName}",
                Errors = new List<string> { "Please try again later" }
            };
        }
    }

    public async Task<PagedResponse<T>> HandlePagedOperationAsync<T>(
        Func<Task<(List<T> data, int totalCount)>> operation, 
        int pageNumber, 
        int pageSize, 
        string operationName)
    {
        try
        {
            var (data, totalCount) = await operation();
            return new PagedResponse<T>
            {
                Success = true,
                Message = $"{operationName} completed successfully",
                Data = data,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {OperationName}", operationName);
            return new PagedResponse<T>
            {
                Success = false,
                Message = $"An error occurred during {operationName}",
                Errors = new List<string> { "Please try again later" },
                Data = new List<T>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}