using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using codecrafters_http_server.Utils;
using codecrafters_http_server.Utils.Encoding;

namespace codecrafters_http_server;

public class HttpResponse
{
    public string HttpVersion { get; private set; } = "HTTP/1.1";
    public HttpStatusCode StatusCode { get; private set; }
    public Dictionary<string, string> Headers { get; private set; } = new();
    public byte[]? BodyRaw { get; private set; }

    public byte[] GetRawResponse()
    {
        var headerStringBuilder = new StringBuilder();
        headerStringBuilder.Append($"{HttpVersion} {(int)StatusCode} {GetReasonPhrase(StatusCode)}\r\n");

        foreach (var header in Headers)
            headerStringBuilder.Append($"{header.Key}: {header.Value}\r\n");

        headerStringBuilder.Append("\r\n");

        var headerBytes = Encoding.ASCII.GetBytes(headerStringBuilder.ToString());

        Console.WriteLine($"BodyRaw Length : {BodyRaw?.Length}");
        if (BodyRaw == null || BodyRaw.Length == 0)
            return headerBytes;


        byte[] fullResponse = new byte[headerBytes.Length + BodyRaw.Length];
        Buffer.BlockCopy(headerBytes, 0, fullResponse, 0, headerBytes.Length);
        Buffer.BlockCopy(BodyRaw, 0, fullResponse, headerBytes.Length, BodyRaw.Length);
        Console.WriteLine($"FullResponse Length : {fullResponse.Length}");
        Console.WriteLine($"FullResponse : {Encoding.UTF8.GetString(fullResponse)}");
        return fullResponse;
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
        public HttpResponseBuilder SetBodyRaw(byte[] bodyRaw)
        {
            if (bodyRaw.Length == 0)
                return this;

            SetHeader("Content-Length", bodyRaw.Length.ToString());
            SetHeaderContentType(bodyRaw);
            _httpResponse.BodyRaw = bodyRaw;
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

        public HttpResponseBuilder SetContentEncodingHeader(EncodingHandled encoding)
        {
            switch (encoding)
            {
                case EncodingHandled.Gzip:
                    SetHeader("Content-Encoding", "gzip");
                    break;
                default: 
                    break;
            }

            return this;
        }

        private void SetHeaderContentType(byte[] rawBody)
        {
            if (_httpResponse.Headers.ContainsKey("Content-Type"))
                return;

            string contentType;

            // ✅ 1️⃣ Check common file signatures (Magic Numbers)
            if (rawBody.Length >= 4)
            {
                if (rawBody[0] == 0x1F && rawBody[1] == 0x8B) contentType = "application/gzip"; // Gzip
                else if (rawBody[0] == 0x50 && rawBody[1] == 0x4B) contentType = "application/zip"; // ZIP
                else if (rawBody[0] == 0xFF && rawBody[1] == 0xD8) contentType = "image/jpeg"; // JPEG
                else if (rawBody[0] == 0x89 && rawBody[1] == 0x50) contentType = "image/png"; // PNG
                else if (rawBody[0] == 0x25 && rawBody[1] == 0x50) contentType = "application/pdf"; // PDF
                else if (rawBody[0] == 0x47 && rawBody[1] == 0x49 && rawBody[2] == 0x46)
                    contentType = "image/gif"; // GIF
                else
                {
                    // ✅ 2️⃣ Try to decode as UTF-8 text and detect JSON/XML/HTML
                    try
                    {
                        string text = Encoding.UTF8.GetString(rawBody);

                        if (Regex.IsMatch(text.Trim(), @"^\s*(\{.*\}|\[.*\])\s*$"))
                            contentType = "application/json"; // JSON

                        else if (Regex.IsMatch(text.Trim(), @"^\s*<\?xml|<\w+>.*</\w+>\s*$"))
                            contentType = "application/xml"; // XML

                        else if (text.Contains("<html>", StringComparison.OrdinalIgnoreCase))
                            contentType = "text/html"; // HTML

                        else if (text.Contains(",") && text.Split('\n').Length > 1)
                            contentType = "text/csv"; // CSV

                        else if (text.TrimStart().StartsWith("function") || text.Contains("console.log"))
                            contentType = "application/javascript"; // JavaScript

                        else
                            contentType = "text/plain"; // Default text
                    }
                    catch (Exception)
                    {
                        contentType = "application/octet-stream"; // Fallback for unknown binary files
                    }
                }
            }
            else
            {
                contentType = "application/octet-stream"; // Fallback for unknown small files
            }

            SetHeader("Content-Type", contentType);
        }


        public HttpResponse Build()
        {
            return _httpResponse;
        }
    }
}