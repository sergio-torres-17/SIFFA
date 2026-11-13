using System;
using System.Collections.Generic;

namespace SIFFA.WebApp.Data.Models;

public partial class EmployeeType
{
    public int EmployeeTypeId { get; set; }

    public string EmployeeTypeName { get; set; } = null!;

    public string? EmployeeTypeDescription { get; set; }

    public DateTime InsertionDate { get; set; }
}
