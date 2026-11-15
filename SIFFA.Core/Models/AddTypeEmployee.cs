using System;
using System.Collections.Generic;

namespace SIFFA.Core.Models;

public partial class AddTypeEmployee
{
    public Guid AddTypeEmployeeId { get; set; }

    public Guid AddTypeId { get; set; }

    public Guid? EmployeeTypeId { get; set; }

    public Guid? AreaId { get; set; }

    public int Quantity { get; set; }

    public DateTime InsertionDate { get; set; }

    public virtual AddType AddType { get; set; } = null!;

    public virtual Area? Area { get; set; }

    public virtual EmployeeType? EmployeeType { get; set; }
}
