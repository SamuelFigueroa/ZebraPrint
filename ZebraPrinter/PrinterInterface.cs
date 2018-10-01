using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ZebraPrint.Hubs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;


using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer.Discovery;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings;
using Newtonsoft.Json.Linq;
using System.Net.Http;

internal interface IPrinterInterface
{
    Task<(JObject errors, byte[] imageData)> PreviewZpl(string zpl);
    Task ReadAvailableData();
    Task WriteAvailableData();
    Task StartJob(string connection_name);
    Task<(JObject errors, JObject job)> GetNextPrinterJob(string connection_name);
    Task<(JObject errors, bool? response)> PauseQueue(string connection_name);
    Task<(JObject errors, bool? response)> DeletePrinterJob(string connection_name, string jobID);
    string[] GetConnectedPrinters();
    Dictionary<string, Connection> GetConnections();
    void incrementPreviewJobCount(string connection_name);
    void decrementPreviewJobCount(string connection_name);

}

internal class PrinterInterface : IPrinterInterface
{
    private IHubContext<PrinterHub> _printerHub;
    private Dictionary<string, Connection> connections;
    private HttpClientService _httpClientService;
    private Dictionary<string, bool> printerStatus;
    private Dictionary<string, string> dataToSend;
    private Dictionary<string, JObject> currentJob;
    private Dictionary<string, int> previewJobCount;
    private string error;
    
    public void decrementPreviewJobCount(string connection_name)
    {
        previewJobCount[connection_name] = previewJobCount[connection_name] - 1;
    }
    public void incrementPreviewJobCount(string connection_name)
    {
        previewJobCount[connection_name] = previewJobCount[connection_name] + 1;
    }
    public bool PrinterIsReady(string connection_name)
    {
        return printerStatus[connection_name];
    }
    public Dictionary<string, Connection> GetConnections()
    {
        return connections;
    }

    public string[] GetConnectedPrinters()
    {
        string[] connection_names = new string[connections.Keys.Count];
        connections.Keys.CopyTo(connection_names, 0);
        return connection_names;
    }


    public PrinterInterface(IServiceProvider services, HttpClientService httpClientService, IConfiguration configuration)
    {
        connections = new Dictionary<string, Connection>();
        printerStatus = new Dictionary<string, bool>();
        dataToSend = new Dictionary<string, string>();
        currentJob = new Dictionary<string, JObject>();
        previewJobCount = new Dictionary<string, int>();
        Services = services;
        _httpClientService = httpClientService;
        using (var scope = Services.CreateScope())
        {
            _printerHub = scope.ServiceProvider.GetRequiredService<IHubContext<PrinterHub>>();
        }
        DiscoverUsbPrinters();
    }

