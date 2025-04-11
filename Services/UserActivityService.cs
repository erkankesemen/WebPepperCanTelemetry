using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using WebPepperCan.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace WebPepperCan.Services
{
    public class UserActivityService : IUserActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserActivityService> _logger;

        public UserActivityService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<UserActivityService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogActivity(string userId, string activityType, string description, string ipAddress = null, string userAgent = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var activity = new UserActivity
                {
                    UserId = userId,
                    ActivityType = activityType,
                    Description = description,
                    IpAddress = ipAddress ?? httpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = userAgent ?? httpContext?.Request?.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktivite kaydı oluşturulurken hata: {Message}", ex.Message);
            }
        }

        public async Task<(IEnumerable<UserActivity> Activities, int TotalCount)> GetActivitiesAsync(
            int page = 1, 
            int pageSize = 10, 
            string searchTerm = null,
            string activityType = null,
            string ipAddress = null)
        {
            var query = _context.UserActivities
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(a => 
                    a.Description.Contains(searchTerm) || 
                    a.IpAddress.Contains(searchTerm) ||
                    a.UserAgent.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(activityType))
            {
                query = query.Where(a => a.ActivityType == activityType);
            }

            if (!string.IsNullOrEmpty(ipAddress))
            {
                query = query.Where(a => a.IpAddress == ipAddress);
            }

            var totalCount = await query.CountAsync();

            var activities = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (activities, totalCount);
        }

        public async Task<IEnumerable<UserActivity>> GetActivitiesByIpAddress(string ipAddress, int count = 10)
        {
            return await _context.UserActivities
                .Where(a => a.IpAddress == ipAddress)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserActivity>> GetUserActivities(string userId, int count = 10)
        {
            return await _context.UserActivities
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserActivity>> GetRecentActivities(int count = 10)
        {
            return await _context.UserActivities
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserActivity>> GetSuspiciousActivities(int threshold = 5)
        {
            var timeWindow = DateTime.UtcNow.AddHours(-1);

            var suspiciousIPs = await _context.UserActivities
                .Where(a => a.CreatedAt >= timeWindow && a.ActivityType == "LoginFailed")
                .GroupBy(a => a.IpAddress)
                .Where(g => g.Count() >= threshold)
                .Select(g => g.Key)
                .ToListAsync();

            return await _context.UserActivities
                .Where(a => suspiciousIPs.Contains(a.IpAddress))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}
