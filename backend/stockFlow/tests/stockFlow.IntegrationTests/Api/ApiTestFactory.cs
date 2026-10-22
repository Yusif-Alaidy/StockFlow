using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace stockFlow.IntegrationTests.Api;

public sealed class ApiTestFactory : WebApplicationFactory<stockFlow.Program>
{
    private readonly string _environment;
    private readonly HealthStatus _status;
    private readonly bool _exposeDocs;
    public ConcurrentQueue<string> Messages { get; } = new();

    public ApiTestFactory(string environment = "Production", HealthStatus status = HealthStatus.Healthy, bool exposeDocs = false)
    {
        _environment = environment; _status = status; _exposeDocs = exposeDocs;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(FindApiRoot());
        builder.UseEnvironment(_environment);
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=localhost;Database=NeverOpenedByApiTests;Integrated Security=True");
        builder.UseSetting("Api:ExposeDocumentation", _exposeDocs.ToString());
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:4200");
        builder.ConfigureTestServices(services =>
        {
            // Probe endpoints exist only in the test assembly; never copied to the API.
            services.AddControllers().AddApplicationPart(typeof(ProbeController).Assembly);
            services.RemoveAll<HealthCheckService>();
            services.AddSingleton<HealthCheckService>(new StubHealthService(_status));
            services.AddLogging(logging => logging.AddProvider(new CapturingLoggerProvider(Messages)));
        });
    }

    private static string FindApiRoot()
    {
        // API assembly is named stockFlow but its folder is stockFlow.Api.
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var candidate = Path.Combine(folder.FullName, "src", "stockFlow.Api");
            if (File.Exists(Path.Combine(candidate, "stockFlow.csproj"))) return candidate;
        }
        throw new InvalidOperationException("Cannot locate src/stockFlow.Api. Run tests from the repository checkout.");
    }

    public System.Net.Http.HttpClient NewClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });

    private sealed class StubHealthService : HealthCheckService
    {
        private readonly HealthStatus _status;
        public StubHealthService(HealthStatus status) => _status = status;
        public override Task<HealthReport> CheckHealthAsync(Func<HealthCheckRegistration, bool>? predicate,
            CancellationToken cancellationToken = default)
        {
            var entry = new HealthReportEntry(_status, "Test double", TimeSpan.Zero, null, new Dictionary<string, object>());
            return Task.FromResult(new HealthReport(new Dictionary<string, HealthReportEntry> { ["database"] = entry }, TimeSpan.Zero));
        }
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _messages;
        public CapturingLoggerProvider(ConcurrentQueue<string> messages) => _messages = messages;
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(_messages);
        public void Dispose() { }
        private sealed class CapturingLogger : ILogger
        {
            private readonly ConcurrentQueue<string> _messages;
            public CapturingLogger(ConcurrentQueue<string> messages) => _messages = messages;
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) => _messages.Enqueue(formatter(state, exception));
        }
    }
}