    public IServiceProvider Services { get; }
    
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
                    Console.WriteLine(message);
                    if (!message.Contains("PQ JOB COMPLETED"))
                    {
                        await _printerHub.Clients.All.SendAsync("LogMessage", entry.Key, message.TrimEnd('\n'));

                        ZebraPrinterLinkOs printer = ZebraPrinterFactory.GetLinkOsPrinter(connection);
                        PrinterStatus status = printer.GetCurrentStatus();

                        if (status.isReadyToPrint)
                            printerStatus[entry.Key] = true;
                        else
                        {
                            printerStatus[entry.Key] = false;
                            await PauseQueue(entry.Key, "");
                        }
                    }
                    else
                    {
                        if (previewJobCount[entry.Key] > 0)
                            decrementPreviewJobCount(entry.Key);
                        else
                            await CompleteJob(entry.Key, currentJob[entry.Key]);
                    }
                }
                
                connection.Close();
            }
            catch (ConnectionException e)
            {
                error = $"Error reading data from  local printers: {e.Message}";
            }
        }
    }
    
    public async Task WriteAvailableData()
    {

        string data;
        foreach (KeyValuePair<string, Connection> entry in connections)
        {
            Connection connection = entry.Value;
            try
            {
                connection.Open();
                data = dataToSend[entry.Key];
                if (!String.IsNullOrEmpty(data))
                {
                    string zplData = data;
                    dataToSend[entry.Key] = "";
                    connection.Write(Encoding.UTF8.GetBytes(zplData));

                }

                connection.Close();
            }
            catch (ConnectionException e)
            {
                error = $"Error reading data from  local printers: {e.Message}";
            }
        }
    }

    public async Task<(JObject errors, JObject job)> GetNextPrinterJob(string connection_name)
    {
        JObject job = new JObject();
        JObject errors = new JObject();

        try
        {
            (errors, job) = await _httpClientService.GetNextPrinterJob(connection_name);
        }
        catch (HttpRequestException)
        {
            errors.Add("HttpRequest", "Server couldn't be reached");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return (errors, job);
    }

    public async Task<(JObject errors, bool? response)> PauseQueue(string connection_name)
    {
        JObject errors = new JObject();
        bool? response = null;

        try
        {
            (errors, response) = await _httpClientService.PauseQueue(connection_name);
        }
        catch (HttpRequestException)
        {
            errors.Add("HttpRequest", "Server couldn't be reached");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return (errors, response);
    }
    public async Task<(JObject errors, bool? response)> DeletePrinterJob(string connection_name, string jobID)
    {
        JObject errors = new JObject();
        bool? response = null;

        try
        {
            (errors, response) = await _httpClientService.DeletePrinterJob(connection_name, jobID);
        }
        catch (HttpRequestException)
        {
            errors.Add("HttpRequest", "Server couldn't be reached");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return (errors, response);
    }
    
    public async Task StartJob(string connection_name)
    {
        JObject job = new JObject();
        JObject errors = new JObject();
        Console.WriteLine("Job starting...");
        await _printerHub.Clients.All.SendAsync("QueueUpdated", connection_name);
        await _printerHub.Clients.All.SendAsync("LogMessage", connection_name, "Getting printer's current status.");

        (errors, job) = await GetNextPrinterJob(connection_name);
        if (errors.HasValues)
        {
            string reason = "Printer failed to find the next job in the queue.";
            Console.WriteLine(reason);
            Console.WriteLine(errors.ToString());
            await PauseQueue(connection_name, reason);
        }
        else if (PrinterIsReady(connection_name))
        {
            await _printerHub.Clients.All.SendAsync("LogMessage", connection_name, "Printer is ready.");

            Console.WriteLine("Next job found successfully.");
            Console.WriteLine(job.ToString());
            Console.WriteLine("Printer is ready to print.");
            await ProcessJob(connection_name, job);
        }
        else
        {
            string reason = "Printer is not ready to print.";
            await PauseQueue(connection_name, reason);
        }
    }
    public async Task ProcessJob(string connection_name, JObject job)
    {
        
        await _printerHub.Clients.All.SendAsync("LogMessage", connection_name, $"Job Started: {(string)job.Property("name")}");

        //Send ZPL to printer and wait for job failed or job completed.

        dataToSend[connection_name] = (string)job.Property("data");
        currentJob[connection_name] = job;

    }

    public async Task PauseQueue(string connection_name, string reason)
    {
        bool? response = null;
        JObject errors = new JObject();

        
        (errors, response) = await PauseQueue(connection_name);
        if (errors.HasValues)
        {
            Console.WriteLine("Error while pausing queue...");
            string errorMessage = (string)errors.Property("HttpRequest");
            await _printerHub.Clients.All.SendAsync("LogMessage", connection_name, errorMessage);
            await _printerHub.Clients.All.SendAsync("LogMessage", connection_name, "Printer failed to pause the job queue. Please check connection from hub to server.");
        }
        else
        {
            Console.WriteLine("Queue paused successfully.");
            await _printerHub.Clients.All.SendAsync("QueueUpdated", connection_name);
            if (!String.IsNullOrEmpty(reason))
              await _printerHub.Clients.All.SendAsync("LogMessage", connection_name, $"The job queue has been paused. {reason}");
        }
    }

    public async Task CompleteJob(string connection_name, JObject job)
    {
        await _printerHub.Clients.All.SendAsync("LogMessage", connection_name, $"Job Completed: {(string)job.Property("name")}");
        currentJob[connection_name] = new JObject();
        bool? response = null;
        JObject errors = new JObject();

        
        (errors, response) = await DeletePrinterJob(connection_name, (string)job.Property("id"));
        if (errors.HasValues)
        {
            string reason = "Printer failed to delete the finished job from queue.";
            Console.WriteLine(reason);
            Console.WriteLine(errors.ToString());
            await PauseQueue(connection_name, reason);
        }
        else
        {
            Console.WriteLine("Finished job deleted successfully.");
            await _printerHub.Clients.All.SendAsync("QueueUpdated", connection_name);
            if (response == true)
            {
                StartJob(connection_name);
            }
        }
    }
    public async Task<(JObject errors, byte[] imageData)> PreviewZpl(string zpl)
    {
        JObject errors = new JObject();
        byte[] imageData = null;
        try
        {
            imageData = await _httpClientService.GetPreviewImageData(zpl);
        }
        catch (HttpRequestException)
        {
            errors.Add("HttpRequest", "Server couldn't be reached");
        }
        catch (Exception e)
        {
            errors.Add("HttpRequest", e.Message);
        }

        return (errors, imageData);
    }
    public void DiscoverUsbPrinters()
    {
        Console.WriteLine("Discovering printers...");
        List<DiscoveredUsbPrinter> printers;
        try
        {
            printers = UsbDiscoverer.GetZebraUsbPrinters();
            foreach (DiscoveredUsbPrinter usbPrinter in printers)
            {
                string name = usbPrinter.ToString();
                Console.WriteLine($"Found Printer: {name}");
                Connection connection = usbPrinter.GetConnection();
                if (!connections.ContainsKey(name))
                    connections.Add(name, connection);
                connection.Open();
                Zebra.Sdk.Printer.ZebraPrinter printer = ZebraPrinterFactory.GetLinkOsPrinter(connection);

                ZebraPrinterLinkOs linkOsPrinter = ZebraPrinterFactory.CreateLinkOsPrinter(printer);
                printerStatus.Add(name, printer.GetCurrentStatus().isReadyToPrint);
                dataToSend.Add(name, "");
                currentJob.Add(name, new JObject());
                previewJobCount.Add(name, 0);
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
}