using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
            services.AddHttpClient();
            services.AddOcelot();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseHongAuthenticationMiddleware();
            app.UseOcelot(PipelineConfiguration).Wait();
        }

        private void PipelineConfiguration(OcelotPipelineConfiguration config)
        {
            config.AuthenticationMiddleware = AuthenticationMiddleware;
            config.AuthorisationMiddleware = AuthorisationMiddleware;
        }

        private async Task AuthorisationMiddleware(HttpContext context, Func<Task> next)
        {
            var logger = context.RequestServices.GetService<IOcelotLoggerFactory>();
            var ocelotLogger = logger.CreateLogger<Startup>();
            ocelotLogger.LogInformation("授权。。。。");
            await next.Invoke();
        }

        private async Task AuthenticationMiddleware(HttpContext context, Func<Task> next)
        {
            if (IsOptionsHttpMethod(context))
            {
                await next.Invoke();
                return;
            }

            var loggerFactory = context.RequestServices.GetService<IOcelotLoggerFactory>();
            var httpClientFactory = context.RequestServices.GetService<IHttpClientFactory>();

            var logger = loggerFactory.CreateLogger<Startup>();
            logger.LogInformation("认证。。。");

            var downstreamRoute = context.Items.DownstreamRoute();
            if (downstreamRoute.UpstreamPathTemplate.OriginalValue.StartsWith("/v1/li"))
            {
                logger.LogInformation("login 服务 无需认证");
                await next.Invoke();
                return;
            }

            if (!context.Request.Headers.TryGetValue("Authorization", out var authorizationValues))
            {
                context.Items.SetError(new UnauthenticatedError("未发现认证字段"));
                return;
            }

            var authorizationHeader = authorizationValues.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                context.Items.SetError(new UnauthenticatedError("认证字段为空"));
                return;
            }

            var loginService = Configuration.GetValue<string>("login_service") ?? "http://127.0.0.1:18082";
            var url = $"{loginService}/authorization";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add("Accept", "application/json; charset=utf-8");
            requestMessage.Headers.Add("Authorization", authorizationHeader);
            var httpClient = httpClientFactory.CreateClient();
            var responseMessage = await httpClient.SendAsync(requestMessage);
            if (responseMessage.StatusCode == HttpStatusCode.OK)
            {
                await next.Invoke();
                return;
            }

            context.Items.UpsertDownstreamResponse(new DownstreamResponse(responseMessage));
        }

        private static bool IsOptionsHttpMethod(HttpContext httpContext)
        {
            return httpContext.Request.Method.ToUpper() == "OPTIONS";
        }
    }
}