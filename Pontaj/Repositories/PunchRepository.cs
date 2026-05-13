using System.Data;
using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;
using Pontaj.Services.Scan;

namespace Pontaj.Repositories;

public class PunchRepository : IPunchRepository
{
    private readonly PontajContext _context;
    private readonly ScanRuntimeOptions _scanOptions;

    public PunchRepository(PontajContext context, ScanRuntimeOptions scanOptions)
    {
        _context = context;
        _scanOptions = scanOptions;
    }

    public async Task InsertWithDirectionInferenceAsync(Punches punch, CancellationToken ct = default)
    {
        // Per-employee named application lock. Two concurrent scans for the same employee
        // serialize through this lock; scans for different employees use different lock
        // resources and don't contend with each other at all.
        var lockKey = $"ScanLock:Emp:{punch.EmployeeId}";
        var lockTimeoutMs = _scanOptions.LockTimeoutMs;

        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        // sp_getapplock returns < 0 on timeout / failure; THROW surfaces it as SqlException.
        // LockOwner=Transaction releases the lock automatically on COMMIT/ROLLBACK.
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $@"DECLARE @ret INT;
               EXEC @ret = sp_getapplock
                   @Resource    = {lockKey},
                   @LockMode    = 'Exclusive',
                   @LockOwner   = 'Transaction',
                   @LockTimeout = {lockTimeoutMs};
               IF @ret < 0 THROW 51000, N'Could not acquire scan lock for employee.', 1;",
            ct);

        var last = await _context.Punches
            .Where(p => p.EmployeeId == punch.EmployeeId && p.InsertedDate == punch.InsertedDate)
            .OrderByDescending(p => p.InsertedMoment)
            .FirstOrDefaultAsync(ct);

        punch.InOut = last == null ? true : !last.InOut;

        await _context.Punches.AddAsync(punch, ct);
        await _context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
