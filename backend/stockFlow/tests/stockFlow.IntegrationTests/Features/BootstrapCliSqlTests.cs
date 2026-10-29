using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using stockFlow.IntegrationTests.Persistence;
using Xunit;
namespace stockFlow.IntegrationTests.Features;
[Trait("Category", "Sql")]
public sealed class BootstrapCliSqlTests
{
    [Fact] public async Task Real_cli_flag_creates_first_admin_and_exits_without_starting_web_server()
    {
        await using var database = new SqlTestDatabase(); await database.InitializeAsync();
        var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        var start = new ProcessStartInfo(host) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(stockFlow.Program).Assembly.Location); start.ArgumentList.Add("--bootstrap-admin");
        start.Environment["ConnectionStrings__DefaultConnection"] = database.ConnectionString;
        start.Environment["STOCKFLOW_BOOTSTRAP_EMAIL"] = "cli@example.test";
        start.Environment["STOCKFLOW_BOOTSTRAP_PASSWORD"] = BackendFixture.Password;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start dotnet CLI.");
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        var output = await stdout + await stderr;
        Assert.True(process.ExitCode == 0, "CLI failed: " + output);
        Assert.DoesNotContain(BackendFixture.Password, output);
        await using var db = database.CreateContext(); var admin = await db.Users.SingleAsync();
        Assert.Equal("CLI@EXAMPLE.TEST", admin.Email); Assert.Equal("Admin", admin.Role);
        Assert.NotEqual(BackendFixture.Password, admin.PasswordHash);
        Assert.Empty(await db.AuthSessions.ToListAsync());
    }
}
