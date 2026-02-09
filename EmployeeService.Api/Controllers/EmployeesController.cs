using EmployeeService.Api.Dtos;
using EmployeeService.Api.Entities;
using EmployeeService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EmployeeService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly EmployeesService _service;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(EmployeesService service, ILogger<EmployeesController> logger)
    {
        _service = service;
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
    public Task<ActionResult<List<EmployeeResponseDto>>> GetAll()
        => _service.GetAll();

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
    public Task<ActionResult<EmployeeResponseDto>> GetById(int documentNumber)
        => _service.GetById(documentNumber);

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
    public Task<ActionResult<EmployeeResponseDto>> Create([FromBody] CreateEmployeeDto dto)
    {
        _logger.LogInformation("Create employee requested by doc={Caller}", GetCallerDoc());
        return _service.Create(dto, GetCurrentRole(), GetCallerDoc());
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
    /// <response code="200">Employee updated successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="403">Forbidden (role restriction).</response>
    /// <response code="404">Employee not found.</response>
    /// <response code="409">Conflict (Email already exists).</response>
    [HttpPut]
    public Task<ActionResult<EmployeeResponseDto>> Update([FromBody] UpdateEmployeeDto dto)
    {
        _logger.LogInformation("Update employee doc={Doc} requested by caller={Caller}", dto.DocumentNumber, GetCallerDoc());
        return _service.Update(dto, GetCurrentRole(), GetCallerDoc());
    }

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
    public Task<IActionResult> Delete(int documentNumber)
    {
        _logger.LogInformation("Delete employee doc={Doc} requested by caller={Caller}", documentNumber, GetCallerDoc());
        return _service.Delete(documentNumber, GetCurrentRole(), GetCallerDoc());
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
}
