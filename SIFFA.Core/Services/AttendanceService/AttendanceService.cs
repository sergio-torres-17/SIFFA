using Microsoft.EntityFrameworkCore;
using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom.Response;

namespace SIFFA.Core.Services.AttendanceService
{
    public class AttendanceService : IAttendanceService
    {
        public AttendanceService(SiffaContext db)
        {
            Db = db;
        }

        private SiffaContext Db { get; set; }

        public async Task<AttendanceReport> GetReportAsync(AttendanceStatus? status = null)
        {
            var employees = await Db.Employees.AsNoTracking()
                .Select(e => new
                {
                    e.EmployeeId,
                    e.InernalIdentifier,
                    e.FirstName,
                    e.LastName,
                    e.EmployeeEmail,
                    Area = e.Area.Description,
                    Type = e.EmployeeType.EmployeeTypeName,
                })
                .ToListAsync();

            var traces = await Db.Traces.AsNoTracking()
                .Select(t => new { t.EmployeeId, t.TraceDate, Action = t.AccessAction.AccessActionName })
                .ToListAsync();

            // Mismo criterio que el dashboard: el catálogo se reconoce por nombre y cuenta el último movimiento.
            var byEmployee = traces
                .Select(t => new
                {
                    t.EmployeeId,
                    t.TraceDate,
                    IsEntry = SIFFA.Core.Services.DashboardService.DashboardService.IsEntryName(t.Action),
                    IsExit = SIFFA.Core.Services.DashboardService.DashboardService.IsExitName(t.Action),
                })
                .Where(t => t.IsEntry || t.IsExit)
                .GroupBy(t => t.EmployeeId)
                .ToDictionary(g => g.Key, g => new
                {
                    Last = g.OrderByDescending(t => t.TraceDate).First(),
                    FirstEntry = g.Where(t => t.IsEntry).Select(t => (DateTime?)t.TraceDate).Min(),
                });

            var rows = employees
                .Select(e =>
                {
                    byEmployee.TryGetValue(e.EmployeeId, out var info);
                    var state = info is null ? AttendanceStatus.NotEntered
                        : info.Last.IsExit ? AttendanceStatus.Exited
                        : AttendanceStatus.Inside;
                    return new AttendanceRow
                    {
                        EmployeeNumber = e.InernalIdentifier,
                        FullName = $"{e.FirstName} {e.LastName}".Trim(),
                        Email = e.EmployeeEmail,
                        Area = e.Area,
                        EmployeeType = e.Type,
                        Status = state,
                        StatusText = Text(state),
                        FirstEntry = info?.FirstEntry,
                        LastMovement = info?.Last.TraceDate,
                    };
                })
                .OrderBy(r => r.FullName)
                .ToList();

            return new AttendanceReport
            {
                GeneratedAt = DateTime.Now,
                Total = rows.Count,
                Inside = rows.Count(r => r.Status == AttendanceStatus.Inside),
                NotEntered = rows.Count(r => r.Status == AttendanceStatus.NotEntered),
                Exited = rows.Count(r => r.Status == AttendanceStatus.Exited),
                Employees = status is null ? rows : rows.Where(r => r.Status == status).ToList(),
            };
        }

        public static string Text(AttendanceStatus status) => status switch
        {
            AttendanceStatus.Inside => "Dentro",
            AttendanceStatus.Exited => "Ya salió",
            _ => "No ha entrado",
        };
    }
}
