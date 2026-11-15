using SIFFA.Core.Models.Custom.Response;

namespace SIFFA.Core.Services.DashboardService
{
    public interface IDashboardService
    {
        Task<DashboardSummary> GetSummaryAsync(int latestCount = 10);
    }
}
