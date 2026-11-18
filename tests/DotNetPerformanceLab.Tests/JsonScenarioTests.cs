using System.Text;
using System.Text.Json;
using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.Generators;
using DotNetPerformanceLab.Data.Json;
using DotNetPerformanceLab.Models.Json;
using DotNetPerformanceLab.Reporting.Json;

namespace DotNetPerformanceLab.Tests;

public class JsonScenarioTests
{
    [Fact]
    public void FixtureGeneration_IsDeterministic()
    {
        JsonPayloadFixture first = JsonPayloadFixtureGenerator.Create(DatasetSize.Small);
        JsonPayloadFixture second = JsonPayloadFixtureGenerator.Create(DatasetSize.Small);

        Assert.Equal(first.PayloadText, second.PayloadText);
        Assert.Equal(first.PayloadUtf8, second.PayloadUtf8);
    }

    [Fact]
    public void AllApproaches_ProduceEquivalentResults()
    {
        JsonPayloadFixture fixture = JsonPayloadFixtureGenerator.Create(DatasetSize.Small);
        JsonPayloadProcessingService service = new();
        JsonCorrectnessVerifier verifier = new(service);

        JsonCorrectnessVerificationResult result = verifier.VerifyAllEquivalent(fixture.PayloadUtf8);

        Assert.Equal(4, result.ParserOnlyResults.Count);
        Assert.Equal(4, result.EndToEndResults.Count);
    }

    [Fact]
    public void NestedCustomerAndLineItemValues_ArePreserved()
    {
        JsonPayloadFixture fixture = JsonPayloadFixtureGenerator.Create(DatasetSize.Small);
        JsonPayloadProcessingService service = new();

        ApiOrdersPayloadDto payload = service.DeserializeSystemTextJson(fixture.PayloadUtf8);

        Assert.NotEmpty(payload.Orders);
        ApiOrderDto firstOrder = payload.Orders[0];
        Assert.NotNull(firstOrder.Customer);
        Assert.NotEmpty(firstOrder.Customer.Name);
        Assert.NotEmpty(firstOrder.LineItems);
        Assert.True(firstOrder.LineItems[0].ProductId > 0);
    }

    [Fact]
    public void DecimalValues_AreHandledConsistently()
    {
        JsonPayloadFixture fixture = JsonPayloadFixtureGenerator.Create(DatasetSize.Small);
        JsonPayloadProcessingService service = new();

        var stj = service.ProcessSystemTextJson(fixture.PayloadUtf8);
        var sourceGen = service.ProcessSystemTextJsonSourceGenerated(fixture.PayloadUtf8);
        var streaming = service.ProcessSystemTextJsonStreaming(fixture.PayloadUtf8);

        Assert.Equal(stj.GrossSales, sourceGen.GrossSales);
        Assert.Equal(stj.DiscountTotal, sourceGen.DiscountTotal);
        Assert.Equal(stj.NetSales, sourceGen.NetSales);

        Assert.Equal(stj.GrossSales, streaming.GrossSales);
        Assert.Equal(stj.DiscountTotal, streaming.DiscountTotal);
        Assert.Equal(stj.NetSales, streaming.NetSales);
    }

    [Fact]
    public void NullOptionalFields_AreHandledAsDocumented()
    {
        byte[] payload = Encoding.UTF8.GetBytes("""
            {
              "metadata": { "requestId": "x", "generatedAtUtc": "2024-01-01T00:00:00Z", "sourceSystem": "test" },
              "orders": [
                {
                  "orderId": 1,
                  "status": "Completed",
                  "orderDateUtc": "2024-01-02T00:00:00Z",
                  "customer": { "customerId": 1, "name": "A", "email": "a@test.local", "region": "North" },
                  "lineItems": [
                    { "productId": 11, "sku": "SKU-000011", "description": "Desc", "quantity": 1, "unitPrice": 10.00, "discountPercent": 0.10, "notes": null }
                  ]
                }
              ]
            }
            """);

        JsonPayloadProcessingService service = new();
        ApiOrdersPayloadDto payloadDto = service.DeserializeSystemTextJson(payload);

        Assert.Null(payloadDto.Orders[0].LineItems[0].Notes);
    }

    [Fact]
    public void MissingOptionalFields_AreHandledAsDocumented()
    {
        byte[] payload = Encoding.UTF8.GetBytes("""
            {
              "metadata": { "requestId": "x", "generatedAtUtc": "2024-01-01T00:00:00Z", "sourceSystem": "test" },
              "orders": [
                {
                  "orderId": 1,
                  "status": "Completed",
                  "orderDateUtc": "2024-01-02T00:00:00Z",
                  "customer": { "customerId": 1, "name": "A", "email": "a@test.local", "region": "North" },
                  "lineItems": [
                    { "productId": 11, "sku": "SKU-000011", "description": "Desc", "quantity": 1, "unitPrice": 10.00, "discountPercent": 0.10 }
                  ]
                }
              ]
            }
            """);

        JsonPayloadProcessingService service = new();
        ApiOrdersPayloadDto payloadDto = service.DeserializeSystemTextJson(payload);

        Assert.Null(payloadDto.Orders[0].LineItems[0].Notes);
    }

