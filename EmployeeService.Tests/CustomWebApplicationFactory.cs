using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace EmployeeService.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<EmployeeService.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseContentRoot(GetApiOutputPath());
        return base.CreateHost(builder);
    }

    private static string GetApiOutputPath()
    {
        var solutionRoot = Directory.GetCurrentDirectory();
        var repoRoot = Path.GetFullPath(Path.Combine(solutionRoot, "..", "..", "..", ".."));

        return Path.Combine(repoRoot, "EmployeeService.Api", "bin", "Debug", "net8.0");
    }
}
