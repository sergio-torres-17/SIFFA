using System;
using System.Collections.Generic;

namespace SIFFA.WebApp.Data.Models;

public partial class AddTypeEmployee
{
    public int AddTypeEmployeeId { get; set; }

    public int AddTypeId { get; set; }

    public Guid EmployeeId { get; set; }

    public DateTime InsertionDate { get; set; }

    public virtual AddType AddType { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;
}
