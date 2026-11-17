using System.Text;
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
}
