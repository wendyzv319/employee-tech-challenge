namespace EmployeeService.Api.Entities;

public enum EmployeeRole
{
    Employee = 1,
    Leader = 2,
    Director = 3
}

public enum Gender
{
    Unspecified = 0,
    Female = 1,
    Male = 2
}

public class Employee
{
    public int Id { get; set; }  // PK autogenerada

    public int DocumentNumber { get; set; } // UNIQUE

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime BirthDate { get; set; }

    public Gender Gender { get; set; } = Gender.Unspecified;
    public EmployeeRole Role { get; set; } = EmployeeRole.Employee;
    
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    
    public string Password { get; set; } = string.Empty;

    public List<EmployeePhone> Phones { get; set; } = new();
}
