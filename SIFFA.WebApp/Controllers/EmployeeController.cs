using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIFFA.Core.Models.Custom.Request.Employeees;
using SIFFA.Core.Services.EmployeeService;

namespace SIFFA.WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        public EmployeeController(IEmployeeService employeeService)
        {
            EmployeeService = employeeService;
        }

        public IEmployeeService EmployeeService { get; set; }


        public IActionResult AddSeveralEmployees(List<InsertEmployeeRequest> employees)
        {
            var response = EmployeeService.AddSeveralEmployees(employees);
            if (response.IsSuccess)
                return Ok(response);
            else
                return BadRequest(response);
        }
    }
}
