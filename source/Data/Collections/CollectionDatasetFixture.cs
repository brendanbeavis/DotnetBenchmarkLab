using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Models;

namespace DotNetPerformanceLab.Data.Collections;

public sealed record CollectionDatasetFixture(
    DatasetSize DatasetSize,
    IReadOnlyList<RetailTransaction> Transactions);
