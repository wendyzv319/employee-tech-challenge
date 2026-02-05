using EmployeeService.Api.Auth;
using EmployeeService.Api.Data;
using EmployeeService.Api.Dtos;
using EmployeeService.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(AppDbContext db, IPasswordHasher hasher, ILogger<EmployeesController> logger)
    {
        _db = db;
        _hasher = hasher;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all registered employees.
    /// </summary>
    /// <remarks>
    /// Requires a valid JWT (Authorize).
    /// Returns a list of employees including their phone numbers and manager information.
    /// </remarks>
    /// <response code="200">List of employees.</response>
    /// <response code="401">Unauthorized (missing or invalid token).</response>
    [HttpGet]
    public async Task<ActionResult<List<EmployeeResponseDto>>> GetAll()
    {
        var employees = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Phones)
            .Include(e => e.Manager)
            .ToListAsync();

        return Ok(employees.Select(MapToResponse).ToList());
    }

    /// <summary>
    /// Retrieves an employee by document number.
    /// </summary>
    /// <remarks>
    /// Requires a valid JWT (Authorize).
    /// </remarks>
    /// <param name="documentNumber">Employee document number.</param>
    /// <response code="200">Employee found.</response>
    /// <response code="404">Employee not found.</response>
    /// <response code="401">Unauthorized (missing or invalid token).</response>
    [HttpGet("{documentNumber:int}")]
    public async Task<ActionResult<EmployeeResponseDto>> GetById(int documentNumber)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Phones)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.DocumentNumber == documentNumber);

        return employee is null ? NotFound() : Ok(MapToResponse(employee));
    }

    // POST /api/employees
    /// <summary>
    /// Creates a new employee.
    /// </summary>
    /// <remarks>
    /// Requires a valid JWT (Authorize).
    ///
    /// Business rules:
    /// - You cannot create a user with a higher role than your own.
    /// - An employee must have at least two phone numbers.
    /// - DocumentNumber and Email must be unique.
    /// - The employee must be 18 years old or older.
    /// - If a manager is provided, the manager must exist and have an equal or higher role.
    ///
    /// Example request:
    /// {
    ///   "documentNumber": 2001,
    ///   "firstName": "Carlos",
    ///   "lastName": "Silva",
    ///   "email": "carlos@demo.com",
    ///   "birthDate": "1990-01-01",
    ///   "gender": 2,
    ///   "managerDocumentNumber": 1001,
    ///   "role": 1,
    ///   "phones": [11911111111, 11922222222],
    ///   "password": "Carlos@123"
    /// }
    /// </remarks>
    /// <response code="201">Employee created successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="403">Forbidden (role restriction).</response>
    /// <response code="409">Conflict (DocumentNumber or Email already exists).</response>

    [HttpPost]
    public async Task<ActionResult<EmployeeResponseDto>> Create([FromBody] CreateEmployeeDto dto)
    {
        _logger.LogInformation("Create employee requested by doc={Caller}", GetCallerDoc());

        var callerRole = GetCurrentRole();
        if ((int)dto.Role > (int)callerRole)
            return Forbid("You cannot create a user with higher permissions than yours.");

        if (dto.Phones is null || dto.Phones.Count < 2)
            return BadRequest("Employee must have more than one phone (min 2).");

        if (dto.Phones.Any(p => p <= 0))
            return BadRequest("Phones must be positive numbers.");

        if (IsMinor(dto.BirthDate))
            return BadRequest("Employee must not be a minor (must be 18+).");

        if (await _db.Employees.AnyAsync(e => e.DocumentNumber == dto.DocumentNumber))
            return Conflict("DocumentNumber already exists.");

        if (await _db.Employees.AnyAsync(e => e.Email == dto.Email))
            return Conflict("Email already exists.");



        int? managerId = null;
        if (dto.ManagerDocumentNumber.HasValue)
        {
            if (dto.ManagerDocumentNumber.Value == dto.DocumentNumber)
                return BadRequest("ManagerDocumentNumber cannot be the same as DocumentNumber.");

            var manager = await _db.Employees.FirstOrDefaultAsync(e => e.DocumentNumber == dto.ManagerDocumentNumber.Value);
            if (manager is null)
                return BadRequest("ManagerDocumentNumber does not exist.");

            if ((int)manager.Role < (int)dto.Role)
                return BadRequest("Manager must have an equal or higher role than the employee.");

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

        return CreatedAtAction(nameof(GetById), new { documentNumber = created.DocumentNumber }, MapToResponse(created));
    }

    /// <summary>
    /// Updates an existing employee.
    /// </summary>
    /// <remarks>
    /// Requires a valid JWT (Authorize).
    ///
    /// Business rules:
    /// - An employee must have at least two phone numbers.
    /// - The employee must be 18 years old or older.
    /// - Email must remain unique.
    /// - You cannot assign a role higher than your own.
    /// - If a manager is provided, the manager must exist and have an equal or higher role.
    /// - Password is optional during update.
    /// </remarks>
    /// <param name="documentNumber">Employee document number.</param>
    /// <response code="200">Employee updated successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="403">Forbidden (role restriction).</response>
    /// <response code="404">Employee not found.</response>
    /// <response code="409">Conflict (Email already exists).</response>
    [HttpPut("{documentNumber:int}")]
    public async Task<ActionResult<EmployeeResponseDto>> Update(int documentNumber, [FromBody] UpdateEmployeeDto dto)
    {
        _logger.LogInformation("Update employee doc={Doc} requested by caller={Caller}", documentNumber, GetCallerDoc());

        if (dto.Phones is null || dto.Phones.Count < 2)
            return BadRequest("Employee must have more than one phone (min 2).");

        if (dto.Phones.Any(p => p <= 0))
            return BadRequest("Phones must be positive numbers.");

        if (IsMinor(dto.BirthDate))
            return BadRequest("Employee must not be a minor (must be 18+).");

        var employee = await _db.Employees
            .Include(e => e.Phones)
            .FirstOrDefaultAsync(e => e.DocumentNumber == documentNumber);

        if (employee is null)
            return NotFound();

        var callerRole = GetCurrentRole();
        if ((int)dto.Role > (int)callerRole)
            return Forbid("You cannot assign a higher role than yours.");

        // unique email
        var email = dto.Email.Trim();
        var emailExists = await _db.Employees.AnyAsync(e => e.Email == email && e.Id != employee.Id);
        if (emailExists)
            return Conflict("Email already exists.");

        int? managerId = null;
        if (dto.ManagerDocumentNumber.HasValue)
        {
            if (dto.ManagerDocumentNumber.Value == documentNumber)
                return BadRequest("ManagerDocumentNumber cannot be the same as DocumentNumber.");

            var manager = await _db.Employees.FirstOrDefaultAsync(e => e.DocumentNumber == dto.ManagerDocumentNumber.Value);
            if (manager is null)
                return BadRequest("ManagerDocumentNumber does not exist.");

            if ((int)manager.Role < (int)dto.Role)
                return BadRequest("Manager must have an equal or higher role than the employee.");

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

        // replace phones
        var newPhones = dto.Phones.Distinct().ToHashSet();

        employee.Phones.RemoveAll(p => !newPhones.Contains(p.PhoneNumber));

        var existing = employee.Phones.Select(p => p.PhoneNumber).ToHashSet();
        foreach (var phone in newPhones)
        {
            if (!existing.Contains(phone))
                employee.Phones.Add(new EmployeePhone { PhoneNumber = phone, EmployeeId = employee.Id });
        }

        await _db.SaveChangesAsync();

        var updated = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Phones)
            .Include(e => e.Manager)
            .FirstAsync(e => e.Id == employee.Id);

        return Ok(MapToResponse(updated));
    }

    // DELETE /api/employees/{documentNumber}
    /// <summary>
    /// Deletes an employee.
    /// </summary>
    /// <remarks>
    /// Requires a valid JWT (Authorize).
    ///
    /// Business rule:
    /// - You cannot delete a user with a higher role than your own.
    /// </remarks>
    /// <param name="documentNumber">Employee document number.</param>
    /// <response code="204">Employee deleted successfully.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="403">Forbidden (role restriction).</response>
    /// <response code="404">Employee not found.</response>
    [HttpDelete("{documentNumber:int}")]
    public async Task<IActionResult> Delete(int documentNumber)
    {
        _logger.LogInformation("Delete employee doc={Doc} requested by caller={Caller}", documentNumber, GetCallerDoc());

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.DocumentNumber == documentNumber);
        if (employee is null)
            return NotFound();

        var callerRole = GetCurrentRole();
        if ((int)employee.Role > (int)callerRole)
            return Forbid("You cannot delete a user with higher permissions than yours.");

        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();

        return NoContent();
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

    private EmployeeRole GetCurrentRole()
    {
        var roleStr = User.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<EmployeeRole>(roleStr, out var role) ? role : EmployeeRole.Employee;
    }

    private int? GetCallerDoc()
    {
        var docStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(docStr, out var doc) ? doc : null;
    }

    private static bool IsMinor(DateTime birthDate)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Date.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age < 18;
    }
}
