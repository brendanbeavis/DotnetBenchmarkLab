using DotNetPerformanceLab.Reporting.Json;

namespace DotNetPerformanceLab.Data.Json;

public sealed class JsonCorrectnessVerifier
{
    private readonly JsonPayloadProcessingService _service;

    public JsonCorrectnessVerifier(JsonPayloadProcessingService service)
    {
        _service = service;
    }

    public JsonCorrectnessVerificationResult VerifyAllEquivalent(byte[] payloadUtf8, decimal decimalTolerance = 0.01m)
    {
        Dictionary<JsonProcessingApproach, JsonParserOnlyResultDto> parserOnlyResults = new()
        {
            [JsonProcessingApproach.Newtonsoft] = _service.ParseOnlyNewtonsoft(payloadUtf8),
            [JsonProcessingApproach.SystemTextJson] = _service.ParseOnlySystemTextJson(payloadUtf8),
            [JsonProcessingApproach.SystemTextJsonSourceGen] = _service.ParseOnlySystemTextJsonSourceGenerated(payloadUtf8),
            [JsonProcessingApproach.SystemTextJsonStreaming] = _service.ParseOnlySystemTextJsonStreaming(payloadUtf8)
        };

        Dictionary<JsonProcessingApproach, JsonProcessingResultDto> endToEndResults = new()
        {
            [JsonProcessingApproach.Newtonsoft] = _service.ProcessNewtonsoft(payloadUtf8),
            [JsonProcessingApproach.SystemTextJson] = _service.ProcessSystemTextJson(payloadUtf8),
            [JsonProcessingApproach.SystemTextJsonSourceGen] = _service.ProcessSystemTextJsonSourceGenerated(payloadUtf8),
            [JsonProcessingApproach.SystemTextJsonStreaming] = _service.ProcessSystemTextJsonStreaming(payloadUtf8)
        };

        JsonParserOnlyResultDto parserBaseline = parserOnlyResults[JsonProcessingApproach.SystemTextJson];
        foreach ((JsonProcessingApproach approach, JsonParserOnlyResultDto candidate) in parserOnlyResults)
        {
            AssertParserOnlyEquivalent(parserBaseline, candidate, approach, decimalTolerance);
        }

        JsonProcessingResultDto endToEndBaseline = endToEndResults[JsonProcessingApproach.SystemTextJson];
        foreach ((JsonProcessingApproach approach, JsonProcessingResultDto candidate) in endToEndResults)
        {
            AssertEndToEndEquivalent(endToEndBaseline, candidate, approach, decimalTolerance);
        }

        return new JsonCorrectnessVerificationResult(parserOnlyResults, endToEndResults, decimalTolerance);
    }

    private static void AssertParserOnlyEquivalent(JsonParserOnlyResultDto expected, JsonParserOnlyResultDto actual, JsonProcessingApproach approach, decimal tolerance)
    {
        if (expected.OrderCount != actual.OrderCount
            || expected.TotalLineItemCount != actual.TotalLineItemCount
            || expected.TotalUnits != actual.TotalUnits)
        {
            throw new InvalidOperationException($"Parser-only structural mismatch for {approach}.");
        }

        if (Math.Abs(expected.GrossSales - actual.GrossSales) > tolerance
            || Math.Abs(expected.DiscountTotal - actual.DiscountTotal) > tolerance
            || Math.Abs(expected.NetSales - actual.NetSales) > tolerance)
        {
            throw new InvalidOperationException($"Parser-only monetary mismatch for {approach}.");
        }
    }

    private static void AssertEndToEndEquivalent(JsonProcessingResultDto expected, JsonProcessingResultDto actual, JsonProcessingApproach approach, decimal tolerance)
    {
        if (expected.OrderCount != actual.OrderCount
            || expected.TotalUnits != actual.TotalUnits)
        {
            throw new InvalidOperationException($"End-to-end aggregate mismatch for {approach}.");
        }

        if (Math.Abs(expected.GrossSales - actual.GrossSales) > tolerance
            || Math.Abs(expected.DiscountTotal - actual.DiscountTotal) > tolerance
            || Math.Abs(expected.NetSales - actual.NetSales) > tolerance)
        {
            throw new InvalidOperationException($"End-to-end monetary mismatch for {approach}.");
        }

        if (expected.Orders.Count != actual.Orders.Count)
        {
            throw new InvalidOperationException($"End-to-end order count mismatch for {approach}.");
        }

        for (int index = 0; index < expected.Orders.Count; index++)
        {
            JsonOrderResponseDto expectedOrder = expected.Orders[index];
            JsonOrderResponseDto actualOrder = actual.Orders[index];

            if (expectedOrder.OrderId != actualOrder.OrderId
                || expectedOrder.CustomerId != actualOrder.CustomerId
                || !string.Equals(expectedOrder.CustomerName, actualOrder.CustomerName, StringComparison.Ordinal)
                || !string.Equals(expectedOrder.Status, actualOrder.Status, StringComparison.Ordinal)
                || expectedOrder.OrderDateUtc != actualOrder.OrderDateUtc
                || expectedOrder.TotalUnits != actualOrder.TotalUnits
                || expectedOrder.DistinctProductCount != actualOrder.DistinctProductCount)
            {
                throw new InvalidOperationException($"End-to-end order header mismatch for {approach}, order index {index}.");
            }

            if (Math.Abs(expectedOrder.GrossTotal - actualOrder.GrossTotal) > tolerance
                || Math.Abs(expectedOrder.DiscountTotal - actualOrder.DiscountTotal) > tolerance
                || Math.Abs(expectedOrder.NetTotal - actualOrder.NetTotal) > tolerance)
            {
                throw new InvalidOperationException($"End-to-end order monetary mismatch for {approach}, orderId {expectedOrder.OrderId}.");
            }
        }
    }
}
