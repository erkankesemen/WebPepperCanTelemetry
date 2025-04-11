using System;
using System.Threading.Tasks;
using WebPepperCan.Data;
using Microsoft.EntityFrameworkCore;

namespace WebPepperCan.Services
{
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;

        public ActivityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActivity(string userId, string action, string description, string ipAddress, string userAgent)
        {
            var activity = new UserActivity
            {
                UserId = userId,
                ActivityType = action,
                Description = description,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            await _context.UserActivities.AddAsync(activity);
            await _context.SaveChangesAsync();
        }
    }
} 