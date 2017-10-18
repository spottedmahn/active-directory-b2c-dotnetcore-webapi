using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace B2CWebApi
{
    public class Startup
    {
        public static string ScopeRead;
        public static string ScopeWrite;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see https://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();

            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            //app.UseCors(builder => builder
            //.AllowAnyOrigin()
            //.AllowAnyMethod()
            //.AllowAnyHeader());

            var tenant = Configuration["Authentication:AzureAd:Tenant"];
            var policy = Configuration["Authentication:AzureAd:Policy"];
            var audience = Configuration["Authentication:AzureAd:ClientId"];

            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                Authority = string.Format("https://login.microsoftonline.com/tfp/{0}/{1}/v2.0/",
                    tenant, policy),
                Audience = audience,
                Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = AuthenticationFailed,
                    OnChallenge = (context) =>
                    {
                        var debug = context;
                        return Task.FromResult(0);
                    },
                    OnMessageReceived = context =>
                    {
                        var debug = context;
                        return Task.FromResult(0);
                    }
                }
            });

            ScopeRead = Configuration["Authentication:AzureAd:ScopeRead"];
            ScopeWrite = Configuration["Authentication:AzureAd:ScopeWrite"];

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private Task AuthenticationFailed(AuthenticationFailedContext arg)
        {
            // For debugging purposes only!
            var s = $"AuthenticationFailed: {arg.Exception.Message}";
            arg.Response.ContentLength = s.Length;
            arg.Response.Body.Write(Encoding.UTF8.GetBytes(s), 0, s.Length);
            return Task.FromResult(0);
        }
    }
}
