using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class Punches
{
    public long Id { get; set; }

    public int? WorkStationId { get; set; }

    public int? OrganizationalUnitId { get; set; }

    public int EmployeeId { get; set; }

    public string Badge { get; set; } = null!;

    public bool InOut { get; set; }

    public DateOnly InsertedDate { get; set; }

    public TimeOnly InsertedTime { get; set; }

    public DateTime InsertedMoment { get; set; }

    public string? Ip { get; set; }

    public virtual Employees Employee { get; set; } = null!;

    public virtual OrganizationalUnits? OrganizationalUnit { get; set; }

    public virtual WorkStations? WorkStation { get; set; }
}
