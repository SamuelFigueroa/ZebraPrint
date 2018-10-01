using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

//Request Content
internal class GQLPayload
{
    public string operationName { get; set; }
    public GQLPayloadVariables variables { get; set; }
    public string query { get; set; }

}
internal class GQLPayloadVariables
{
    public GQLPayloadVariables(string operation)
    {
        _operation = operation;
    }

    [JsonIgnore]
    private readonly string _operation;

    public GQLPayloadInputVariable input { get; set; }
    public string address { get; set; }
    public string connection_name { get; set; }

    public bool ShouldSerializeinput()
    {
        return (_operation == "loginUser" ||
                _operation == "registerPrinterHub" ||
                _operation == "updatePrinter" ||
                _operation == "deletePrinterJob");
    }

    public bool ShouldSerializeaddress()
    {
        return (_operation == "getPrinterHub");
    }
    public bool ShouldSerializeconnection_name()
    {
        return (_operation == "getNextPrinterJob");
    }
}
internal class GQLPayloadInputVariable
{
    public GQLPayloadInputVariable(string operation)
    {
        _operation = operation;
    }

    [JsonIgnore]
    private readonly string _operation;

    public string login { get; set; }
    public string password { get; set; }
    public string name { get; set; }
    public string address { get; set; }
    public string user { get; set; }
    public bool online { get; set; }
    public string connection_name { get; set; }
    public bool queue { get; set; }
    public bool reset { get; set; }
    public bool dequeue { get; set; }
    public string jobID { get; set; }

    public bool ShouldSerializelogin()
    {
        return (_operation == "loginUser");
    }
    public bool ShouldSerializepassword()
    {
        return (_operation == "loginUser");
    }
    public bool ShouldSerializename()
    {
        return (_operation == "registerPrinterHub");
    }
    public bool ShouldSerializeuser()
    {
        return (_operation == "registerPrinterHub");
    }
    public bool ShouldSerializeaddress()
    {
        return (_operation == "registerPrinterHub");
    }
    public bool ShouldSerializeonline()
    {
        return (_operation == "registerPrinterHub");
    }
    public bool ShouldSerializeconnection_name()
    {
        return (_operation == "updatePrinter" ||
                _operation == "deletePrinterJob" );
    }
    public bool ShouldSerializequeue()
    {
        return (_operation == "updatePrinter");
    }
    public bool ShouldSerializereset()
    {
        return (_operation == "updatePrinter");
    }
    public bool ShouldSerializedequeue()
    {
        return (_operation == "deletePrinterJob");
    }
    public bool ShouldSerializejobID()
    {
        return (_operation == "deletePrinterJob");
    }
}

//Response Content
internal class GQLResponse
{
    public GQLResponseData data { get; set; }
    public GQLResponseError[] errors { get; set; }
}
internal class GQLResponseData
{
    public GQLResponseDataLogin login { get; set; }
}
internal class GQLResponseDataLogin
{
    public string token { get; set; }
    public bool success { get; set; }
    public string __typename { get; set; }
}

//Register Response Content
internal class GQLRegisterResponse
{
    public GQLRegisterResponseData data { get; set; }
    public GQLResponseError[] errors { get; set; }
}
internal class GQLRegisterResponseData
{
    public GQLResponseDataPrinterHub registerPrinterHub { get; set; }
}
internal class GQLResponseDataPrinterHub
{
    public string response { get; set; }
    public string __typename { get; set; }
}

//GetHub Response Content
internal class GQLGetHubResponse
{
    public GQLGetHubResponseData data { get; set; }
    public GQLResponseError[] errors { get; set; }
}
internal class GQLGetHubResponseData
{
    public GQLGetHubData printerHub { get; set; }
}
internal class GQLGetHubData
{
    public string user { get; set; }
    public string __typename { get; set; }
}

//UpdatePrinter Response Content
internal class GQLUpdatePrinterResponse
{
    public GQLUpdatePrinterResponseData data { get; set; }
    public GQLResponseError[] errors { get; set; }
}
internal class GQLUpdatePrinterResponseData
{
    public bool updatePrinter { get; set; }
}

//DeletePrinterJob Response Content
internal class GQLDeletePrinterJobResponse
{
    public GQLDeletePrinterJobResponseData data { get; set; }
    public GQLResponseError[] errors { get; set; }
}
internal class GQLDeletePrinterJobResponseData
{
    public bool? deletePrinterJob { get; set; }
}

//NextPrinterJob Response Content
internal class GQLNextPrinterJobResponse
{
    public GQLNextPrinterJobResponseData data { get; set; }
    public GQLResponseError[] errors { get; set; }
}
internal class GQLNextPrinterJobResponseData
{
    public JObject nextPrinterJob { get; set; }
}

//Error Response Content
internal class GQLResponseError
{
    public string message { get; set; }
    public GQLResponseErrorExtensions extensions { get; set; }
}
internal class GQLResponseErrorExtensions
{
    public string code { get; set; }
    public GQLResponseException exception { get; set; }

}
internal class GQLResponseException
{
    public JObject errors { get; set; }
}
