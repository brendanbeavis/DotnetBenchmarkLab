using DotNetPerformanceLab.Reporting.Database;

namespace DotNetPerformanceLab.Data.Database;

public sealed record DatabaseCorrectnessVerificationResult(
    IReadOnlyDictionary<DatabaseReportingApproach, IReadOnlyList<DatabaseOrderReportDto>> ResultsByApproach,
    decimal DecimalTolerance);
