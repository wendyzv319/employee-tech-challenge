using EmployeeService.Api.Auth;
using EmployeeService.Api.Data;
using EmployeeService.Api.Dtos;
using EmployeeService.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeService.Api.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly JwtTokenService _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IPasswordHasher hasher, JwtTokenService jwt, ILogger<AuthService> logger)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, ClaimsPrincipal user)
    {
        _logger.LogInformation("Register attempt doc={Doc} email={Email}", request.DocumentNumber, request.Email);

        // validations
        ValidatePhonesOrFail(request.Phones, out var phonesError);
        if (phonesError is not null)
            return new BadRequestObjectResult(phonesError);

        if (IsMinor(request.BirthDate))
            return new BadRequestObjectResult("Employee must not be a minor (must be 18+).");

        if (request.ManagerDocumentNumber.HasValue && request.ManagerDocumentNumber.Value == request.DocumentNumber)
            return new BadRequestObjectResult("ManagerDocumentNumber cannot be the same as DocumentNumber.");

        if (await _db.Employees.AnyAsync(e => e.DocumentNumber == request.DocumentNumber))
            return new ConflictObjectResult("DocumentNumber already exists.");

        if (await _db.Employees.AnyAsync(e => e.Email == request.Email))
            return new ConflictObjectResult("Email already exists.");

        int? managerId = null;
        if (request.ManagerDocumentNumber.HasValue)
        {
            var manager = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.DocumentNumber == request.ManagerDocumentNumber.Value);

            if (manager is null)
                return new BadRequestObjectResult("ManagerDocumentNumber does not exist.");

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
            if (!(user.Identity?.IsAuthenticated ?? false))
                return new UnauthorizedObjectResult("Register requires authentication after the first user.");

            var currentRole = GetCurrentRole(user);
            if ((int)request.Role > (int)currentRole)
                return Forbidden("You cannot create a user with higher permissions than yours.");

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

        return new OkObjectResult(new AuthResponse
        {
            Token = token,
            DocumentNumber = employee.DocumentNumber,
            Email = employee.Email,
            Role = employee.Role
        });
    }

    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        _logger.LogInformation("Login attempt doc={Doc}", request.DocumentNumber);

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.DocumentNumber == request.DocumentNumber);

        if (employee is null)
            return new UnauthorizedObjectResult("Invalid credentials.");

        if (!_hasher.Verify(request.Password, employee.Password))
            return new UnauthorizedObjectResult("Invalid credentials.");

        var token = _jwt.GenerateToken(employee);

        _logger.LogInformation("Login success doc={Doc} role={Role}", employee.DocumentNumber, employee.Role);

        return new OkObjectResult(new AuthResponse
        {
            Token = token,
            DocumentNumber = employee.DocumentNumber,
            Email = employee.Email,
            Role = employee.Role
        });
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

    private static EmployeeRole GetCurrentRole(ClaimsPrincipal user)
    {
        var roleStr = user.FindFirstValue(ClaimTypes.Role);
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
