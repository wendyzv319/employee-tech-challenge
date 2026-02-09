namespace EmployeeService.Api.Services;

public class ServiceResult
{
    public int StatusCode { get; init; }
    public string? Message { get; init; }

    public static ServiceResult NoContent() => new() { StatusCode = 204 };
    public static ServiceResult BadRequest(string msg) => new() { StatusCode = 400, Message = msg };
    public static ServiceResult Unauthorized(string msg) => new() { StatusCode = 401, Message = msg };
    public static ServiceResult Forbidden(string msg) => new() { StatusCode = 403, Message = msg };
    public static ServiceResult NotFound(string msg = "Not found.") => new() { StatusCode = 404, Message = msg };
    public static ServiceResult Conflict(string msg) => new() { StatusCode = 409, Message = msg };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static ServiceResult<T> Ok(T data) => new() { StatusCode = 200, Data = data };
    public static ServiceResult<T> Created(T data) => new() { StatusCode = 201, Data = data };
    public new static ServiceResult<T> BadRequest(string msg) => new() { StatusCode = 400, Message = msg };
    public new static ServiceResult<T> Unauthorized(string msg) => new() { StatusCode = 401, Message = msg };
    public new static ServiceResult<T> Forbidden(string msg) => new() { StatusCode = 403, Message = msg };
    public new static ServiceResult<T> NotFound(string msg = "Not found.") => new() { StatusCode = 404, Message = msg };
    public new static ServiceResult<T> Conflict(string msg) => new() { StatusCode = 409, Message = msg };
}
