using Microsoft.EntityFrameworkCore;
using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom.Response;

namespace SIFFA.Core.Services.DashboardService
{
    public class DashboardService : IDashboardService
    {
        public DashboardService(SiffaContext db)
        {
            Db = db;
        }

        private SiffaContext Db { get; set; }

        // El catálogo AccessAction no tiene un código fijo, así que se reconoce por el nombre:
        // Entrada / Entry / Ingreso  y  Salida / Exit / Egreso.
        private static bool IsEntryName(string name) =>
            name.Contains("entr", StringComparison.OrdinalIgnoreCase)
            || name.Contains("ingres", StringComparison.OrdinalIgnoreCase);

        private static bool IsExitName(string name) =>
            name.Contains("sal", StringComparison.OrdinalIgnoreCase)
            || name.Contains("exit", StringComparison.OrdinalIgnoreCase)
            || name.Contains("egres", StringComparison.OrdinalIgnoreCase);

        public async Task<DashboardSummary> GetSummaryAsync(int latestCount = 10)
        {
            var employees = await Db.Employees.AsNoTracking()
                .Select(e => new { e.EmployeeId, e.InernalIdentifier, e.FirstName, e.LastName, Area = e.Area.Description })
                .ToListAsync();

            var traces = await Db.Traces.AsNoTracking()
                .Select(t => new { t.EmployeeId, t.TraceDate, Action = t.AccessAction.AccessActionName })
                .ToListAsync();

            var actions = await Db.AccessActions.AsNoTracking().Select(a => a.AccessActionName).ToListAsync();

            var classified = traces
                .Select(t => new { t.EmployeeId, t.TraceDate, t.Action, IsEntry = IsEntryName(t.Action), IsExit = IsExitName(t.Action) })
                .Where(t => t.IsEntry || t.IsExit)
                .ToList();

            var enteredIds = classified.Where(t => t.IsEntry).Select(t => t.EmployeeId).ToHashSet();
            var lastByEmployee = classified
                .GroupBy(t => t.EmployeeId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.TraceDate).First());
            var exitedIds = lastByEmployee.Where(kv => kv.Value.IsExit).Select(kv => kv.Key).ToHashSet();

            var summary = new DashboardSummary
            {
                TotalEmployees = employees.Count,
                Entered = enteredIds.Count,
                Pending = employees.Count - enteredIds.Count,
                Exited = exitedIds.Count,
                Inside = enteredIds.Count - exitedIds.Count,
                HasAccessActions = actions.Any(a => IsEntryName(a) || IsExitName(a)),
            };

            summary.ByArea = employees
                .GroupBy(e => e.Area)
                .Select(g => new DashboardAreaRow
                {
                    Area = g.Key,
                    Registered = g.Count(),
                    Entered = g.Count(e => enteredIds.Contains(e.EmployeeId)),
                    Exited = g.Count(e => exitedIds.Contains(e.EmployeeId)),
                })
                .OrderByDescending(a => a.Registered)
                .ThenBy(a => a.Area)
                .ToList();

            summary.EntriesByHour = classified
                .Where(t => t.IsEntry)
                .GroupBy(t => t.TraceDate.Hour)
                .Select(g => new DashboardHourRow { Hour = g.Key, Entries = g.Count() })
                .OrderBy(h => h.Hour)
                .ToList();

            var byId = employees.ToDictionary(e => e.EmployeeId);
            summary.LatestAccesses = classified
                .OrderByDescending(t => t.TraceDate)
                .Take(latestCount)
                .Where(t => byId.ContainsKey(t.EmployeeId))
                .Select(t =>
                {
                    var e = byId[t.EmployeeId];
                    return new DashboardAccessRow
                    {
                        Date = t.TraceDate,
                        EmployeeNumber = e.InernalIdentifier,
                        EmployeeName = $"{e.FirstName} {e.LastName}".Trim(),
                        Area = e.Area,
                        Action = t.Action,
                        IsEntry = t.IsEntry,
                    };
                })
                .ToList();

            return summary;
        }
    }
}
