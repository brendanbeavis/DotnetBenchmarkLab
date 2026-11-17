using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Tests;

public class BenchmarkConventionsTests
{
    [Theory]
    [InlineData(DatasetSize.Small, 1_000)]
    [InlineData(DatasetSize.Medium, 10_000)]
    [InlineData(DatasetSize.Large, 100_000)]
    public void GetApproximateRecordCount_MapsExpectedValues(DatasetSize datasetSize, int expected)
    {
        int result = BenchmarkConventions.GetApproximateRecordCount(datasetSize);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToParameterValue_ReturnsEnumName()
    {
        string parameterValue = BenchmarkConventions.ToParameterValue(DatasetSize.Medium);

        Assert.Equal("Medium", parameterValue);
    }
}
