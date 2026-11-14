using System;
using System.Collections.Generic;

namespace SIFFA.Core.Models;

public partial class Area
{
    public Guid AreaId { get; set; }

    public string Description { get; set; } = null!;

    public DateTime InsertionDate { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
