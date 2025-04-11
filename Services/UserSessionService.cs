using Microsoft.EntityFrameworkCore;
using WebPepperCan.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebPepperCan.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserSessionService> _logger;

        public UserSessionService(ApplicationDbContext context, ILogger<UserSessionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserSession> CreateSessionAsync(string userId, string connectionId, string ipAddress, string userAgent)
        {
            var session = new UserSession
            {
                UserId = userId,
                ConnectionId = connectionId,
                LastActivity = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsActive = true
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task UpdateSessionActivityAsync(string connectionId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.ConnectionId == connectionId);

            if (session != null)
            {
                session.LastActivity = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task EndSessionAsync(string connectionId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.ConnectionId == connectionId);

            if (session != null)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetActiveSessionCountAsync(int minutesThreshold = 30)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-minutesThreshold);
            return await _context.UserSessions
                .CountAsync(s => s.IsActive && s.LastActivity >= threshold);
        }

        public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(int minutesThreshold = 30)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-minutesThreshold);
            return await _context.UserSessions
                .Include(s => s.User)
                .Where(s => s.IsActive && s.LastActivity >= threshold)
                .OrderByDescending(s => s.LastActivity)
                .ToListAsync();
        }

        public async Task CleanupInactiveSessions(int minutesThreshold = 30)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-minutesThreshold);
            var inactiveSessions = await _context.UserSessions
                .Where(s => s.IsActive && s.LastActivity < threshold)
                .ToListAsync();

            foreach (var session in inactiveSessions)
            {
                session.IsActive = false;
            }

            await _context.SaveChangesAsync();
        }
    }
} 