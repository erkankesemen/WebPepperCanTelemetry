using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebPepperCan.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebPepperCan.Pages.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    public class OrganizationManagementModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public OrganizationManagementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Organization> Organizations { get; set; } = new();

        public async Task OnGetAsync()
        {
            Organizations = await _context.Organizations
                .Include(o => o.Users)
                .ToListAsync();
        }
    }
} 