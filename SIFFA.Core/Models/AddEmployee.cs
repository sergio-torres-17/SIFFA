using System;
using System.Collections.Generic;

namespace SIFFA.Core.Models;

public partial class AddEmployee
{
    public Guid AddEmployeeId { get; set; }

    public Guid AddTypeId { get; set; }

    public Guid EmployeeId { get; set; }

    public bool IsDeliveryAdd { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public DateTime InsertionDate { get; set; }

    public virtual AddType AddType { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;
}
