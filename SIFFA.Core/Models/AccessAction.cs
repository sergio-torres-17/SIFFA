using System;
using System.Collections.Generic;

namespace SIFFA.Core.Models;

public partial class AccessAction
{
    public Guid AccessActionId { get; set; }

    public string AccessActionName { get; set; } = null!;

    public string? AccessActionDescription { get; set; }

    public DateTime InsertionDate { get; set; }

    public virtual ICollection<Trace> Traces { get; set; } = new List<Trace>();
}
