using Microsoft.AspNetCore.Mvc;

namespace Core.Filters.Extensions
{
    public static class ValidationExtensions
    {
        public static IServiceCollection AddApiValidation(this IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddControllers(options =>
            {
                options.Filters.Add<ValidateModelAttribute>();
            });

            return services;
        }
    }
}
