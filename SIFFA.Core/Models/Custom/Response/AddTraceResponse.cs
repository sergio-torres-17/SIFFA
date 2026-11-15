using SIFFA.Core.Models.Custom.Model;

namespace SIFFA.Core.Models.Custom.Response
{
    public record AddTraceResponse(
        bool status,
        string message,
        List<AddEmployeeModel>? employeeAdds,
        string? employeeNumber = null,
        string? employeeName = null);
}
