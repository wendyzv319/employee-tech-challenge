using System.ComponentModel.DataAnnotations;

namespace EmployeeService.Api.Dtos;

public class LoginRequest
{
    [Required]
    public int DocumentNumber { get; set; }

    [Required]
    public string Password { get; set; } = string.Empty;
}
