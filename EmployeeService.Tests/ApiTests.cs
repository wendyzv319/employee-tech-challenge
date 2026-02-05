using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace EmployeeService.Tests;

public class ApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public ApiTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("http://localhost")
        });

        _output = output;
    }

    // ---------- AUTH ----------

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        LogTest(nameof(Login_WithValidCredentials_ReturnsToken),
            "Return 200 OK and valid JWT");

        var body = new { documentNumber = 1001, password = "Ana@123456" };

        var res = await _client.PostAsJsonAsync("/api/Auth/login", body);

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await res.Content.ReadAsStringAsync();
        json.Should().Contain("token");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        LogTest(nameof(Login_WithInvalidCredentials_Returns401),
            "Return 401 if wrong password");

        var body = new { documentNumber = 1001, password = "wrong" };

        var res = await _client.PostAsJsonAsync("/api/Auth/login", body);

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- EMPLOYEES ----------

    [Fact]
    public async Task GetEmployees_WithoutToken_Returns401()
    {
        LogTest(nameof(GetEmployees_WithoutToken_Returns401),
            "Return 401 when no JWT is provided");

        var res = await _client.GetAsync("/api/Employees");

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateEmployee_Minor_Returns400()
    {
        LogTest(nameof(CreateEmployee_Minor_Returns400),
            "Reject employees under 18 (minor)");

        var token = await LoginAndGetToken();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var body = new
        {
            documentNumber = 2001,
            firstName = "Kid",
            lastName = "Test",
            email = "kid@test.com",
            birthDate = DateTime.UtcNow.AddYears(-10).ToString("yyyy-MM-ddTHH:mm:ss"),
            gender = 1,
            role = 1,
            managerDocumentNumber = (int?)null,
            phones = new[] { 11911111111L, 11822222222L },
            password = "123456"
        };

        var res = await _client.PostAsJsonAsync("/api/Employees", body);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var msg = await res.Content.ReadAsStringAsync();
        msg.Should().Contain("minor");
    }

    [Fact]
    public async Task CreateEmployee_ManagerEqualsSelf_Returns400()
    {
        LogTest(nameof(CreateEmployee_ManagerEqualsSelf_Returns400),
            "Manager cannot be the same employee");

        var token = await LoginAndGetToken();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var body = new
        {
            documentNumber = 2002,
            firstName = "Self",
            lastName = "Manager",
            email = "selfmanager@test.com",
            birthDate = DateTime.UtcNow.AddYears(-30).ToString("yyyy-MM-ddTHH:mm:ss"),
            gender = 1,
            role = 1,
            managerDocumentNumber = 2002,
            phones = new[] { 11933333333L, 11844444444L },
            password = "123456"
        };

        var res = await _client.PostAsJsonAsync("/api/Employees", body);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var msg = await res.Content.ReadAsStringAsync();
        msg.Should().Contain("ManagerDocumentNumber");
    }
    

    private async Task<string> LoginAndGetToken()
    {
        _output.WriteLine("Helper: LoginAndGetToken → login válido y extracción del JWT");

        var body = new { documentNumber = 1001, password = "Ana@123456" };

        var res = await _client.PostAsJsonAsync("/api/Auth/login", body);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var token = doc.RootElement.GetProperty("token").GetString();

        token.Should().NotBeNullOrWhiteSpace();
        return token!;
    }

    [Fact]
    public async Task CreateEmployee_ManagerHasLowerRole_Returns400()
    {
        _output.WriteLine("TEST: CreateEmployee_ManagerHasLowerRole_Returns400");
        _output.WriteLine("EXPECTS: Returns 400 when manager role is lower than the new employee role");
        _output.WriteLine("SETUP: Create a low-role manager first, then create a higher-role employee using that manager");

        var token = await LoginAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1) Create a manager with a LOWER role (e.g., Employee = 1)
        var managerBody = new
        {
            documentNumber = 3101,
            firstName = "Low",
            lastName = "Manager",
            email = "low.manager.3101@test.com",
            birthDate = DateTime.UtcNow.AddYears(-30).ToString("yyyy-MM-ddTHH:mm:ss"),
            gender = 1,
            role = 1, // lower role
            managerDocumentNumber = (int?)null,
            phones = new[] { 11911111111L, 11822222222L },
            password = "123456"
        };

        var managerRes = await _client.PostAsJsonAsync("/api/Employees", managerBody);

        managerRes.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);
       
        var employeeBody = new
        {
            documentNumber = 3102,
            firstName = "Higher",
            lastName = "Role",
            email = "higher.role.3102@test.com",
            birthDate = DateTime.UtcNow.AddYears(-25).ToString("yyyy-MM-ddTHH:mm:ss"),
            gender = 1,
            role = 2, // higher role than manager
            managerDocumentNumber = 3101,
            phones = new[] { 11933333333L, 11844444444L },
            password = "123456"
        };

        var res = await _client.PostAsJsonAsync("/api/Employees", employeeBody);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var msg = await res.Content.ReadAsStringAsync();
        msg.Should().Contain("Manager must have an equal or higher role");
    }

    [Fact]
    public async Task CreateEmployee_WithLessThanTwoPhones_Returns400()
    {
        LogTest(nameof(CreateEmployee_WithLessThanTwoPhones_Returns400),
            "Return 400 BadRequest when employee has less than two phones");

        var token = await LoginAndGetToken();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var body = new
        {
            documentNumber = 4001,
            firstName = "One",
            lastName = "Phone",
            email = "one.phone.4001@test.com",
            birthDate = DateTime.UtcNow.AddYears(-25).ToString("yyyy-MM-ddTHH:mm:ss"),
            gender = 1,
            role = 1,
            managerDocumentNumber = (int?)null,
            phones = new[] { 11999999999L }, // only one phone
            password = "123456"
        };

        var res = await _client.PostAsJsonAsync("/api/Employees", body);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var msg = await res.Content.ReadAsStringAsync();
        msg.Should().Contain("validation errors occurred")
           .And.Contain("Phones")
           .And.Contain("minimum length of '2'");
    }


    [Fact]
    public async Task CreateEmployee_WithInvalidPhoneNumbers_Returns400()
    {
        LogTest(nameof(CreateEmployee_WithInvalidPhoneNumbers_Returns400),
            "Return 400 BadRequest when phones contain zero or negative values");

        var token = await LoginAndGetToken();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var body = new
        {
            documentNumber = 4002,
            firstName = "Invalid",
            lastName = "Phone",
            email = "invalid.phone.4002@test.com",
            birthDate = DateTime.UtcNow.AddYears(-30).ToString("yyyy-MM-ddTHH:mm:ss"),
            gender = 1,
            role = 1,
            managerDocumentNumber = (int?)null,
            phones = new[] { 0L, -123L }, // invalid phones
            password = "123456"
        };

        var res = await _client.PostAsJsonAsync("/api/Employees", body);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var msg = await res.Content.ReadAsStringAsync();
        msg.Should().Contain("positive");
    }


    [Fact]
    public async Task CreateEmployee_DuplicateEmail_Returns409()
    {
        LogTest(nameof(CreateEmployee_DuplicateEmail_Returns409),
            "Return 409 Conflict when email already exists");

        var token = await LoginAndGetToken();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var email = "duplicate.email@test.com";

        // First employee
        var firstBody = new
        {
            documentNumber = 4003,
            firstName = "First",
            lastName = "User",
            email = email,
            birthDate = DateTime.UtcNow.AddYears(-30).ToString("yyyy-MM-ddTHH:mm:ss"),
            gender = 1,
            role = 1,
            managerDocumentNumber = (int?)null,
            phones = new[] { 11911111111L, 11822222222L },
            password = "123456"
        };

        var firstRes = await _client.PostAsJsonAsync("/api/Employees", firstBody);
        firstRes.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);

        // Second employee with same email
        var secondBody = new
        {
            documentNumber = 4004,
            firstName = "Second",
            lastName = "User",
            email = email,
            birthDate = DateTime.UtcNow.AddYears(-25).ToString("yyyy-MM-ddTHH:mm:ss"),
            gender = 1,
            role = 1,
            managerDocumentNumber = (int?)null,
            phones = new[] { 11933333333L, 11844444444L },
            password = "123456"
        };

        var res = await _client.PostAsJsonAsync("/api/Employees", secondBody);

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var msg = await res.Content.ReadAsStringAsync();
        msg.Should().Contain("Email already exists");
    }



    private void LogTest(string testName, string description)
    {
        _output.WriteLine("");
        _output.WriteLine($"TEST: {testName}");
        _output.WriteLine($"EXPECTS: {description}");
    }
}
