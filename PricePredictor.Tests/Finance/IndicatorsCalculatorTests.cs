using Shouldly;
using PricePredictor.Api.Finance;
using Xunit;

namespace PricePredictor.Tests.Finance;

public class IndicatorsCalculatorTests
{
    #region Logarithmic Return Tests

    [Fact]
    public void CalculateLogarithmicReturn_WithValidPrices_ReturnsCorrectValue()
    {
        // Arrange
        decimal currentPrice = 100m;
        decimal previousPrice = 99m;

        // Act
        var result = IndicatorsCalculator.CalculateLogarithmicReturn(currentPrice, previousPrice);

        // Assert
        result.ShouldBeGreaterThan(0);
        result.ShouldBe(0.010050335, 0.0000001);
    }

    [Fact]
    public void CalculateLogarithmicReturn_WithZeroPreviousPrice_ReturnsZero()
    {
        // Arrange
        decimal currentPrice = 100m;
        decimal previousPrice = 0m;

        // Act
        var result = IndicatorsCalculator.CalculateLogarithmicReturn(currentPrice, previousPrice);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void CalculateLogarithmicReturn_WithPriceDecrease_ReturnsNegative()
    {
        // Arrange
        decimal currentPrice = 99m;
        decimal previousPrice = 100m;

        // Act
        var result = IndicatorsCalculator.CalculateLogarithmicReturn(currentPrice, previousPrice);

        // Assert
        result.ShouldBeLessThan(0);
    }

    [Fact]
    public void CalculateLogarithmicReturn_WithSamePrices_ReturnsZero()
    {
        // Arrange
        decimal currentPrice = 100m;
        decimal previousPrice = 100m;

        // Act
        var result = IndicatorsCalculator.CalculateLogarithmicReturn(currentPrice, previousPrice);

        // Assert
        result.ShouldBe(0, 0.0000001);
    }

    #endregion

    #region Rolling Volatility Tests

    [Fact]
    public void CalculateRollingVolatility_WithValidReturns_ReturnsPositiveValue()
    {
        // Arrange
        var returns = new[] { 0.01, -0.005, 0.002, 0.008, -0.003 };

        // Act
        var result = IndicatorsCalculator.CalculateRollingVolatility(returns);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CalculateRollingVolatility_WithConstantReturns_ReturnsZero()
    {
        // Arrange
        var returns = new[] { 0.01, 0.01, 0.01, 0.01, 0.01 };

        // Act
        var result = IndicatorsCalculator.CalculateRollingVolatility(returns);

        // Assert
        result.ShouldBe(0, 0.0000001);
    }

    [Fact]
    public void CalculateRollingVolatility_WithSingleReturn_ReturnsZero()
    {
        // Arrange
        var returns = new[] { 0.01 };

        // Act
        var result = IndicatorsCalculator.CalculateRollingVolatility(returns);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void CalculateRollingVolatility_WithEmptyArray_ReturnsZero()
    {
        // Arrange
        var returns = Array.Empty<double>();

        // Act
        var result = IndicatorsCalculator.CalculateRollingVolatility(returns);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void CalculateRollingVolatility_WithHighVolatilityReturns_ReturnsHighValue()
    {
        // Arrange
        var returns = new[] { 0.05, -0.06, 0.04, -0.05, 0.06 }; // High volatility

        // Act
        var result = IndicatorsCalculator.CalculateRollingVolatility(returns);

        // Assert
        result.ShouldBeGreaterThan(0.04);
    }

    #endregion

    #region Standard Deviation Tests

    [Fact]
    public void CalculateStdDev_WithValidValues_ReturnsCorrectValue()
    {
        // Arrange
        var values = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var expectedMean = 3.0;
        var expectedVariance = 2.0; // ((1-3)^2 + (2-3)^2 + ... ) / 5
        var expectedStdDev = Math.Sqrt(expectedVariance);

        // Act
        var result = IndicatorsCalculator.CalculateStdDev(values);

        // Assert
        result.ShouldBe(expectedStdDev, 0.0001);
    }

    [Fact]
    public void CalculateStdDev_WithConstantValues_ReturnsZero()
    {
        // Arrange
        var values = new[] { 5.0, 5.0, 5.0, 5.0, 5.0 };

        // Act
        var result = IndicatorsCalculator.CalculateStdDev(values);

        // Assert
        result.ShouldBe(0, 0.0000001);
    }

    [Fact]
    public void CalculateStdDev_WithEmptyArray_ReturnsZero()
    {
        // Arrange
        var values = Array.Empty<double>();

        // Act
        var result = IndicatorsCalculator.CalculateStdDev(values);

        // Assert
        result.ShouldBe(0);
    }

    #endregion

    #region Normalized Volatility Panic Score Tests

    [Fact]
    public void CalculateNormalizedVolatilityPanicScore_WithStableMarket_ReturnsLowScore()
    {
        // Arrange - stable market: small return, short-term volatility similar to long-term
        var logReturn = 0.001; // Small return
        var shortVolatility = 0.02;
        var longVolatility = 0.02;
        // Expected: 0.3 * |0.001| + 0.7 * (0.02 / 0.02) = 0.0003 + 0.7 = 0.7003

        // Act
        var result = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
            logReturn, shortVolatility, longVolatility);

        // Assert - when ratio is 1 (equal volatilities), still returns 0.7 from weights
        result.ShouldBe(0.7003, 0.001);
    }

    [Fact]
    public void CalculateNormalizedVolatilityPanicScore_WithPanicMarket_ReturnsHighScore()
    {
        // Arrange - panic market: large return, short-term volatility much higher than long-term
        var logReturn = 0.05; // Large return
        var shortVolatility = 0.1;
        var longVolatility = 0.02;

        // Act
        var result = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
            logReturn, shortVolatility, longVolatility);

        // Assert
        result.ShouldBeGreaterThan(0.4); // High panic score
    }

    [Fact]
    public void CalculateNormalizedVolatilityPanicScore_WithLongZeroVolatility_PreventsDivisionByZero()
    {
        // Arrange
        var logReturn = 0.05;
        var shortVolatility = 0.1;
        var longVolatility = 0.0; // Zero volatility

        // Act
        var result = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
            logReturn, shortVolatility, longVolatility);

        // Assert - should use 1e-9 instead of 0
        result.ShouldBeGreaterThan(0);
        result.ShouldNotBe(double.PositiveInfinity);
        result.ShouldNotBe(double.NegativeInfinity);
        result.ShouldNotBe(double.NaN);
    }

    [Fact]
    public void CalculateNormalizedVolatilityPanicScore_WithNegativeReturn_UsesAbsoluteValue()
    {
        // Arrange - negative return should be treated same as positive
        var logReturnPositive = 0.03;
        var logReturnNegative = -0.03;
        var shortVolatility = 0.05;
        var longVolatility = 0.04;

        // Act
        var resultPositive = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
            logReturnPositive, shortVolatility, longVolatility);
        var resultNegative = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
            logReturnNegative, shortVolatility, longVolatility);

