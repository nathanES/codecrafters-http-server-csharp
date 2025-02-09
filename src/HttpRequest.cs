using System.Text;

namespace codecrafters_http_server;

public class HttpRequest
{
    public string HttpMethod { get; private set; } = null!;
    public string RequestTarget { get; private set; } = null!;
    public string HttpVersion { get; private set; } = "HTTP/1.1";
    public Dictionary<string, string> Headers { get; private set; } = new();
    public string? Body { get; private set; }
    
    public static HttpRequest? Parse(byte[] httpRequest, int httpRequestLength)
    {
        return Parse(Encoding.ASCII.GetString(httpRequest, 0, httpRequestLength));
    }

    private static HttpRequest? Parse(string rawRequest)
    {
        if (string.IsNullOrWhiteSpace(rawRequest))
            return null;
        
        var requestBuilder = new HttpRequestBuilder();
        var sections = rawRequest.Split(new[]{"\r\n\r\n", "\n\n"},2, StringSplitOptions.None);
        var headerSection = sections[0]; // RequestLine + Headers
        var body = sections.Length > 1 ? sections[1] : string.Empty;
        
        var headerLines = headerSection.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (headerLines.Length < 1)
            return null;

        var requestLineTokens = headerLines[0].Split(" ");
        if(requestLineTokens.Length < 3)
            return null;

        requestBuilder
            .SetHttpMethod(requestLineTokens[0])
            .SetRequestTarget(requestLineTokens[1])
            .SetHttpVersion(requestLineTokens[2]);
        
        foreach (var header in headerLines[1..])
        {
            string[] headerKeyValue = header.Split(":",2);
            if(headerKeyValue.Length < 2)
                continue;
            requestBuilder.SetHeader(headerKeyValue[0].Trim(), headerKeyValue[1].Trim());
        } 
        if(!string.IsNullOrWhiteSpace(body))
            requestBuilder.SetBody(body);
        
        return requestBuilder
            .Build();
    }
        
    public class HttpRequestBuilder
    {
        private readonly HttpRequest _httpRequest = new();

        public HttpRequestBuilder SetHttpMethod(string httpMethod)
        {
            _httpRequest.HttpMethod = httpMethod;
            return this;
        }
        public HttpRequestBuilder SetRequestTarget(string requestTarget)
        {
            _httpRequest.RequestTarget = requestTarget;
            return this;
        } 
        public HttpRequestBuilder SetHttpVersion(string httpVersion)
        {
            _httpRequest.HttpVersion = httpVersion;
            return this;
        } 
        public HttpRequestBuilder SetHeader(string name, string value)
        {
            if (_httpRequest.Headers.ContainsKey(name))
                _httpRequest.Headers[name] = value;
            else
                _httpRequest.Headers.Add(name, value);
            return this;
        }

        public HttpRequestBuilder SetBody(string body)
        {
            _httpRequest.Body = body;
            return this;
        }
        public HttpRequest? Build()
        {
            return _httpRequest;
        }
    }
}