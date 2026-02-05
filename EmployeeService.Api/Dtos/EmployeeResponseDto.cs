using EmployeeService.Api.Entities;

namespace EmployeeService.Api.Dtos;

public class EmployeeResponseDto
{
    public int Id { get; set; }
    public int DocumentNumber { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public DateTime BirthDate { get; set; }

    public Gender Gender { get; set; }
    public EmployeeRole Role { get; set; }

    public int? ManagerDocumentNumber { get; set; }
    public string? ManagerName { get; set; } 

    public List<long> Phones { get; set; } = new();
}
