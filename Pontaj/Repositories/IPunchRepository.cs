using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public interface IPunchRepository
{
    /// <summary>
    /// Atomically reads the employee's most recent punch on <paramref name="punch"/>.InsertedDate
    /// and inserts <paramref name="punch"/> with <c>InOut</c> set to: <c>true</c> if no prior
    /// punch today, otherwise the inverse of the last one. The read+write pair is serialized
    /// per-employee via a SQL Server application lock, so concurrent scans for the same employee
    /// can never produce duplicate directions. The caller must set EmployeeId, InsertedDate,
    /// InsertedTime, InsertedMoment, and Badge before calling. InOut is overwritten.
    /// </summary>
    Task InsertWithDirectionInferenceAsync(Punches punch, CancellationToken ct = default);
}
