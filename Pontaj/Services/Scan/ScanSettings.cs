namespace Pontaj.Services.Scan;

public static class ScanSettings
{
    public const string LockTimeoutMsConfigName = "Scan:LockTimeoutMs";

    // Default if the Configurations row is missing/unparseable or the DB read fails at startup.
    // 5s is comfortably above the realistic critical-section time (~20ms read+insert),
    // but short enough that genuine contention surfaces as an error instead of hanging forever.
    public const int DefaultLockTimeoutMs = 5000;
}
