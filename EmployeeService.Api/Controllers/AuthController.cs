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
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly JwtTokenService _jwt;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext db, IPasswordHasher hasher, JwtTokenService jwt, ILogger<AuthController> logger)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user (Employee) and returns a JWT.
    /// </summary>
    /// <remarks>
    /// - If this is the first user in the system, registration is allowed without authentication
    ///   and the role will be automatically set to <b>Director</b> (bootstrap scenario).
    /// - If at least one user already exists, authentication is required and role-based
    ///   permission rules apply:
    ///   you cannot create a user with a higher role than your own.
    ///
    /// Main rules:
    /// - DocumentNumber and Email must be unique.
    /// - The employee must have at least two phone numbers.
    /// - The employee must be 18 years old or older.
    /// - ManagerDocumentNumber (optional) must reference an existing employee
    ///   and cannot be the same as the employee being created.
    ///
    /// Example request:
    /// {
    ///   "documentNumber": 1001,
    ///   "firstName": "Ana",
    ///   "lastName": "Perez",
    ///   "email": "ana@demo.com",
    ///   "birthDate": "1995-02-10",
    ///   "gender": 2,
    ///   "managerDocumentNumber": null,
    ///   "role": 3,
    ///   "phones": [11999999999, 11888888888],
    ///   "password": "Ana@123456"
    /// }
    /// </remarks>
    /// <response code="200">
    /// User successfully created. Returns a JWT token and basic user information.
    /// </response>
    /// <response code="400">
    /// Validation error (e.g., insufficient phones, underage employee, invalid manager).
    /// </response>
    /// <response code="401">
    /// Unauthorized when this is not the first user and no JWT is provided.
    /// </response>
    /// <response code="403">
    /// Forbidden when attempting to create a user with a higher role than the current user.
    /// </response>
    /// <response code="409">
    /// Conflict when DocumentNumber or Email already exists.
    /// </response>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("Register attempt doc={Doc} email={Email}", request.DocumentNumber, request.Email);

        if (request.Phones is null || request.Phones.Count < 2)
            return BadRequest("Employee must have more than one phone (min 2).");

        if (request.Phones.Any(p => p <= 0))
            return BadRequest("Phones must be positive numbers.");

        if (IsMinor(request.BirthDate))
            return BadRequest("Employee must not be a minor (must be 18+).");

        if (request.ManagerDocumentNumber.HasValue && request.ManagerDocumentNumber.Value == request.DocumentNumber)
            return BadRequest("ManagerDocumentNumber cannot be the same as DocumentNumber.");

        if (await _db.Employees.AnyAsync(e => e.DocumentNumber == request.DocumentNumber))
            return Conflict("DocumentNumber already exists.");

        if (await _db.Employees.AnyAsync(e => e.Email == request.Email))
            return Conflict("Email already exists.");
       
        int? managerId = null;
        if (request.ManagerDocumentNumber.HasValue)
        {
            var manager = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.DocumentNumber == request.ManagerDocumentNumber.Value);

            if (manager is null)
                return BadRequest("ManagerDocumentNumber does not exist.");

            managerId = manager.Id;
        }

        var anyUser = await _db.Employees.AnyAsync();
        EmployeeRole finalRole;

        if (!anyUser)
        {
            finalRole = EmployeeRole.Director;
            _logger.LogWarning("Bootstrap register: first user will be created as Director.");
        }
        else
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
                return Unauthorized("Register requires authentication after the first user.");

            var currentRole = GetCurrentRole();
            if ((int)request.Role > (int)currentRole)
                return Forbid("You cannot create a user with higher permissions than yours.");

            finalRole = request.Role;
        }

        var employee = new Employee
        {
            DocumentNumber = request.DocumentNumber,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            BirthDate = request.BirthDate,
            Gender = request.Gender,
            Role = finalRole,
            ManagerId = managerId,
            Password = _hasher.Hash(request.Password),
        };
       
        employee.Phones = request.Phones
            .Distinct()
            .Select(p => new EmployeePhone
            {
                PhoneNumber = p,
                Employee = employee
            })
            .ToList();

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User registered id={Id} doc={Doc} role={Role}", employee.Id, employee.DocumentNumber, employee.Role);

        var token = _jwt.GenerateToken(employee);

        return Ok(new AuthResponse
        {
            Token = token,
            DocumentNumber = employee.DocumentNumber,
            Email = employee.Email,
            Role = employee.Role
        });
    }

    /// <summary>
    /// Authenticates a user and returns a JWT.
    /// </summary>
    /// <remarks>
    /// Authentication is performed using <b>DocumentNumber</b> and <b>Password</b>.
    ///
    /// Example request:
    /// {
    ///   "documentNumber": 1001,
    ///   "password": "Ana@123456"
    /// }
    /// </remarks>
    /// <response code="200">
    /// Login successful. Returns a JWT token.
    /// </response>
    /// <response code="401">
    /// Invalid credentials.
    /// </response>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt doc={Doc}", request.DocumentNumber);

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.DocumentNumber == request.DocumentNumber);
        if (employee is null)
            return Unauthorized("Invalid credentials.");

        if (!_hasher.Verify(request.Password, employee.Password))
            return Unauthorized("Invalid credentials.");

        var token = _jwt.GenerateToken(employee);

        _logger.LogInformation("Login success doc={Doc} role={Role}", employee.DocumentNumber, employee.Role);

        return Ok(new AuthResponse
        {
            Token = token,
            DocumentNumber = employee.DocumentNumber,
            Email = employee.Email,
            Role = employee.Role
        });
    }

    private EmployeeRole GetCurrentRole()
    {
        var roleStr = User.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<EmployeeRole>(roleStr, out var role) ? role : EmployeeRole.Employee;
    }

    private static bool IsMinor(DateTime birthDate)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Date.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age < 18;
    }
}
