using SIFFA.Core.Models.Custom;
using SIFFA.Core.Models.Custom.Request.Employeees;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIFFA.Core.Services.EmployeeService
{
    public interface IEmployeeService
    {
        public GenericReponse AddSeveralEmployees(List<InsertEmployeeRequest> employees);
    }
}
