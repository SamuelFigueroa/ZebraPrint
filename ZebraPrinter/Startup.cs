using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ZebraPrint.Hubs;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ZebraPrinter
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true;
            });
            services.AddHttpClient<HttpClientService>();
            services.AddSingleton<IPrinterInterface, PrinterInterface>();
            services.AddSingleton<IUserInterface, UserInterface>();
            
            services.AddHostedService<PrinterStatusService>();
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime, IHostingEnvironment env, IConfiguration configuration)
        {
            string serverAddress = configuration.GetValue<string>("server");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors(builder =>
                       builder
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials()
                       .WithOrigins($"http://{serverAddress}"));

            app.UseSignalR(routes =>
            {
                routes.MapHub<PrinterHub>("/zph");
            });

            app.Run(async (context) =>
            {
               
                await context.Response.WriteAsync("Welcome to the Zebra Printer Hub");
            });

            app.ApplicationServices.GetService<IUserInterface>();
        }
    }
}
