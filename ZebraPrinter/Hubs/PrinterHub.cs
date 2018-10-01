using System;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace ZebraPrint.Hubs
{
    public class PrinterHub : Hub
    {
        public PrinterHub(IServiceProvider services)
        {
            Services = services;
        }

        public IServiceProvider Services { get; }

        public async Task SendMessage(string connection_name, string message)
        {
            await Clients.All.SendAsync("LogMessage", connection_name, message);
        }

        public async Task PreviewZpl(string connection_name, string zpl)
        {
            JObject errors = new JObject();
            byte[] imageData = null;
            using (var scope = Services.CreateScope())
            {
                var printerInterface =
                    scope.ServiceProvider
                        .GetRequiredService<IPrinterInterface>();

                 (errors, imageData) = await printerInterface.PreviewZpl(zpl);
            }
            if (errors.HasValues)
            {
                string errorMessage = (string)errors.Property("HttpRequest");
                await Clients.Caller.SendAsync("LogMessage", connection_name, errorMessage);
            }
            else
            {
                await Clients.Caller.SendAsync("ShowPreview", connection_name, imageData);
            }

        }
        public async Task GetPrinters()
        {
            string[] printers = null;
            using (var scope = Services.CreateScope())
            {
                var printerInterface =
                    scope.ServiceProvider
                        .GetRequiredService<IPrinterInterface>();

                printers = printerInterface.GetConnectedPrinters();
            }
            await Clients.Caller.SendAsync("PrintersFound", printers);
        }

        public async Task RefreshQueue(string connection_name)
        {
            await Clients.All.SendAsync("QueueUpdated", connection_name);
        }

        public async Task StartQueue(string connection_name)
        {
            using (var scope = Services.CreateScope())
            {
                var printerInterface =
                    scope.ServiceProvider
                        .GetRequiredService<IPrinterInterface>();

                await Clients.All.SendAsync("LogMessage", connection_name, "Queue started.");
                printerInterface.StartJob(connection_name);
            }

        }
    }
}