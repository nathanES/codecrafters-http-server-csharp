namespace codecrafters_http_server.Utils.Encoding;

public static class EncodingHelper
{
    public static byte[] CompressIfNeeded(string body, EncodingHandled encoding)
    {
        if(string.IsNullOrEmpty(body))
            return [];
        
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
        switch (encoding)
        {
            case EncodingHandled.Gzip:
                return bodyBytes;
            case EncodingHandled.None :
            default:
                return bodyBytes;
        }
    }
}