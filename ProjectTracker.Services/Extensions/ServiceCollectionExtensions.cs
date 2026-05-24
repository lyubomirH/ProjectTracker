using Microsoft.Extensions.DependencyInjection;
using ProjectTracker.Services.Interfaces;
using ProjectTracker.Services.Services;

namespace ProjectTracker.Services.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            // Register services
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IWorkItemService, WorkItemService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<IDashboardService, DashboardService>();

            return services;
        }
    }
}