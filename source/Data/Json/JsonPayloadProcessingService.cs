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
        using MemoryStream payloadStream = CreateReadOnlyPayloadStream(payloadUtf8);
        return ParseOnlySystemTextJsonStreaming(payloadStream);
    }

    public JsonParserOnlyResultDto ParseOnlySystemTextJsonStreaming(Stream payloadStream, int readBufferSize = 16 * 1024)
    {
        int orderCount = 0;
        int totalLineItems = 0;
        int totalUnits = 0;
        decimal grossSales = 0m;
        decimal discountTotal = 0m;

        ForEachStreamedOrder(payloadStream, readBufferSize, order =>
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
        using MemoryStream payloadStream = CreateReadOnlyPayloadStream(payloadUtf8);
        return ProcessSystemTextJsonStreaming(payloadStream);
    }

    public JsonProcessingResultDto ProcessSystemTextJsonStreaming(Stream payloadStream, int readBufferSize = 16 * 1024)
    {
        List<JsonOrderResponseDto> transformedOrders = [];
        int totalUnits = 0;
        decimal grossSales = 0m;
        decimal discountTotal = 0m;

        ForEachStreamedOrder(payloadStream, readBufferSize, order =>
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

    private static MemoryStream CreateReadOnlyPayloadStream(byte[] payloadUtf8)
    {
        return new MemoryStream(payloadUtf8, writable: false);
    }

    private static void ForEachStreamedOrder(Stream payloadStream, int readBufferSize, Action<ApiOrderDto> onOrder)
    {
        ArgumentNullException.ThrowIfNull(payloadStream);
        ArgumentNullException.ThrowIfNull(onOrder);

        if (!payloadStream.CanRead)
        {
            throw new ArgumentException("The payload stream must be readable.", nameof(payloadStream));
        }

        if (readBufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(readBufferSize), readBufferSize, "Read buffer size must be greater than zero.");
        }

        byte[] readBuffer = new byte[readBufferSize];
        int bytesInBuffer = 0;
        JsonReaderState readerState = default;
        bool ordersPropertyFound = false;
        bool insideOrdersArray = false;
        bool ordersArrayCompleted = false;
        bool sawAnyToken = false;

        while (true)
        {
            int readCount = payloadStream.Read(readBuffer.AsSpan(bytesInBuffer));
            bool isFinalBlock = readCount == 0;
            bytesInBuffer += readCount;

            Utf8JsonReader reader = new(readBuffer.AsSpan(0, bytesInBuffer), isFinalBlock, readerState);
            int? rewindConsumedTo = null;
            bool hasRewindState = false;
            JsonReaderState rewindReaderState = default;

            while (true)
            {
                JsonReaderState stateBeforeRead = reader.CurrentState;
                long bytesConsumedBeforeRead = reader.BytesConsumed;
                if (!reader.Read())
                {
                    break;
                }

                sawAnyToken = true;

                if (!ordersPropertyFound)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("orders"u8))
                    {
                        ordersPropertyFound = true;
                    }

                    continue;
                }

                if (!insideOrdersArray)
                {
                    if (ordersArrayCompleted)
                    {
                        continue;
                    }

                    if (reader.TokenType != JsonTokenType.StartArray)
                    {
                        throw new System.Text.Json.JsonException("The 'orders' property value must be a JSON array.");
                    }

                    insideOrdersArray = true;
                    continue;
                }

                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    insideOrdersArray = false;
                    ordersArrayCompleted = true;
                    continue;
                }

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    continue;
                }

                Utf8JsonReader orderReader = reader;
                ApiOrderDto? order;
                try
                {
                    order = System.Text.Json.JsonSerializer.Deserialize(ref orderReader, JsonSerializationContexts.Default.ApiOrderDto);
                }
                catch (System.Text.Json.JsonException) when (!isFinalBlock)
                {
                    rewindConsumedTo = (int)bytesConsumedBeforeRead;
                    rewindReaderState = stateBeforeRead;
                    hasRewindState = true;
                    break;
                }

                reader = orderReader;
                if (order is not null)
                {
                    onOrder(order);
                }
            }

            int consumed = rewindConsumedTo ?? (int)reader.BytesConsumed;
            int remaining = bytesInBuffer - consumed;
            if (remaining > 0)
            {
                Buffer.BlockCopy(readBuffer, consumed, readBuffer, 0, remaining);
            }

            bytesInBuffer = remaining;
            readerState = hasRewindState ? rewindReaderState : reader.CurrentState;

            if (isFinalBlock)
            {
                if (!sawAnyToken)
                {
                    throw new System.Text.Json.JsonException("Empty JSON payload.");
                }

                if (bytesInBuffer > 0)
                {
                    throw new System.Text.Json.JsonException("Incomplete JSON payload.");
                }

                return;
            }

            if (bytesInBuffer == readBuffer.Length)
            {
                int nextSize = checked(readBuffer.Length * 2);
                Array.Resize(ref readBuffer, nextSize);
            }
        }
    }

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
