using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using stockFlow.Application.Features;
using stockFlow.Infrastructure.Features;
using stockFlow.Infrastructure.Persistence;
using stockFlow.Infrastructure.Security;
namespace stockFlow.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<StockFlowDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<DocumentNumberGenerator>();
        services.AddScoped<Access>();
        services.AddScoped<PasswordHasher<AppUser>>();
        services.AddScoped<AuthService>();
        services.AddScoped<IAuthService>(sp => sp.GetRequiredService<AuthService>());
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMasterService, MasterService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}
