using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ZebraPrint.Hubs;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Windows.Threading;
using System.Threading;
using ZebraPrinterGUI;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using System.Net.Http;

internal interface IUserInterface
{
    string GetUsername();
    string GetPassword();
}

internal class UserInterface : IUserInterface
{
    private string username;
    private string password;
    public int ExitStatus { get; set; }
    private HttpClientService _httpClientService;
    private MainWindow _zebraPrinterGUI;

    public string GetUsername()
    {
        return username;
    }
    public string GetPassword()
    {
        return password;
    }

    public UserInterface(IApplicationLifetime applicationLifetime, IConfiguration configuration, HttpClientService httpClientService)
    {
        Console.WriteLine("Interface starting...");
        _httpClientService = httpClientService;
        ExitStatus = 0;
        Task<(string serverStatus, string username)> tryConnect = TryConnect();
        tryConnect.Wait();
        (string serverStatus, string username) = tryConnect.Result;
        Task task = StartSTATask(() => {
            _zebraPrinterGUI = new MainWindow(ConnectToServer, username, _httpClientService.BaseAddress, serverStatus);
            _zebraPrinterGUI.Show();
            Dispatcher.Run();
            username = _zebraPrinterGUI.Username;
            password = _zebraPrinterGUI.Password;
            return Task.CompletedTask;
        });
        task.Wait();
        if (ExitStatus == 0)
            applicationLifetime.StopApplication();

    }
    public void parseJsonResponse(JObject errors, string property, ref Label label)
    {
        var jProperty = errors.Property(property);
        if (jProperty != null)
            label.Content = (string)jProperty.Value;

    }

    public async void ConnectToServer(
        Dispatcher dispatcher,
        string username,
        string password,
        Label serverHelperText,
        Label usernameHelperText,
        Label passwordHelperText
        )
    {
        JObject errors = new JObject();
        string response = null;

        (errors, response) = await _httpClientService.ConnectToServer(
                    username,
                    password);
       if(errors.HasValues)
        {
            parseJsonResponse(errors, "HttpRequest", ref serverHelperText);
            parseJsonResponse(errors, "login", ref usernameHelperText);
            parseJsonResponse(errors, "password", ref passwordHelperText);
            ExitStatus = 0;
        }
       else
        {
            Console.WriteLine(response);
            ExitStatus = 1;
            dispatcher.InvokeShutdown();
        }
    }
    public async Task<(string serverStatus, string username)> TryConnect()
    {
        JObject errors = new JObject();
        string username = null;
        string serverStatus = null;

        (errors, username) = await _httpClientService.TryConnect();
        if (errors.Property("HttpRequest") != null)
        {
            serverStatus = (string)errors.Property("HttpRequest");
        }

        return (serverStatus, username);
    }

    public static Task<T> StartSTATask<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        Thread thread = new Thread(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }
    public IServiceProvider Services { get; }

}