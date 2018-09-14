using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace ZebraPrint.Hubs
{
    public class PrinterHub : Hub
    {

        public PrinterHub(IServiceProvider services)
        {
            Services = services;
        }

        public IServiceProvider Services { get; }

        public override Task OnConnectedAsync()
        {
            using (var scope = Services.CreateScope())
            {
                var printerInterface =
                    scope.ServiceProvider
                        .GetRequiredService<IPrinterInterface>();

                printerInterface.AddConnectedUser(Context.ConnectionId);
                printerInterface.GetConnectedUserCount();
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            using (var scope = Services.CreateScope())
            {
                var printerInterface =
                    scope.ServiceProvider
                        .GetRequiredService<IPrinterInterface>();

                printerInterface.RemoveConnectedUser(Context.ConnectionId);
                printerInterface.GetConnectedUserCount();
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("MessageSent", message);
        }

      
        //public async Task GetPrinterStatus(string name)
        //{
        //    Console.WriteLine($"Name is: {name}");
        //    Console.WriteLine(printer_connections.ToString());
        //    Connection connection = printer_connections[name];
        //    try
        //    {
        //        // Open the connection - physical connection is established here.
        //        connection.Open();
        //        Zebra.Sdk.Printer.ZebraPrinter printer = ZebraPrinterFactory.GetLinkOsPrinter(connection);
        //        ZebraPrinterLinkOs linkOsPrinter = ZebraPrinterFactory.CreateLinkOsPrinter(printer);
        //        List<PrinterAlert> printerAlerts = linkOsPrinter.GetConfiguredAlerts();
        //        foreach( PrinterAlert alert in printerAlerts.ToArray())
        //        {
        //            Console.WriteLine($"{alert.ConditionName} directed to ${alert.Destination.DestinationName}");
        //        }
        //        //Zebra.Sdk.Printer.ZebraPrinter printer = ZebraPrinterFactory.GetInstance(connection);
        //        PrinterStatus printerStatus = printer.GetCurrentStatus();
        //        if (printerStatus.isReadyToPrint)
        //        {
        //            printer_status = "Ready To Print";
        //        }
        //        else if (printerStatus.isPaused)
        //        {
        //            printer_status = "Cannot Print because the printer is paused.";
        //        }
        //        else if (printerStatus.isHeadOpen)
        //        {
        //            printer_status = "Cannot Print because the printer head is open.";
        //        }
        //        else if (printerStatus.isPaperOut)
        //        {
        //            printer_status = "Cannot Print because the paper is out.";
        //        }
        //        else
        //        {
        //            printer_status = "Cannot Print.";
        //        }
        //    }
        //    catch (ConnectionException e)
        //    {
        //        error = $"Error connecting to printer: {e.Message}";
        //    }
        //    catch (ZebraPrinterLanguageUnknownException e)
        //    {
        //        error = $"Unknown ZPL: {e.Message}";
        //    }
        //    catch (ZebraIllegalArgumentException e)
        //    {
        //        error = $"Illegal Arguments: {e.Message}";
        //    }
        //    finally
        //    {
        //        // Close the connection to release resources.
        //        connection.Close();
        //        await Clients.Caller.SendAsync("StatusReceived", error, printer_status);
        //    }
        //}
        
        //public async Task FindPrinters()
        //{
        //    string error;
        //    List<string> printer_names = new List<string>();
        //    Dictionary<string, Connection> connections = new Dictionary<string, Connection>();
        //    List<DiscoveredUsbPrinter> printers;
        //    try
        //    {
        //        printers = UsbDiscoverer.GetZebraUsbPrinters();
        //        foreach (DiscoveredUsbPrinter usbPrinter in printers)
        //        {
        //            string name = usbPrinter.ToString();
        //            printer_names.Add(name);
        //            connections.Add(name, usbPrinter.getConnection());
        //        }
        //        printer_connections = connections;
        //    }
        //    catch (ConnectionException e)
        //    {
        //        error = $"Error discovering local printers: {e.Message}";
        //    }
        //    await Clients.Caller.SendAsync("DiscoveredPrinters", error, printer_names); //add printers and error argument.
        //}
        //public async Task SendZpl(string printer_name, string zpl)
        //{
        //    string error;
        //    Connection printer_connection = printer_connections[printer_name];
        //    try
        //    {
        //        // Open the connection - physical connection is established here.
        //        printer_connection.Open();
        //        ZebraPrinter printer = ZebraPrinterFactory.GetInstance(printer_connection);
        //        ZebraPrinterLinkOs linkOsPrinter = ZebraPrinterFactory.CreateLinkOsPrinter(printer);
        //        if (linkOsPrinter != null)
        //        {
        //            linkOsPrinter.SendCommand(zpl);

        //            // Send the data to printer as a byte array.
        //            //printer_connection.Write(Encoding.UTF8.GetBytes(zpl));
        //        }
        //    }
        //    catch (ConnectionException e)
        //    {
        //        error = $"Error connecting to printer: {e.Message}";
        //    }
        //    catch (ZebraPrinterLanguageUnknownException e)
        //    {
        //        error = $"Unknown ZPL: {e.Message}";
        //    }
        //    catch (ZebraIllegalArgumentException e)
        //    {
        //        error = $"Illegal Arguments: {e.Message}";
        //    }
        //    finally
        //    {
        //        // Close the connection to release resources.
        //        printer_connection.Close();
        //        await Clients.All.SendAsync("ZplSent");
        //    }
        //}
        //private ConfigurePrinter(string printer_name)
        //{
        //    string error;
        //    Connection printer_connection = printer_connections[printer_name];
        //    try
        //    {
        //        // Open the connection - physical connection is established here.
        //        printer_connection.Open();
        //        ZebraPrinter printer = ZebraPrinterFactory.GetInstance(printer_connection);
        //        ZebraPrinterLinkOs linkOsPrinter = ZebraPrinterFactory.CreateLinkOsPrinter(printer);
        //        if (linkOsPrinter != null)
        //        {
        //            List<PrinterAlert> alerts;
        //            alerts.Add(new PrinterAlert(AlertCondition.ALL_MESSAGES, AlertDestination.USB, true, false));
        //            linkOsPrinter.ConfigureAlerts(alerts);
        //        }
        //    }
        //    catch (ConnectionException e)
        //    {
        //        error = $"Error connecting to printer: {e.Message}";
        //    }
        //    catch (ZebraPrinterLanguageUnknownException e)
        //    {
        //        error = $"Unknown ZPL: {e.Message}";
        //    }
        //    catch (ZebraIllegalArgumentException e)
        //    {
        //        error = $"Illegal Arguments: {e.Message}";
        //    }
        //    finally
        //    {
        //        // Close the connection to release resources.
        //        printer_connection.Close();
        //    }
        //}
    }
}