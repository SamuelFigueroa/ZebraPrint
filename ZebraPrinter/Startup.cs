using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using ZebraPrint.Hubs;
using Microsoft.AspNetCore.Cors;
using ZebraPrinterGUI;

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
            services.AddSingleton<PrinterStatusService>();
            services.AddSingleton<IUserInterface, UserInterface>();
            
            //services.AddHostedService<PrinterStatusService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder =>
                       builder
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials()
                       .WithOrigins("http://localhost:8080"));

            app.UseSignalR(routes =>
            {
                routes.MapHub<PrinterHub>("/zph");
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });

            app.ApplicationServices.GetService<IUserInterface>();
        }
    }
}
