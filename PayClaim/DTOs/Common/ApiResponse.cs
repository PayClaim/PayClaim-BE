namespace PayClaim.DTOs.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Request processed successfully.")
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = 200,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Created(T data, string message = "Resource created successfully.")
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = 201,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string message, int statusCode = 400, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Data = default,
            Errors = errors
        };
    }
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "Request processed successfully.")
    {
        return new ApiResponse
        {
            Success = true,
            StatusCode = 200,
            Message = message,
            Data = null
        };
    }

    public static new ApiResponse Fail(string message, int statusCode = 400, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Data = null,
            Errors = errors
        };
    }
}
