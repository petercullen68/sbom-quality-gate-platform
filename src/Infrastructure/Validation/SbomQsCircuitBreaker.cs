namespace SbomQualityGate.Infrastructure.Validation;

/// <summary>
/// Thread-safe circuit breaker for the sbomqs process.
/// Registered as Singleton so state is shared across all scoped IValidationTool instances.
/// Opens after <see cref="FailureThreshold"/> consecutive failures and stays open
/// for <see cref="BlockDuration"/> before allowing another attempt.
/// </summary>
public sealed class SbomQsCircuitBreaker
{
    public static readonly int FailureThreshold = 5;
    public static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(1);

    private readonly Lock _lock = new();
    private int _failureCount;
    private DateTime _blockedUntil = DateTime.MinValue;

    /// <summary>
    /// Returns true when the circuit is open and calls should be rejected.
    /// </summary>
    public bool IsOpen => DateTime.UtcNow < _blockedUntil;

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;

            if (_failureCount >= FailureThreshold)
            {
                _blockedUntil = DateTime.UtcNow.Add(BlockDuration);
                _failureCount = 0;
            }
        }
    }
}