        // Assert
        resultPositive.ShouldBe(resultNegative, 0.0001);
    }

    [Fact]
    public void CalculateNormalizedVolatilityPanicScore_WithCustomWeights_ReturnsCorrectValue()
    {
        // Arrange
        var logReturn = 0.02;
        var shortVolatility = 0.05;
        var longVolatility = 0.03;
        var weightReturn = 0.5;
        var weightVol = 0.5;
        
        // Expected: 0.5 * |0.02| + 0.5 * (0.05 / 0.03)
        var expected = 0.5 * 0.02 + 0.5 * (0.05 / 0.03);

        // Act
        var result = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
            logReturn, shortVolatility, longVolatility, weightReturn, weightVol);

        // Assert
        result.ShouldBe(expected, 0.0001);
    }

    [Fact]
    public void CalculateNormalizedVolatilityPanicScore_Short5MinLong60Min_CalculatesCorrectly()
    {
        // Arrange - real trading scenario: 5-min short window, 60-min long window
        var logReturn = 0.008;
        var vol5min = 0.035; // Higher short-term volatility = market stress
        var vol60min = 0.025; // Lower long-term volatility

        // Act
        var shortPanicScore = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
            logReturn, vol5min, vol60min, 0.3, 0.7);

        // Assert - should detect higher stress
        shortPanicScore.ShouldBeGreaterThan(0.7);
    }

    [Fact]
    public void CalculateNormalizedVolatilityPanicScore_ScoreIncreasesWithVolatilityRatio()
    {
        // Arrange
        var logReturn = 0.01;
        var shortVolatility = 0.04;
        var longVolatility = 0.04;

        var stableScoreBase = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
            logReturn, shortVolatility, longVolatility);

        var panicScore = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
            logReturn, 0.08, longVolatility); // Double short volatility

        // Act & Assert
        panicScore.ShouldBeGreaterThan(stableScoreBase);
    }

    #endregion

    #region Composite Panic Indicator Tests

    [Fact]
    public void Return_WithExampleData_ReturnsExpectedLogReturn()
    {
        // Arrange
        const double delta = 0.001;
        double prevClose = 180.0;
        double close = 181.0;

        // Act
        var result = IndicatorsCalculator.Return(close, prevClose);

        // Assert
        result.ShouldBe(0.00554018, delta); // Standard log return
    }

    [Fact]
    public void RollingVolatility_WithExampleData_ReturnsExpectedValue()
    {
        // Arrange
        const double delta = 0.001;
        double[] last15Closes = { 180, 180.5, 179.8, 180.2, 180.7, 181, 180.9, 180.6, 180.8, 181.2, 181.5, 181.3, 181.4, 181.6, 181.7 };
        var last15Returns = last15Closes.Zip(last15Closes.Skip(1), (curr, prev) => Math.Log(curr / prev)).ToArray();

        // Act
        var result = IndicatorsCalculator.RollingVolatility(last15Returns);

        // Assert
        result.ShouldBe(0.00182818, delta); // Standard deviation (population)
    }

    [Fact]
    public void ATR_WithExampleData_ReturnsExpectedValue()
    {
        // Arrange
        const double delta = 0.001;
        double high = 182.0;
        double low = 179.5;
        double prevClose = 180.0;

        // Act
        var result = IndicatorsCalculator.ATR(high, low, prevClose);

        // Assert
        result.ShouldBe(2.5, delta);
    }

    [Fact]
    public void RSIDeviation_WithExampleData_ReturnsExpectedValue()
    {
        // Arrange
        const double delta = 0.001;
        double[] last15Closes = { 180, 180.5, 179.8, 180.2, 180.7, 181, 180.9, 180.6, 180.8, 181.2, 181.5, 181.3, 181.4, 181.6, 181.7 };

        // Act
        var result = IndicatorsCalculator.RSIDeviation(last15Closes);

        // Assert
        result.ShouldBe(19.76744, delta); // Standard RSI deviation from 50
    }

    [Fact]
    public void BollingerDeviation_WithExampleData_ReturnsExpectedValue()
    {
        // Arrange
        const double delta = 0.001;
        double close = 181.0;
        double[] last20Closes = { 180, 180.5, 179.8, 180.2, 180.7, 181, 180.9, 180.6, 180.8, 181.2, 181.5, 181.3, 181.4, 181.6, 181.7 };

        // Act
        var result = IndicatorsCalculator.BollingerDeviation(close, last20Closes);

        // Assert
        result.ShouldBe(0, delta); // Not enough points for 20-period bands
    }

    [Fact]
    public void VolumeSpike_WithExampleData_ReturnsExpectedValue()
    {
        // Arrange
        const double delta = 0.001;
        double volume = 1500;
        double avgVolume = 1000;

        // Act
        var result = IndicatorsCalculator.VolumeSpike(volume, avgVolume);

        // Assert
        result.ShouldBe(1.5, delta);
    }

    [Fact]
    public void VROC_WithExampleData_ReturnsExpectedValue()
    {
        // Arrange
        const double delta = 0.001;
        double volume = 1500;
        double pastVolume = 1200;

        // Act
        var result = IndicatorsCalculator.VROC(volume, pastVolume);

        // Assert
        result.ShouldBe(0.25, delta);
    }

    [Fact]
    public void CompositePanicScore_WithExampleData_ReturnsExpectedValue()
    {
        // Arrange
        const double delta = 0.001;
        double prevClose = 180.0;
        double close = 181.0;
        double high = 182.0;
        double low = 179.5;
        double[] last15Closes = { 180, 180.5, 179.8, 180.2, 180.7, 181, 180.9, 180.6, 180.8, 181.2, 181.5, 181.3, 181.4, 181.6, 181.7 };
        var last15Returns = last15Closes.Zip(last15Closes.Skip(1), (curr, prev) => Math.Log(curr / prev)).ToArray();
        double volume = 1500;
        double avgVolume = 1000;
        double pastVolume = 1200;

        double ret = IndicatorsCalculator.Return(close, prevClose);
        double rollingVol = IndicatorsCalculator.RollingVolatility(last15Returns);
        double atr = IndicatorsCalculator.ATR(high, low, prevClose);
        double rsiDev = IndicatorsCalculator.RSIDeviation(last15Closes);
        double bbDev = IndicatorsCalculator.BollingerDeviation(close, last15Closes);
        double volSpike = IndicatorsCalculator.VolumeSpike(volume, avgVolume);
        double vroc = IndicatorsCalculator.VROC(volume, pastVolume);

        // Act
        var result = IndicatorsCalculator.CompositePanicScore(ret, rollingVol, atr, rsiDev, bbDev, volSpike, vroc);

        // Assert
        result.ShouldBe(1.415583, delta); // Standard formula + weights
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void IntegrationTest_CalculateAllMetricsForTimeFrame()
    {
        // Arrange - simulate 5 candles with closing prices
        var closePrices = new[] { 100m, 101m, 100.5m, 102m, 101m };
        var logReturns = new List<double>();

        // Calculate returns
        for (int i = 1; i < closePrices.Length; i++)
        {
            var ret = IndicatorsCalculator.CalculateLogarithmicReturn(closePrices[i], closePrices[i - 1]);
            logReturns.Add(ret);
        }

        // Act
        var vol = IndicatorsCalculator.CalculateRollingVolatility(logReturns);
        var panicScore = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
            logReturns[^1], vol, vol, 0.3, 0.7);

        // Assert
        logReturns.Count.ShouldBe(4);
        vol.ShouldBeGreaterThan(0);
        panicScore.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void IntegrationTest_VolatilityCalculationWith5And60MinWindows()
    {
        // Arrange - simulate 60 minutes of 1-minute returns
        var random = new Random(42); // Fixed seed for deterministic test
        var allReturns = Enumerable.Range(0, 60)
            .Select(_ => (random.NextDouble() - 0.5) * 0.02) // Small random returns
            .ToList();

        // Calculate volatilities for different windows
        var vol5 = IndicatorsCalculator.CalculateRollingVolatility(allReturns.TakeLast(5));
        var vol60 = IndicatorsCalculator.CalculateRollingVolatility(allReturns.TakeLast(60));

        // Act
        var shortPanic = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
            allReturns[^1], vol5, vol60, 0.3, 0.7);
        var longPanic = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
            allReturns[^1], vol60, vol60, 0.3, 0.7);

        // Assert
        vol5.ShouldBeGreaterThanOrEqualTo(0);
        vol60.ShouldBeGreaterThanOrEqualTo(0);
        shortPanic.ShouldBeGreaterThanOrEqualTo(0);
        longPanic.ShouldBeGreaterThanOrEqualTo(0);
    }

    #endregion
}


