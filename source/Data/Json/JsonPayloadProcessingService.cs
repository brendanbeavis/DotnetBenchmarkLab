using System.Text;
using System.Text.Json;
using DotNetPerformanceLab.Models.Json;
using DotNetPerformanceLab.Reporting.Json;
using Newtonsoft.Json;

namespace DotNetPerformanceLab.Data.Json;

public sealed class JsonPayloadProcessingService
{
    public ApiOrdersPayloadDto DeserializeNewtonsoft(byte[] payloadUtf8)
    {
        string json = Encoding.UTF8.GetString(payloadUtf8);
        ApiOrdersPayloadDto? payload = JsonConvert.DeserializeObject<ApiOrdersPayloadDto>(json, JsonSerializationOptionsProvider.NewtonsoftSettings);
        return payload ?? new ApiOrdersPayloadDto();
    }

    public ApiOrdersPayloadDto DeserializeSystemTextJson(byte[] payloadUtf8)
    {
        ApiOrdersPayloadDto? payload = System.Text.Json.JsonSerializer.Deserialize<ApiOrdersPayloadDto>(payloadUtf8, JsonSerializationOptionsProvider.Standard);
        return payload ?? new ApiOrdersPayloadDto();
    }

    public ApiOrdersPayloadDto DeserializeSystemTextJsonSourceGenerated(byte[] payloadUtf8)
    {
        ApiOrdersPayloadDto? payload = System.Text.Json.JsonSerializer.Deserialize(payloadUtf8, JsonSerializationContexts.Default.ApiOrdersPayloadDto);
        return payload ?? new ApiOrdersPayloadDto();
    }

    public JsonParserOnlyResultDto ParseOnlyNewtonsoft(byte[] payloadUtf8)
    {
        ApiOrdersPayloadDto payload = DeserializeNewtonsoft(payloadUtf8);
        return BuildParserOnlyResult(payload.Orders);
    }

    public JsonParserOnlyResultDto ParseOnlySystemTextJson(byte[] payloadUtf8)
    {
        ApiOrdersPayloadDto payload = DeserializeSystemTextJson(payloadUtf8);
        return BuildParserOnlyResult(payload.Orders);
    }

    public JsonParserOnlyResultDto ParseOnlySystemTextJsonSourceGenerated(byte[] payloadUtf8)
    {
        ApiOrdersPayloadDto payload = DeserializeSystemTextJsonSourceGenerated(payloadUtf8);
        return BuildParserOnlyResult(payload.Orders);
    }

    public JsonParserOnlyResultDto ParseOnlySystemTextJsonStreaming(byte[] payloadUtf8)
    {
        int orderCount = 0;
        int totalLineItems = 0;
        int totalUnits = 0;
        decimal grossSales = 0m;
        decimal discountTotal = 0m;

        ForEachStreamedOrder(payloadUtf8, order =>
        {
            orderCount++;
            totalLineItems += order.LineItems.Count;
            foreach (ApiOrderLineItemDto line in order.LineItems)
            {
                totalUnits += line.Quantity;
                decimal gross = line.UnitPrice * line.Quantity;
                decimal discount = gross * line.DiscountPercent;
                grossSales += gross;
                discountTotal += discount;
            }
        });

        decimal roundedGross = RoundMoney(grossSales);
        decimal roundedDiscount = RoundMoney(discountTotal);
        decimal net = RoundMoney(roundedGross - roundedDiscount);

        return new JsonParserOnlyResultDto(orderCount, totalLineItems, totalUnits, roundedGross, roundedDiscount, net);
    }

    public JsonProcessingResultDto ProcessNewtonsoft(byte[] payloadUtf8)
    {
        ApiOrdersPayloadDto payload = DeserializeNewtonsoft(payloadUtf8);
        return Transform(payload.Orders);
    }

    public JsonProcessingResultDto ProcessSystemTextJson(byte[] payloadUtf8)
    {
        ApiOrdersPayloadDto payload = DeserializeSystemTextJson(payloadUtf8);
        return Transform(payload.Orders);
    }

    public JsonProcessingResultDto ProcessSystemTextJsonSourceGenerated(byte[] payloadUtf8)
    {
        ApiOrdersPayloadDto payload = DeserializeSystemTextJsonSourceGenerated(payloadUtf8);
        return Transform(payload.Orders);
    }

