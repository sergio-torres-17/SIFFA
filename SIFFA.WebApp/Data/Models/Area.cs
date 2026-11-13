using System;
using System.Collections.Generic;

namespace SIFFA.WebApp.Data.Models;

public partial class Area
{
    public int AreaId { get; set; }

    public string Description { get; set; } = null!;

    public DateTime InsertionDate { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
