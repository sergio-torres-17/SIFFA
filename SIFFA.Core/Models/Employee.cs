using System;
using System.Collections.Generic;

namespace SIFFA.Core.Models;

public partial class Employee
{
    public Guid EmployeeId { get; set; }

    public Guid EmployeeTypeId { get; set; }

    public string InernalIdentifier { get; set; } = null!;

    public Guid AreaId { get; set; }

    public string FirstName { get; set; } = null!;

    public string? LastName { get; set; }

    public string EmployeeEmail { get; set; } = null!;

    public DateTime InsertionDate { get; set; }

    public virtual ICollection<AddEmployee> AddEmployees { get; set; } = new List<AddEmployee>();

    public virtual Area Area { get; set; } = null!;

    public virtual EmployeeType EmployeeType { get; set; } = null!;

    public virtual ICollection<Trace> Traces { get; set; } = new List<Trace>();
}