    [Fact]
    public void EmptyArrays_AreHandled()
    {
        byte[] payload = Encoding.UTF8.GetBytes("""
            {
              "metadata": { "requestId": "x", "generatedAtUtc": "2024-01-01T00:00:00Z", "sourceSystem": "test" },
              "orders": []
            }
            """);

        JsonPayloadProcessingService service = new();
        JsonCorrectnessVerifier verifier = new(service);
        JsonCorrectnessVerificationResult result = verifier.VerifyAllEquivalent(payload);

        Assert.All(result.EndToEndResults.Values, x => Assert.Equal(0, x.OrderCount));
        Assert.All(result.ParserOnlyResults.Values, x => Assert.Equal(0, x.OrderCount));
    }

    [Fact]
    public void EmptyPayload_CanBeHandledByDocumentedDefaults()
    {
        byte[] payload = Encoding.UTF8.GetBytes("{}");

        JsonPayloadProcessingService service = new();
        var parserOnly = service.ParseOnlySystemTextJson(payload);
        var endToEnd = service.ProcessSystemTextJson(payload);

        Assert.Equal(0, parserOnly.OrderCount);
        Assert.Equal(0, endToEnd.OrderCount);
    }

    [Fact]
    public void StreamingMatchesOtherApproaches()
    {
        JsonPayloadFixture fixture = JsonPayloadFixtureGenerator.Create(DatasetSize.Small);
        JsonPayloadProcessingService service = new();

        JsonProcessingResultDto standard = service.ProcessSystemTextJson(fixture.PayloadUtf8);
        JsonProcessingResultDto streaming = service.ProcessSystemTextJsonStreaming(fixture.PayloadUtf8);

        Assert.Equal(standard.OrderCount, streaming.OrderCount);
        Assert.Equal(standard.TotalUnits, streaming.TotalUnits);
        Assert.Equal(standard.GrossSales, streaming.GrossSales);
        Assert.Equal(standard.DiscountTotal, streaming.DiscountTotal);
        Assert.Equal(standard.NetSales, streaming.NetSales);
    }

    [Fact]
    public void Streaming_NoOrdersProperty_ReturnsZeroResults()
    {
        byte[] payload = Encoding.UTF8.GetBytes("""
            {
              "metadata": { "requestId": "x", "generatedAtUtc": "2024-01-01T00:00:00Z", "sourceSystem": "test" }
            }
            """);

        JsonPayloadProcessingService service = new();
        using Stream stream = new ChunkedReadStream(payload, maxBytesPerRead: 2);

        JsonParserOnlyResultDto parserOnly = service.ParseOnlySystemTextJsonStreaming(stream, readBufferSize: 3);

        Assert.Equal(0, parserOnly.OrderCount);
        Assert.Equal(0, parserOnly.TotalLineItemCount);
        Assert.Equal(0, parserOnly.TotalUnits);
        Assert.Equal(0m, parserOnly.GrossSales);
        Assert.Equal(0m, parserOnly.DiscountTotal);
        Assert.Equal(0m, parserOnly.NetSales);
    }

    [Fact]
    public void Streaming_EmptyOrdersArray_ReturnsZeroResults()
    {
        byte[] payload = Encoding.UTF8.GetBytes("""
            {
              "metadata": { "requestId": "x", "generatedAtUtc": "2024-01-01T00:00:00Z", "sourceSystem": "test" },
              "orders": []
            }
            """);

        JsonPayloadProcessingService service = new();
        using Stream stream = new ChunkedReadStream(payload, maxBytesPerRead: 1);

        JsonProcessingResultDto result = service.ProcessSystemTextJsonStreaming(stream, readBufferSize: 2);

        Assert.Equal(0, result.OrderCount);
        Assert.Empty(result.Orders);
    }

