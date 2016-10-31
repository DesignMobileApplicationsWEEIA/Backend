using System;
using Core.Domain.Database.Implementations;
using Core.Domain.Database.Interfaces;
using Core.Domain.Repositories.Interfaces;
using Domain.Cache.Implementations;
using Domain.Cache.Interfaces;
using Domain.Repositories.Implementations;
using Domain.Services.Implementations;
using Domain.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backend.Web
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            services.AddMemoryCache();

            services.AddEntityFrameworkNpgsql()
                .AddDbContext<DefaultDbContext>(options => options.UseNpgsql(Configuration["Data:DbContext:LocalConnectionString"]));

            services.AddScoped<IDbManager, DefaultDbContext>();
            services.AddTransient<ICacheService, InMemoryCacheService>();
            services.AddScoped<IUnitOfWork>(provider =>
            {
                var dbManager = provider.GetRequiredService<IDbManager>();
                var cache = provider.GetRequiredService<IMemoryCache>();
                return new UnitOfWork(dbManager, cache);
            });

            services.AddTransient<IBuildingService, BuildingService>();
            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc().UseSwagger().UseSwaggerUi();

            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetService<DefaultDbContext>().Database.Migrate();
            }
        }
    }
}
