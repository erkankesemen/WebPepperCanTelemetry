using WebPepperCan.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebPepperCan.Services
{
    public interface IUserSessionService
    {
        Task<UserSession> CreateSessionAsync(string userId, string connectionId, string ipAddress, string userAgent);
        Task UpdateSessionActivityAsync(string connectionId);
        Task EndSessionAsync(string connectionId);
        Task<int> GetActiveSessionCountAsync(int minutesThreshold = 30);
        Task<IEnumerable<UserSession>> GetActiveSessionsAsync(int minutesThreshold = 30);
        Task CleanupInactiveSessions(int minutesThreshold = 30);
    }
} 