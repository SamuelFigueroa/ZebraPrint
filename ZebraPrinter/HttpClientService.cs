using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Formatting;
using System.Collections.Generic;
using HtmlAgilityPack;

internal class HttpClientService
{
    public HttpClient Client { get; }
    public HttpClient hubClient { get; }
    public string PrinterHubAddress { get; }
    public string PrinterHubName { get; }
    public string ServerAddress { get; set; }
    public string PrinterAddress { get; set; }
    public bool Connected { get; set; }

    public HttpClientService(HttpClient client, IServer server, IServiceProvider services, IConfiguration configuration)
    {
        Connected = false;
        Client = client;
        ServerAddress = configuration.GetValue<string>("server");
        Client.BaseAddress = new Uri($"http://{ServerAddress}/");
        Client.DefaultRequestHeaders.Add("Accept", "*/*");
        hubClient = new HttpClient();
        PrinterAddress = configuration.GetValue<string>("printer");
        hubClient.BaseAddress = new Uri($"http://{PrinterAddress}/");
        hubClient.DefaultRequestHeaders.Add("Accept", "*/*");
        Services = services;
        PrinterHubName = configuration.GetValue<string>("hubname");
        PrinterHubAddress = configuration.GetValue<string>("urls") + "/zph";
    }

    public IServiceProvider Services { get; }

    public async Task<byte[]> GetPreviewImageData(string zpl)
    {
        byte[] imageData = null;
        var dict = new Dictionary<string, string>();
        dict.Add("data", zpl);
        dict.Add("dev", "E");
        dict.Add("oname", "test");
        dict.Add("otype", "---");
        dict.Add("prev", "Preview Label");
        dict.Add("pw", "");

        var res = await hubClient.PostAsync("/zpl", new FormUrlEncodedContent(dict));

        res.EnsureSuccessStatusCode();

        try
        {
            var result = await res.Content.ReadAsStringAsync();
            var htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(result);
            string imgSrc = htmlDoc.DocumentNode.SelectSingleNode("//body/div/img")
                .Attributes["src"].Value;
            imageData = await hubClient.GetByteArrayAsync(hubClient.BaseAddress.AbsoluteUri + imgSrc);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return imageData;
        
    }
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
    public async Task<(JObject errors, string response)> RegisterPrinterHub(string name, string address, bool online)
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
                    online = online,
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
          
                (errors, response) = await RegisterPrinterHub(PrinterHubName, PrinterHubAddress, true);
                if (!errors.HasValues)
                {
                    Connected = true;
                }
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
    public async Task DisconnectFromServer()
    {
        JObject errors = new JObject();
        string response = null;

        try
        {
            (errors, response) = await RegisterPrinterHub(PrinterHubName, PrinterHubAddress, false);
        }
        catch (HttpRequestException)
        {
            errors.Add("HttpRequest", "Server couldn't be reached");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        if (errors.HasValues)
            Console.WriteLine(errors);
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
    public async Task<(JObject errors, bool? response)> PauseQueue(string connection_name)
    {
        JObject errors = new JObject();
        bool? response = null;
        GQLPayload payload = new GQLPayload()
        {
            operationName = "updatePrinter",
            variables = new GQLPayloadVariables("updatePrinter")
            {
                input = new GQLPayloadInputVariable("updatePrinter")
                {
                    connection_name = connection_name,
                    reset = true,
                    queue = false
                }
            },
            query = "mutation updatePrinter($input: UpdatePrinterInput!) { updatePrinter(input: $input) }"
        };

        var res = await Client.PostAsync<GQLPayload>(
            "/graphql", payload, new JsonMediaTypeFormatter());

        res.EnsureSuccessStatusCode();
        try
        {
            var result = await res.Content.ReadAsAsync<GQLUpdatePrinterResponse>();
            response = result.data.updatePrinter;
        }
        catch (Exception)
        {
            Stream result_stream = await res.Content.ReadAsStreamAsync();
            result_stream.Seek(0, SeekOrigin.Begin);
            var result = await res.Content.ReadAsAsync<GQLUpdatePrinterResponse>();
            errors = result.errors[0].extensions.exception.errors;
        }
        return (errors, response);
    }
    public async Task<(JObject errors, bool? response)> DeletePrinterJob(string connection_name, string jobID)
    {
        JObject errors = new JObject();
        bool? response = null;
        GQLPayload payload = new GQLPayload()
        {
            operationName = "deletePrinterJob",
            variables = new GQLPayloadVariables("deletePrinterJob")
            {
                input = new GQLPayloadInputVariable("deletePrinterJob")
                {
                    connection_name = connection_name,
                    dequeue = true,
                    jobID = jobID
                }
            },
            query = "mutation deletePrinterJob($input: DeletePrinterJobInput!) { deletePrinterJob(input: $input) }"
        };

        var res = await Client.PostAsync<GQLPayload>(
            "/graphql", payload, new JsonMediaTypeFormatter());

        res.EnsureSuccessStatusCode();
        try
        {
            var result = await res.Content.ReadAsAsync<GQLDeletePrinterJobResponse>();
            response = result.data.deletePrinterJob;
        }
        catch (Exception)
        {
            Stream result_stream = await res.Content.ReadAsStreamAsync();
            result_stream.Seek(0, SeekOrigin.Begin);
            var result = await res.Content.ReadAsAsync<GQLDeletePrinterJobResponse>();
            errors = result.errors[0].extensions.exception.errors;
        }
        return (errors, response);
    }
    public async Task<(JObject errors, JObject job)> GetNextPrinterJob(string connection_name)
    {
        JObject job = new JObject();
        JObject errors = new JObject();

        GQLPayload payload = new GQLPayload()
        {
            operationName = "getNextPrinterJob",
            variables = new GQLPayloadVariables("getNextPrinterJob")
            {
                connection_name = connection_name
            },
            query = "query getNextPrinterJob($connection_name: String!) { nextPrinterJob(connection_name: $connection_name) { id name data time_added status __typename  } }"
        };

        var res = await Client.PostAsync<GQLPayload>(
            "/graphql", payload, new JsonMediaTypeFormatter());

        res.EnsureSuccessStatusCode();

        try
        {
            var result = await res.Content.ReadAsAsync<GQLNextPrinterJobResponse>();
            job = result.data.nextPrinterJob;
        }
        catch (Exception)
        {
            Stream result_stream = await res.Content.ReadAsStreamAsync();
            result_stream.Seek(0, SeekOrigin.Begin);
            var result = await res.Content.ReadAsAsync<GQLNextPrinterJobResponse>();
            errors = result.errors[0].extensions.exception.errors;
        }

        return (errors, job);
    }
}