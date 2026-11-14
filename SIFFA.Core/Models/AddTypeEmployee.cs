using System;
using System.Collections.Generic;

namespace SIFFA.Core.Models;

public partial class AddTypeEmployee
{
    public Guid AddTypeEmployeeId { get; set; }

    public Guid AddTypeId { get; set; }

    public Guid EmployeeId { get; set; }

    public DateTime InsertionDate { get; set; }

    public virtual AddType AddType { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;
}
