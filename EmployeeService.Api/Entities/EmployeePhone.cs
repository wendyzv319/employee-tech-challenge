namespace EmployeeService.Api.Entities;

public class EmployeePhone
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }    
    public Employee Employee { get; set; } = null!;

    public long PhoneNumber { get; set; }
}