    public JsonProcessingResultDto ProcessSystemTextJsonStreaming(byte[] payloadUtf8)
    {
        List<JsonOrderResponseDto> transformedOrders = [];
        int totalUnits = 0;
        decimal grossSales = 0m;
        decimal discountTotal = 0m;

        ForEachStreamedOrder(payloadUtf8, order =>
        {
            JsonOrderResponseDto transformed = TransformOrder(order);
            transformedOrders.Add(transformed);
            totalUnits += transformed.TotalUnits;
            grossSales += transformed.GrossTotal;
            discountTotal += transformed.DiscountTotal;
        });

        decimal roundedGross = RoundMoney(grossSales);
        decimal roundedDiscount = RoundMoney(discountTotal);
        decimal net = RoundMoney(roundedGross - roundedDiscount);

        return new JsonProcessingResultDto(
            transformedOrders.Count,
            totalUnits,
            roundedGross,
            roundedDiscount,
            net,
            transformedOrders);
    }

    private static JsonParserOnlyResultDto BuildParserOnlyResult(IReadOnlyList<ApiOrderDto> orders)
    {
        int orderCount = orders.Count;
        int totalLineItems = 0;
        int totalUnits = 0;
        decimal grossSales = 0m;
        decimal discountTotal = 0m;

        foreach (ApiOrderDto order in orders)
        {
            totalLineItems += order.LineItems.Count;
            foreach (ApiOrderLineItemDto line in order.LineItems)
            {
                totalUnits += line.Quantity;
                decimal gross = line.UnitPrice * line.Quantity;
                decimal discount = gross * line.DiscountPercent;
                grossSales += gross;
                discountTotal += discount;
            }
        }

        decimal roundedGross = RoundMoney(grossSales);
        decimal roundedDiscount = RoundMoney(discountTotal);
        decimal net = RoundMoney(roundedGross - roundedDiscount);

        return new JsonParserOnlyResultDto(orderCount, totalLineItems, totalUnits, roundedGross, roundedDiscount, net);
    }

    private static JsonProcessingResultDto Transform(IReadOnlyList<ApiOrderDto> orders)
    {
        List<JsonOrderResponseDto> responseOrders = new(orders.Count);
        int totalUnits = 0;
        decimal grossSales = 0m;
        decimal discountTotal = 0m;

        foreach (ApiOrderDto order in orders)
        {
            JsonOrderResponseDto responseOrder = TransformOrder(order);
            responseOrders.Add(responseOrder);
            totalUnits += responseOrder.TotalUnits;
            grossSales += responseOrder.GrossTotal;
            discountTotal += responseOrder.DiscountTotal;
        }

        decimal roundedGross = RoundMoney(grossSales);
        decimal roundedDiscount = RoundMoney(discountTotal);
        decimal net = RoundMoney(roundedGross - roundedDiscount);

        return new JsonProcessingResultDto(
            responseOrders.Count,
            totalUnits,
            roundedGross,
            roundedDiscount,
            net,
            responseOrders);
    }

    private static JsonOrderResponseDto TransformOrder(ApiOrderDto order)
    {
        int totalUnits = 0;
        decimal grossTotal = 0m;
        decimal discountTotal = 0m;

        HashSet<int> distinctProducts = [];
        foreach (ApiOrderLineItemDto line in order.LineItems)
        {
            totalUnits += line.Quantity;
            distinctProducts.Add(line.ProductId);
            decimal gross = line.UnitPrice * line.Quantity;
            decimal discount = gross * line.DiscountPercent;
            grossTotal += gross;
            discountTotal += discount;
        }

        decimal roundedGross = RoundMoney(grossTotal);
        decimal roundedDiscount = RoundMoney(discountTotal);
        decimal netTotal = RoundMoney(roundedGross - roundedDiscount);

        return new JsonOrderResponseDto(
            order.OrderId,
            order.Customer.CustomerId,
            order.Customer.Name,
            order.Status,
            DateTime.SpecifyKind(order.OrderDateUtc, DateTimeKind.Utc),
            totalUnits,
            roundedGross,
            roundedDiscount,
            netTotal,
            distinctProducts.Count);
    }

    private static void ForEachStreamedOrder(byte[] payloadUtf8, Action<ApiOrderDto> onOrder)
    {
        Utf8JsonReader reader = new(payloadUtf8, isFinalBlock: true, state: default);

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            if (!reader.ValueTextEquals("orders"u8))
            {
                continue;
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                return;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return;
                }

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    continue;
                }

                using JsonDocument orderDocument = JsonDocument.ParseValue(ref reader);
                byte[] orderBytes = Encoding.UTF8.GetBytes(orderDocument.RootElement.GetRawText());
                ApiOrderDto? order = System.Text.Json.JsonSerializer.Deserialize(orderBytes, JsonSerializationContexts.Default.ApiOrderDto);
                if (order is not null)
                {
                    onOrder(order);
                }
            }
        }
    }

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
