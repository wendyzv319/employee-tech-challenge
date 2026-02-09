using EmployeeService.Api.Auth;
using EmployeeService.Api.Data;
using EmployeeService.Api.Dtos;
using EmployeeService.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Api.Services;

public class EmployeesService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<EmployeesService> _logger;

    public EmployeesService(AppDbContext db, IPasswordHasher hasher, ILogger<EmployeesService> logger)
    {
        _db = db;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task<ActionResult<List<EmployeeResponseDto>>> GetAll()
    {
        var employees = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Phones)
            .Include(e => e.Manager)
            .ToListAsync();

        return new OkObjectResult(employees.Select(MapToResponse).ToList());
    }

    public async Task<ActionResult<EmployeeResponseDto>> GetById(int documentNumber)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Phones)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.DocumentNumber == documentNumber);

        return employee is null
            ? new NotFoundResult()
            : new OkObjectResult(MapToResponse(employee));
    }

    public async Task<ActionResult<EmployeeResponseDto>> Create(CreateEmployeeDto dto, EmployeeRole callerRole, int? callerDoc)
    {
        _logger.LogInformation("Create employee requested by doc={Caller}", callerDoc);

        if ((int)dto.Role > (int)callerRole)
            return Forbidden("You cannot create a user with higher permissions than yours.");

        ValidatePhonesOrFail(dto.Phones, out var phonesError);
        if (phonesError is not null)
            return new BadRequestObjectResult(phonesError);

        if (IsMinor(dto.BirthDate))
            return new BadRequestObjectResult("Employee must not be a minor (must be 18+).");

        if (await _db.Employees.AnyAsync(e => e.DocumentNumber == dto.DocumentNumber))
            return new ConflictObjectResult("DocumentNumber already exists.");

        if (await _db.Employees.AnyAsync(e => e.Email == dto.Email))
            return new ConflictObjectResult("Email already exists.");

        int? managerId = null;
        if (dto.ManagerDocumentNumber.HasValue)
        {
            if (dto.ManagerDocumentNumber.Value == dto.DocumentNumber)
                return new BadRequestObjectResult("ManagerDocumentNumber cannot be the same as DocumentNumber.");

            var manager = await _db.Employees
                .FirstOrDefaultAsync(e => e.DocumentNumber == dto.ManagerDocumentNumber.Value);

            if (manager is null)
                return new BadRequestObjectResult("ManagerDocumentNumber does not exist.");

            if ((int)manager.Role < (int)dto.Role)
                return new BadRequestObjectResult("Manager must have an equal or higher role than the employee.");

            managerId = manager.Id;
        }

        var employee = new Employee
        {
            DocumentNumber = dto.DocumentNumber,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            BirthDate = dto.BirthDate,
            Gender = dto.Gender,
            Role = dto.Role,
            ManagerId = managerId,
            Password = _hasher.Hash(dto.Password),
        };

        employee.Phones = dto.Phones
            .Distinct()
            .Select(p => new EmployeePhone { PhoneNumber = p, Employee = employee })
            .ToList();

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        var created = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Phones)
            .Include(e => e.Manager)
            .FirstAsync(e => e.Id == employee.Id);

        return new CreatedAtActionResult(
            actionName: "GetById",
            controllerName: "Employees",
            routeValues: new { documentNumber = created.DocumentNumber },
            value: MapToResponse(created)
        );
    }

    public async Task<ActionResult<EmployeeResponseDto>> Update(UpdateEmployeeDto dto, EmployeeRole callerRole, int? callerDoc)
    {
        _logger.LogInformation("Update employee doc={Doc} requested by caller={Caller}", dto.DocumentNumber, callerDoc);

        var employee = await _db.Employees
            .Include(e => e.Phones)
            .FirstOrDefaultAsync(e => e.DocumentNumber == dto.DocumentNumber);

        if (employee is null)
            return new NotFoundResult();
        
        if ((int)dto.Role > (int)callerRole)
            return Forbidden("You cannot assign a higher role than yours.");
       
        ValidatePhonesOrFail(dto.Phones, out var phonesError);
        if (phonesError is not null)
            return new BadRequestObjectResult(phonesError);

        if (IsMinor(dto.BirthDate))
            return new BadRequestObjectResult("Employee must not be a minor (must be 18+).");

        var email = dto.Email.Trim();
        var emailExists = await _db.Employees.AnyAsync(e => e.Email == email && e.Id != employee.Id);
        if (emailExists)
            return new ConflictObjectResult("Email already exists.");

        int? managerId = null;
        if (dto.ManagerDocumentNumber.HasValue)
        {
            if (dto.ManagerDocumentNumber.Value == dto.DocumentNumber)
                return new BadRequestObjectResult("ManagerDocumentNumber cannot be the same as DocumentNumber.");

            var manager = await _db.Employees
                .FirstOrDefaultAsync(e => e.DocumentNumber == dto.ManagerDocumentNumber.Value);

            if (manager is null)
                return new BadRequestObjectResult("ManagerDocumentNumber does not exist.");

            if ((int)manager.Role < (int)dto.Role)
                return new BadRequestObjectResult("Manager must have an equal or higher role than the employee.");

            managerId = manager.Id;
        }

        employee.FirstName = dto.FirstName.Trim();
        employee.LastName = dto.LastName.Trim();
        employee.Email = email;
        employee.BirthDate = dto.BirthDate;
        employee.Gender = dto.Gender;
        employee.Role = dto.Role;
        employee.ManagerId = managerId;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            employee.Password = _hasher.Hash(dto.Password);

        ReplacePhones(employee, dto.Phones);

        await _db.SaveChangesAsync();

        var updated = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Phones)
            .Include(e => e.Manager)
            .FirstAsync(e => e.Id == employee.Id);

        return new OkObjectResult(MapToResponse(updated));
    }

    public async Task<IActionResult> Delete(int documentNumber, EmployeeRole callerRole, int? callerDoc)
    {
        _logger.LogInformation("Delete employee doc={Doc} requested by caller={Caller}", documentNumber, callerDoc);

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.DocumentNumber == documentNumber);

        if (employee is null)
            return new NotFoundResult();

        if ((int)employee.Role > (int)callerRole)
            return Forbidden("You cannot delete a user with higher permissions than yours.");

        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();

        return new NoContentResult();
    }

    private static ObjectResult Forbidden(string message)
        => new(message) { StatusCode = 403 };

    private static void ValidatePhonesOrFail(List<long>? phones, out string? error)
    {
        if (phones is null || phones.Count < 2)
        {
            error = "Employee must have more than one phone (min 2).";
            return;
        }

        if (phones.Any(p => p <= 0))
        {
            error = "Phones must be positive numbers.";
            return;
        }

        error = null;
    }

    private static void ReplacePhones(Employee employee, List<long> phones)
    {
        var newPhones = phones.Distinct().ToHashSet();

        employee.Phones.RemoveAll(p => !newPhones.Contains(p.PhoneNumber));

        var existing = employee.Phones.Select(p => p.PhoneNumber).ToHashSet();
        foreach (var phone in newPhones)
        {
            if (!existing.Contains(phone))
                employee.Phones.Add(new EmployeePhone { PhoneNumber = phone, EmployeeId = employee.Id });
        }
    }

    private static EmployeeResponseDto MapToResponse(Employee e)
    {
        return new EmployeeResponseDto
        {
            Id = e.Id,
            DocumentNumber = e.DocumentNumber,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            BirthDate = e.BirthDate,
            Gender = e.Gender,
            Role = e.Role,
            ManagerDocumentNumber = e.Manager?.DocumentNumber,
            ManagerName = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}" : null,
            Phones = e.Phones.OrderBy(p => p.Id).Select(p => p.PhoneNumber).ToList()
        };
    }

    private static bool IsMinor(DateTime birthDate)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Date.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age < 18;
    }
}
