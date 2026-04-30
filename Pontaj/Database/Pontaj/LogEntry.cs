using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class LogEntry
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string StackTrace { get; set; } = null!;

    public DateTime LoggedAt { get; set; }
}
