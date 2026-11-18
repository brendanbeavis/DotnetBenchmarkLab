namespace DotNetPerformanceLab.Data.Json;

public static class JsonPayloadStreamFactory
{
    public static Stream CreateReadOnly(byte[] payloadUtf8)
    {
        ArgumentNullException.ThrowIfNull(payloadUtf8);
        return new MemoryStream(payloadUtf8, writable: false);
    }
}
