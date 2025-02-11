using System.IO.Compression;

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
                return GzipCompress(bodyBytes);
            case EncodingHandled.None :
            default:
                return bodyBytes;
        }
    }

    private static byte[] GzipCompress(byte[] bodyBytes)
    {
        using var memoryStream = new MemoryStream();
        using var gZipStream = new GZipStream(memoryStream, CompressionLevel.Optimal);
        gZipStream.Write(bodyBytes, 0, bodyBytes.Length);
        return memoryStream.ToArray();
    }
}