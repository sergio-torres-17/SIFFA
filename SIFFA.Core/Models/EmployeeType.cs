using System;
using System.Collections.Generic;

namespace SIFFA.Core.Models;

public partial class EmployeeType
{
    public Guid EmployeeTypeId { get; set; }

    public string EmployeeTypeName { get; set; } = null!;

    public string? EmployeeTypeDescription { get; set; }

    public DateTime InsertionDate { get; set; }
}
