using System.IO.Compression;

namespace codecrafters_http_server.Utils.Encoding;

public static class EncodingHelper
{
    public static byte[] CompressIfNeeded(string body, EncodingHandled encoding)
    {
        if(string.IsNullOrEmpty(body))
            return [];
        Console.WriteLine($"Body: {body}"); 
        var bodyBytes = System.Text.Encoding.ASCII.GetBytes(body);
        switch (encoding)
        {
            case EncodingHandled.Gzip:
                Console.WriteLine("Gzip Compression");
                return GzipCompress(bodyBytes);
            case EncodingHandled.None :
            default:
                return bodyBytes;
        }
    }

    private static byte[] GzipCompress(byte[] bodyBytes)
    {
        using var memoryStream = new MemoryStream();
        using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true)) // ✅ Keep stream open
        {
            gZipStream.Write(bodyBytes, 0, bodyBytes.Length);
            gZipStream.Flush(); // ✅ Ensures all data is written
        }

        return memoryStream.ToArray(); // ✅ Now contains complete Gzip data
    }

}