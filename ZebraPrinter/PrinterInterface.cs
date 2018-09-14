using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ZebraPrint.Hubs;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using System.Text;

using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer.Discovery;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings;

internal interface IPrinterInterface
{
    Task ReadAvailableData();
    int GetConnectedUserCount();
    bool AddConnectedUser(string ID);
    bool RemoveConnectedUser(string ID);
}

internal class PrinterInterface : IPrinterInterface
{
    private HashSet<string> connectedUserIds;
    private IHubContext<PrinterHub> _printerHub;
    private Dictionary<string, Connection> connections;
    //private List<string> printer_names = new List<string>();
    private string error;

    public int GetConnectedUserCount()
    {
        Console.WriteLine($"{connectedUserIds.Count} users online {string.Join(",", connectedUserIds)}");
        return connectedUserIds.Count;
    }
    public bool AddConnectedUser(string ID)
    {
        return connectedUserIds.Add(ID);
    }
    public bool RemoveConnectedUser(string ID)
    {
        return connectedUserIds.Remove(ID);
    }
    public PrinterInterface(IServiceProvider services)
    {
        connectedUserIds = new HashSet<string>();
        connections = new Dictionary<string, Connection>();
        Services = services;
        using (var scope = Services.CreateScope())
        {
            _printerHub = scope.ServiceProvider.GetRequiredService<IHubContext<PrinterHub>>();
        }
        
        DiscoverPrinters();
    }

    public IServiceProvider Services { get; }

    public void DiscoverPrinters()
    {
        List<DiscoveredUsbPrinter> printers;
        try
        {
            printers = UsbDiscoverer.GetZebraUsbPrinters();
            foreach (DiscoveredUsbPrinter usbPrinter in printers)
            {
                string name = usbPrinter.ToString();
                Connection connection = usbPrinter.GetConnection();
                if (!connections.ContainsKey(name))
                    connections.Add(name, connection);
                connection.Open();
                Zebra.Sdk.Printer.ZebraPrinter printer = ZebraPrinterFactory.GetLinkOsPrinter(connection);
                ZebraPrinterLinkOs linkOsPrinter = ZebraPrinterFactory.CreateLinkOsPrinter(printer);
                if (linkOsPrinter != null)
                {
                    linkOsPrinter.ConfigureAlert(new PrinterAlert(AlertCondition.ALL_MESSAGES, AlertDestination.USB, true, true, null, 0, false));
                }
                connection.Close();
            }
        }
        catch (ConnectionException e)
        {
            error = $"Error discovering local printers: {e.Message}";
        }
    }

    public async Task ReadAvailableData()
    {

        Byte[] data;
        foreach (KeyValuePair<string,Connection> entry in connections)
        {
            Connection connection = entry.Value;
            try
            {
                connection.Open();
                data = connection.Read();
                if (data != null && data.Length > 0)
                {
                    string message = Encoding.UTF8.GetString(data);
                    await _printerHub.Clients.All.SendAsync("MessageSent", message);
                }
                connection.Close();
            }
            catch (ConnectionException e)
            {
                error = $"Error discovering local printers: {e.Message}";
            }
        }
    }
}