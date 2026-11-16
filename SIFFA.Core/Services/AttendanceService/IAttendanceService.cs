using SIFFA.Core.Models.Custom.Response;

namespace SIFFA.Core.Services.AttendanceService
{
    public interface IAttendanceService
    {
        /// <summary>Quién está dentro, quién no ha entrado y quién ya salió. Con <paramref name="status"/> solo devuelve ese grupo en la lista (los totales siempre son del conjunto completo).</summary>
        Task<AttendanceReport> GetReportAsync(AttendanceStatus? status = null);
    }
}
