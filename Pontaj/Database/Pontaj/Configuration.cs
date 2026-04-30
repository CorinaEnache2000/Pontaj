using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class Configuration
{
    public int Id { get; set; }

    public string ConfigKey { get; set; } = null!;

    public string ConfigValue { get; set; } = null!;

    public string? Description { get; set; }
}
