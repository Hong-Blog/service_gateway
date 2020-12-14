using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Middleware;
using service_gateway.Middleware;

namespace service_gateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOcelot();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHongAuthenticationMiddleware();
            app.UseOcelot(PipelineConfiguration).Wait();
        }

        private void PipelineConfiguration(OcelotPipelineConfiguration config)
        {
            config.AuthenticationMiddleware = AuthenticationMiddleware;

            config.AuthorisationMiddleware = async (context, next) =>
            {
                var logger = context.RequestServices.GetService<IOcelotLoggerFactory>();
                var ocelotLogger = logger.CreateLogger<Startup>();
                ocelotLogger.LogInformation("授权。。。。");
                await next.Invoke();
            };
        }

        private Task AuthenticationMiddleware(HttpContext context, Func<Task> next)
        {
            var logger = context.RequestServices.GetService<IOcelotLoggerFactory>();
            var ocelotLogger = logger.CreateLogger<Startup>();
            ocelotLogger.LogInformation("认证。。。");

            return next.Invoke();
        }
    }
}