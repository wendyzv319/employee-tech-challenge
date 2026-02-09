using EmployeeService.Api.Dtos;
using EmployeeService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _service;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService service, ILogger<AuthController> logger)
    {
        _service = service;
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
    public Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("Register endpoint called doc={Doc} email={Email}", request.DocumentNumber, request.Email);
        return _service.Register(request, User);
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
    public Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login endpoint called doc={Doc}", request.DocumentNumber);
        return _service.Login(request);
    }
}
