using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Formatting;


internal class HttpClientService
{
    public HttpClient Client { get; }
    public string PrinterHubAddress { get; }
    public string PrinterHubName { get; }
    public string BaseAddress { get; set; }

    public HttpClientService(HttpClient client, IServer server, IServiceProvider services, IConfiguration configuration)
    {

        BaseAddress = configuration.GetValue<string>("server");
        client.BaseAddress = new Uri($"http://{BaseAddress}/");
        client.DefaultRequestHeaders.Add("Accept", "*/*");

        Services = services;
        Client = client;
        PrinterHubName = configuration.GetValue<string>("hubname");
        PrinterHubAddress = configuration.GetValue<string>("urls") + "/zph";
    }

    public IServiceProvider Services { get; }

    public async Task<(JObject errors, string token)> AuthenticateClient(string login, string password)
    {
        string token = null;
        JObject errors = new JObject();

        GQLPayload payload = new GQLPayload()
        {
            operationName = "loginUser",
            variables = new GQLPayloadVariables("loginUser")
            {
                input = new GQLPayloadInputVariable("loginUser")
                {
                    login = login,
                    password = password
                }
            },
            query = "mutation loginUser($input: loginInput!) { login(input: $input) { token success __typename  } }"
        };

        var res = await Client.PostAsync<GQLPayload>(
            "/graphql", payload, new JsonMediaTypeFormatter());

        res.EnsureSuccessStatusCode();

        try
        {
            var result = await res.Content.ReadAsAsync<GQLResponse>();
            token = result.data.login.token;
        }
        catch (Exception)
        {
            Stream result_stream = await res.Content.ReadAsStreamAsync();
            result_stream.Seek(0, SeekOrigin.Begin);
            var result = await res.Content.ReadAsAsync<GQLResponse>();
            errors = result.errors[0].extensions.exception.errors;
        }
        return (errors, token);
    }
    public async Task<(JObject errors, string response)> RegisterPrinterHub(string name, string address, string user)
    {
        JObject errors = new JObject();
        string response = null;
        GQLPayload payload = new GQLPayload()
        {
            operationName = "registerPrinterHub",
            variables = new GQLPayloadVariables("registerPrinterHub")
            {
                input = new GQLPayloadInputVariable("registerPrinterHub")
                {
                    name = name,
                    address = address,
                    online = true,
                    user = user
                }
            },
            query = "mutation registerPrinterHub($input: PrinterHubInput!) { registerPrinterHub(input: $input) { response __typename  } }"
        };

        var res = await Client.PostAsync<GQLPayload>(
            "/graphql", payload, new JsonMediaTypeFormatter());

        res.EnsureSuccessStatusCode();
        try
        {
            var result = await res.Content.ReadAsAsync<GQLRegisterResponse>();
            response = result.data.registerPrinterHub.response;
        }
        catch (Exception)
        {
            Stream result_stream = await res.Content.ReadAsStreamAsync();
            result_stream.Seek(0, SeekOrigin.Begin);
            var result = await res.Content.ReadAsAsync<GQLRegisterResponse>();
            errors = result.errors[0].extensions.exception.errors;
        }
        return (errors, response);

    }
    public async Task<(JObject errors, string user)> GetHub(string address)
    {
        string user = null;
        JObject errors = new JObject();

        GQLPayload payload = new GQLPayload()
        {
            operationName = "getPrinterHub",
            variables = new GQLPayloadVariables("getPrinterHub")
            {
                address = address
            },
            query = "query getPrinterHub($address: String!) { printerHub(address: $address) { user __typename  } }"
        };

        var res = await Client.PostAsync<GQLPayload>(
            "/graphql", payload, new JsonMediaTypeFormatter());

        res.EnsureSuccessStatusCode();

        try
        {
            var result = await res.Content.ReadAsAsync<GQLGetHubResponse>();
            user = result.data.printerHub.user;
        }
        catch (Exception)
        {
            Stream result_stream = await res.Content.ReadAsStreamAsync();
            result_stream.Seek(0, SeekOrigin.Begin);
            var result = await res.Content.ReadAsAsync<GQLGetHubResponse>();
            errors = result.errors[0].extensions.exception.errors;
        }

        return (errors, user);
    }

    public async Task<(JObject errors, string response)> ConnectToServer(string username, string password)
    {
        JObject errors = new JObject();
        string token = null;
        string response = null;

        try
        {
            (errors, token) = await AuthenticateClient(username, password);
            if (!errors.HasValues)
            {
                Client.DefaultRequestHeaders.Add("authorization", token);
          
                (errors, response) = await RegisterPrinterHub(PrinterHubName, PrinterHubAddress, username);
            }
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
    public async Task<(JObject errors, string username)> TryConnect()
    {
        JObject errors = new JObject();
        string username = null;

        try
        {
            (errors, username) = await GetHub(PrinterHubAddress);
        }
        catch (HttpRequestException)
        {
            errors.Add("HttpRequest", "Server couldn't be reached");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return (errors, username);
    }
}