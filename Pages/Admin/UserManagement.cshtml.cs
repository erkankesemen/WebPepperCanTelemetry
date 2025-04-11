using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebPepperCan.Data;
using WebPepperCan.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebPepperCan.Pages.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    public class UserManagementModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserActivityService _activityService;

        public UserManagementModel(ApplicationDbContext context, IUserActivityService activityService)
        {
            _context = context;
            _activityService = activityService;
        }

        public List<ApplicationUser> Users { get; set; } = new();
        public List<Organization> Organizations { get; set; } = new();

        public async Task OnGetAsync()
        {
            Users = await _context.Users
                .Include(u => u.Organization)
                .ToListAsync();

            Organizations = await _context.Organizations
                .ToListAsync();
        }
    }
} 