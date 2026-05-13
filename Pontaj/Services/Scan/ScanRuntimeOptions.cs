namespace Pontaj.Services.Scan;

public class ScanRuntimeOptions
{
    public int LockTimeoutMs { get; }

    public ScanRuntimeOptions(int lockTimeoutMs)
    {
        if (lockTimeoutMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lockTimeoutMs), "Scan lock timeout must be positive.");
        }
        LockTimeoutMs = lockTimeoutMs;
    }
}
