using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom;
using SIFFA.Core.Models.Custom.Request.Employeees;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIFFA.Core.Services.EmployeeService
{
    public class EmployeeService : IEmployeeService
    {
        public EmployeeService(SiffaContext db)
        {
            Db = db;
        }

        private SiffaContext Db { get; set; }
        public GenericReponse AddSeveralEmployees(List<InsertEmployeeRequest> employees)
        {
            List<Employee>? employeesNew;
            Area? area;
            List<Area> areas;



            employeesNew = new List<Employee>();
            areas =  new List<Area>();
            try
            {
                ////Distinct all areas
                //foreach (var item in employees)
                //{
                //    if (item.AreaName != null &&
                //        areas.FirstOrDefault(a => a.Description == item.AreaName) == null)
                //    {
                //        area = new Area();
                //        area.Description = item.AreaName;
                //        areas.Add(area);
                //    }
                //}

                //Distinct All employees
                foreach (var item in employees)
                {
                    if (item.InternalIdentificer != null &&
                        employeesNew.FirstOrDefault(e => e.InernalIdentifier == item.InternalIdentificer) == null)
                    {
                        Employee employee = new Employee
                        {
                            InernalIdentifier = item.InternalIdentificer,
                            FirstName = item.FirstName,
                            LastName = item.LastName,
                            EmployeeEmail = item.EmployeeEmail,
                            Area = area = new Area() { Description = item.AreaName }
                        };
                        employeesNew.Add(employee);
                    }
                }

                //Insert all employees
                Db.Employees.AddRange(employeesNew);
                Db.SaveChanges();   

            }
            catch (Exception ex)
            {
                return new GenericReponse(false, $"Error al agregar los empleados: {ex.Message}");
            }
            return new GenericReponse(true, "Empleados agregados correctamente");
        }
    }
}
