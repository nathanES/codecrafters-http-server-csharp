using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace codecrafters_http_server;

public class HttpResponse
{
    public string HttpVersion { get; private set; } = "HTTP/1.1";
    public HttpStatusCode StatusCode { get; private set; }
    public Dictionary<string, string> Headers { get; private set; } = new();
    public string? Body { get; private set; }

    public string Format()
    {
        var responseBuilder = new StringBuilder();
        responseBuilder.Append($"{HttpVersion} {(int)StatusCode} {GetReasonPhrase(StatusCode)}\r\n");


        foreach (var header in Headers)
            responseBuilder.Append($"{header.Key}: {header.Value}\r\n");
            
        responseBuilder.Append("\r\n");
        if (Body != null)
            responseBuilder.Append(Body);
        return responseBuilder.ToString();
    }
    private static readonly Dictionary<HttpStatusCode, string> StatusReasonPhrases = new()
    {
        { HttpStatusCode.OK, "OK" },
        { HttpStatusCode.Created, "Created" },
        { HttpStatusCode.NoContent, "No Content" },
        { HttpStatusCode.BadRequest, "Bad Request" },
        { HttpStatusCode.Unauthorized, "Unauthorized" },
        { HttpStatusCode.Forbidden, "Forbidden" },
        { HttpStatusCode.NotFound, "Not Found" },
        { HttpStatusCode.InternalServerError, "Internal Server Error" },
        { HttpStatusCode.NotImplemented, "Not Implemented" }
    };

    private static string GetReasonPhrase(HttpStatusCode statusCode)
    {
        return StatusReasonPhrases.TryGetValue(statusCode, out var phrase) ? phrase : "Unknown Status";
    }
 
    public class HttpResponseBuilder
    {
        private readonly HttpResponse _httpResponse = new();

        public HttpResponseBuilder SetHttpVersion(string httpVersion)
        {
            _httpResponse.HttpVersion = httpVersion;
            return this;
        }

        public HttpResponseBuilder SetStatusCode(HttpStatusCode statusCode)
        {
            _httpResponse.StatusCode = statusCode;
            return this;
        }

        public HttpResponseBuilder SetHeader(string name, string value)
        {
            if (_httpResponse.Headers.ContainsKey(name))
                _httpResponse.Headers[name] = value;
            else
                _httpResponse.Headers.Add(name, value);
            return this;
        }

        public HttpResponseBuilder SetBody(string body)
        {
            _httpResponse.Body = body;
            
            SetHeader("Content-Length", Encoding.UTF8.GetBytes(body).Length.ToString());
            SetHeaderContentType(body); 
            
            return this;
        }

        private void SetHeaderContentType(string body)
        {
            if (_httpResponse.Headers.ContainsKey("Content-Type"))
                return;
            
            string contentType;

            // Detect JSON (Improved)
            if (Regex.IsMatch(body.Trim(), @"^\s*(\{.*\}|\[.*\])\s*$"))
                contentType = "application/json";

            // Detect XML (Improved)
            else if (Regex.IsMatch(body.Trim(), @"^\s*<\?xml|<\w+>.*</\w+>\s*$"))
                contentType = "application/xml";

            // Detect HTML
            else if (body.Contains("<html>", StringComparison.OrdinalIgnoreCase))
                contentType = "text/html";

            // Detect CSV
            else if (body.Contains(",") && body.Split('\n').Length > 1)
                contentType = "text/csv";

            // Detect JavaScript
            else if (body.TrimStart().StartsWith("function") || body.Contains("console.log"))
                contentType = "application/javascript";

            // Default to Plain Text
            else
                contentType = "text/plain";
            
            SetHeader("Content-Type", contentType);
        }

        public HttpResponse Build()
        {
            return _httpResponse;
        }
            
    }
        
}