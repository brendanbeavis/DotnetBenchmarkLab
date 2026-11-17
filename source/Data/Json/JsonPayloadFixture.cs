using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Data.Json;

public sealed record JsonPayloadFixture(
    DatasetSize DatasetSize,
    byte[] PayloadUtf8,
    string PayloadText);
