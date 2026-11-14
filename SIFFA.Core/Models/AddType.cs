using System;
using System.Collections.Generic;

namespace SIFFA.Core.Models;

public partial class AddType
{
    public Guid AddTypeId { get; set; }

    public string AddTypeName { get; set; } = null!;

    public string? AddTypeDescription { get; set; }

    public DateTime InsertionDate { get; set; }

    public virtual ICollection<AddEmployee> AddEmployees { get; set; } = new List<AddEmployee>();

    public virtual ICollection<AddTypeEmployee> AddTypeEmployees { get; set; } = new List<AddTypeEmployee>();
}
