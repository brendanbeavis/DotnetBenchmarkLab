using DotNetPerformanceLab.Reporting.Json;

namespace DotNetPerformanceLab.Data.Json;

public sealed record JsonCorrectnessVerificationResult(
    IReadOnlyDictionary<JsonProcessingApproach, JsonParserOnlyResultDto> ParserOnlyResults,
    IReadOnlyDictionary<JsonProcessingApproach, JsonProcessingResultDto> EndToEndResults,
    decimal DecimalTolerance);
