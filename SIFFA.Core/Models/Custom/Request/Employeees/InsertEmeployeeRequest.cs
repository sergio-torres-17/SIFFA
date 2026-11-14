using System;
using System.Collections.Generic;
using System.Text;

namespace SIFFA.Core.Models.Custom.Request.Employeees
{
    public class InsertEmployeeRequest
    {
        public string? InternalIdentificer { get; set; }
        public string? AreaName { get; set; }
        public string? FirstName  { get; set; }
        public string? LastName { get; set; }
        public string? EmployeeEmail { get; set; }

    }
}
