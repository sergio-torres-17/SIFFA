using System;
using System.Collections.Generic;

namespace SIFFA.WebApp.Data.Models;

public partial class Trace
{
    public int TraceId { get; set; }

    public int AccessActionId { get; set; }

    public Guid EmployeeId { get; set; }

    public DateTime TraceDate { get; set; }

    public string TraceDescription { get; set; } = null!;

    public DateTime InsertionDate { get; set; }

    public virtual AccessAction AccessAction { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;
}
