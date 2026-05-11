using System;
using System.Collections.Generic;

namespace Pontaj.Database.Pontaj;

public partial class TextResources
{
    public int Id { get; set; }

    public string ResourceKey { get; set; } = null!;

    public string Value { get; set; } = null!;

    public int LanguageId { get; set; }

    public virtual Languages Language { get; set; } = null!;
}
