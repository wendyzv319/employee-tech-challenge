using System.ComponentModel.DataAnnotations;
using EmployeeService.Api.Entities;

namespace EmployeeService.Api.Dtos;

public class UpdateEmployeeDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int DocumentNumber { get; set; }

    [Required, MinLength(1)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public DateTime BirthDate { get; set; }

    public Gender Gender { get; set; } = Gender.Unspecified;

    public int? ManagerDocumentNumber { get; set; }

    public EmployeeRole Role { get; set; } = EmployeeRole.Employee;

    [Required, MinLength(2)]
    public List<long> Phones { get; set; } = new();

    public string? Password { get; set; }
}