    [Fact]
    public void Streaming_OneOrder_WithTinyBuffer_MatchesStandardProcessing()
    {
        byte[] payload = Encoding.UTF8.GetBytes("""
            {
              "metadata": { "requestId": "x", "generatedAtUtc": "2024-01-01T00:00:00Z", "sourceSystem": "test" },
              "orders": [
                {
                  "orderId": 1,
                  "status": "Completed",
                  "orderDateUtc": "2024-01-02T00:00:00Z",
                  "customer": { "customerId": 1, "name": "Escaped \"Name\"", "email": "a@test.local", "region": "North" },
                  "lineItems": [
                    { "productId": 11, "sku": "SKU-000011", "description": "Desc", "quantity": 1, "unitPrice": 10.00, "discountPercent": 0.10, "notes": "line with \\ slash" }
                  ]
                }
              ]
            }
            """);

        JsonPayloadProcessingService service = new();
        JsonProcessingResultDto expected = service.ProcessSystemTextJson(payload);

        using Stream stream = new ChunkedReadStream(payload, maxBytesPerRead: 1);
        JsonProcessingResultDto streaming = service.ProcessSystemTextJsonStreaming(stream, readBufferSize: 2);

        Assert.Equal(expected.OrderCount, streaming.OrderCount);
        Assert.Equal(expected.TotalUnits, streaming.TotalUnits);
        Assert.Equal(expected.GrossSales, streaming.GrossSales);
        Assert.Equal(expected.DiscountTotal, streaming.DiscountTotal);
        Assert.Equal(expected.NetSales, streaming.NetSales);
        Assert.Equal(expected.Orders[0].CustomerName, streaming.Orders[0].CustomerName);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(3, 5)]
    [InlineData(7, 8)]
    public void Streaming_MultipleOrders_TinyChunks_MatchesStandardProcessing(int maxBytesPerRead, int readBufferSize)
    {
        JsonPayloadFixture fixture = JsonPayloadFixtureGenerator.Create(DatasetSize.Small);
        JsonPayloadProcessingService service = new();

        JsonProcessingResultDto expected = service.ProcessSystemTextJson(fixture.PayloadUtf8);

        using Stream stream = new ChunkedReadStream(fixture.PayloadUtf8, maxBytesPerRead);
        JsonProcessingResultDto streaming = service.ProcessSystemTextJsonStreaming(stream, readBufferSize);

        Assert.Equal(expected.OrderCount, streaming.OrderCount);
        Assert.Equal(expected.TotalUnits, streaming.TotalUnits);
        Assert.Equal(expected.GrossSales, streaming.GrossSales);
        Assert.Equal(expected.DiscountTotal, streaming.DiscountTotal);
        Assert.Equal(expected.NetSales, streaming.NetSales);
    }

    [Fact]
    public void Streaming_EmptyPayload_ThrowsJsonException()
    {
        JsonPayloadProcessingService service = new();
        using Stream stream = new ChunkedReadStream([], maxBytesPerRead: 1);

        Assert.ThrowsAny<JsonException>(() => service.ProcessSystemTextJsonStreaming(stream, readBufferSize: 4));
    }

    [Fact]
    public void Streaming_MalformedJson_ThrowsJsonException()
    {
        byte[] payload = Encoding.UTF8.GetBytes("""
            {
              "orders": [
                {
                  "orderId": 1,
                  "status": "Completed",
                  "orderDateUtc": "2024-01-02T00:00:00Z",
                  "customer": { "customerId": 1, "name": "A", "email": "a@test.local", "region": "North" },
                  "lineItems": [
                    { "productId": 11, "sku": "SKU-000011", "description": "Desc", "quantity": 1, "unitPrice": 10.00, "discountPercent": 0.10 }
                  ]
                }
              ]

            """);

        JsonPayloadProcessingService service = new();
        using Stream stream = new ChunkedReadStream(payload, maxBytesPerRead: 2);

        Assert.ThrowsAny<JsonException>(() => service.ParseOnlySystemTextJsonStreaming(stream, readBufferSize: 3));
    }

    [Fact]
    public void Streaming_TruncatedJson_ThrowsJsonException()
    {
        byte[] payload = Encoding.UTF8.GetBytes("""
            {
              "orders": [
                {
                  "orderId": 1,
                  "status": "Completed",
                  "orderDateUtc": "2024-01-02T00:00:00Z",
                  "customer": { "customerId": 1, "name": "A", "email": "a@test.local", "region": "North" },
                  "lineItems": [
                    { "productId": 11, "sku": "SKU-000011", "description": "Desc", "quantity": 1, "unitPrice": 10.00, "discountPercent": 0.10 }
                  ]
            """);

        JsonPayloadProcessingService service = new();
        using Stream stream = new ChunkedReadStream(payload, maxBytesPerRead: 3);

        Assert.ThrowsAny<JsonException>(() => service.ProcessSystemTextJsonStreaming(stream, readBufferSize: 4));
    }

    private sealed class ChunkedReadStream : Stream
    {
        private readonly byte[] _data;
        private readonly int _maxBytesPerRead;
        private int _position;

        public ChunkedReadStream(byte[] data, int maxBytesPerRead)
        {
            _data = data;
            _maxBytesPerRead = maxBytesPerRead;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _data.Length;
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int available = _data.Length - _position;
            if (available <= 0)
            {
                return 0;
            }

            int toCopy = Math.Min(available, Math.Min(count, _maxBytesPerRead));
            Buffer.BlockCopy(_data, _position, buffer, offset, toCopy);
            _position += toCopy;
            return toCopy;
        }

        public override int Read(Span<byte> buffer)
        {
            int available = _data.Length - _position;
            if (available <= 0)
            {
                return 0;
            }

            int toCopy = Math.Min(available, Math.Min(buffer.Length, _maxBytesPerRead));
            _data.AsSpan(_position, toCopy).CopyTo(buffer);
            _position += toCopy;
            return toCopy;
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
