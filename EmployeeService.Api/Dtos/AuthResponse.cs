using EmployeeService.Api.Entities;

namespace EmployeeService.Api.Dtos;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public int DocumentNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public EmployeeRole Role { get; set; }
}
