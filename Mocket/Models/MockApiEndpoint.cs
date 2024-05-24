using Newtonsoft.Json;
namespace Mocket.Models;

public class MockApiEndpoint
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("userId")]
    public string UserId { get; set; }

    [JsonProperty("urlPath")]
    public string UrlPath { get; set; }

    [JsonProperty("method")]
    public string Method { get; set; }

    [JsonProperty("responseStatus")]
    public int ResponseStatus { get; set; }

    [JsonProperty("responseHeaders")]
    public Dictionary<string, string> ResponseHeaders { get; set; }

    [JsonProperty("responseBody")]
    public string ResponseBody { get; set; }
}