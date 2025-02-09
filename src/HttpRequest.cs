using System.Text;

namespace codecrafters_http_server;

public class HttpRequest
{
    public string HttpMethod { get; private set; } = null!;
    public string RequestTarget { get; private set; } = null!;
    public string HttpVersion { get; private set; } = "HTTP/1.1";
        
    public Dictionary<string, string> Headers { get; private set; } = new();
    public static HttpRequest? Parse(byte[] httpRequest, int httpRequestLength)
    {
        return Parse(Encoding.ASCII.GetString(httpRequest, 0, httpRequestLength));
    }

    private static HttpRequest? Parse(string httpRequest)
    {
        var httpRequestParts = httpRequest.Split("\r\n");
        if(httpRequestParts.Length < 1)
            return null;
              
        var httpRequestLine = httpRequestParts[0].Split(" ");
        if(httpRequestLine.Length < 3)
            return null;
              
        return new HttpRequestBuilder()
            .SetHttpMethod(httpRequestLine[0])
            .SetRequestTarget(httpRequestLine[1])
            .SetHttpVersion(httpRequestLine[2])
            .Build();
    }
        
    public class HttpRequestBuilder
    {
        private readonly HttpRequest? _httpRequest = new();

        public HttpRequestBuilder SetHttpMethod(string httpMethod)
        {
            _httpRequest!.HttpMethod = httpMethod;
            return this;
        }
        public HttpRequestBuilder SetRequestTarget(string requestTarget)
        {
            _httpRequest!.RequestTarget = requestTarget;
            return this;
        } 
        public HttpRequestBuilder SetHttpVersion(string httpVersion)
        {
            _httpRequest!.HttpVersion = httpVersion;
            return this;
        } 
        public HttpRequest? Build()
        {
            return _httpRequest;
        }
    }
}