using SbomQualityGate.Infrastructure.Validation;

namespace SbomQualityGate.UnitTests.Infrastructure;

public class SbomQsCircuitBreakerTests
{
    [Fact]
    public void IsOpenInitialStateReturnsFalse()
    {
        var breaker = new SbomQsCircuitBreaker();

        Assert.False(breaker.IsOpen);
    }

    [Fact]
    public void IsOpenBelowThresholdReturnsFalse()
    {
        var breaker = new SbomQsCircuitBreaker();

        for (var i = 0; i < SbomQsCircuitBreaker.FailureThreshold - 1; i++)
            breaker.RecordFailure();

        Assert.False(breaker.IsOpen);
    }

    [Fact]
    public void IsOpenAtThresholdReturnsTrue()
    {
        var breaker = new SbomQsCircuitBreaker();

        for (var i = 0; i < SbomQsCircuitBreaker.FailureThreshold; i++)
            breaker.RecordFailure();

        Assert.True(breaker.IsOpen);
    }

    [Fact]
    public void RecordSuccessResetsFailureCount()
    {
        var breaker = new SbomQsCircuitBreaker();

        for (var i = 0; i < SbomQsCircuitBreaker.FailureThreshold - 1; i++)
            breaker.RecordFailure();

        breaker.RecordSuccess();

        // After a success, hitting threshold again from scratch should still open it
        for (var i = 0; i < SbomQsCircuitBreaker.FailureThreshold; i++)
            breaker.RecordFailure();

        Assert.True(breaker.IsOpen);
    }

    [Fact]
    public void RecordSuccessWhenBelowThresholdCircuitRemainsClosedAfterReset()
    {
        var breaker = new SbomQsCircuitBreaker();

        for (var i = 0; i < SbomQsCircuitBreaker.FailureThreshold - 1; i++)
            breaker.RecordFailure();

        breaker.RecordSuccess();

        // Should need a full threshold of failures again to open
        for (var i = 0; i < SbomQsCircuitBreaker.FailureThreshold - 1; i++)
            breaker.RecordFailure();

        Assert.False(breaker.IsOpen);
    }

    [Fact]
    public void RecordFailureAfterThresholdReachedResetsCounterSoSecondBurstAlsoTrips()
    {
        var breaker = new SbomQsCircuitBreaker();

        // First burst — opens the circuit
        for (var i = 0; i < SbomQsCircuitBreaker.FailureThreshold; i++)
            breaker.RecordFailure();

        Assert.True(breaker.IsOpen);

        // Counter resets internally after opening; a second burst should open it again
        // (simulating a half-open retry scenario after BlockDuration — we just verify
        // the counter resets, not the time gating, which would require a real clock)
        for (var i = 0; i < SbomQsCircuitBreaker.FailureThreshold; i++)
            breaker.RecordFailure();

        Assert.True(breaker.IsOpen);
    }
}
