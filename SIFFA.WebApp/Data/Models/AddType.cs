using System;
using System.Collections.Generic;

namespace SIFFA.WebApp.Data.Models;

public partial class AddType
{
    public int AddTypeId { get; set; }

    public string AddTypeName { get; set; } = null!;

    public string? AddTypeDescription { get; set; }

    public DateTime InsertionDate { get; set; }

    public virtual ICollection<AddEmployee> AddEmployees { get; set; } = new List<AddEmployee>();

    public virtual ICollection<AddTypeEmployee> AddTypeEmployees { get; set; } = new List<AddTypeEmployee>();
}
