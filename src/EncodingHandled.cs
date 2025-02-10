namespace codecrafters_http_server;

public enum EncodingHandled
{
    None,
    Gzip
}

public static class EncodingHandledExtensions
{
    public static EncodingHandled GetEncoding(this HttpRequest httpRequest)
    {
        if (!httpRequest.Headers.TryGetValue("Accept-Encoding", out var encodingValue) 
            || string.IsNullOrWhiteSpace(encodingValue))
            return EncodingHandled.None;
       
        if(encodingValue.ToUpper().Contains("GZIP"))
            return EncodingHandled.Gzip;
        
        return EncodingHandled.None;
    } 
}