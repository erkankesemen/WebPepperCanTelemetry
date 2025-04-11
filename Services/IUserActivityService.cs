using WebPepperCan.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebPepperCan.Services
{
    public interface IUserActivityService
    {
        Task LogActivity(string userId, string activityType, string description, string ipAddress = null, string userAgent = null);
        Task<IEnumerable<UserActivity>> GetRecentActivities(int count = 10);
        Task<IEnumerable<UserActivity>> GetUserActivities(string userId, int count = 10);
        Task<(IEnumerable<UserActivity> Activities, int TotalCount)> GetActivitiesAsync(
            int page = 1,
            int pageSize = 10,
            string searchTerm = null,
            string activityType = null,
            string ipAddress = null);
        Task<IEnumerable<UserActivity>> GetSuspiciousActivities(int threshold = 5);
        Task<IEnumerable<UserActivity>> GetActivitiesByIpAddress(string ipAddress, int count = 10);
    }
} 