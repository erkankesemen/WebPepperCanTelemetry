using Microsoft.EntityFrameworkCore;
using WebPepperCan.Data;

namespace WebPepperCan.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly ApplicationDbContext _context;

        public OrganizationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Organization>> GetAllAsync()
        {
            return await _context.Organizations.ToListAsync();
        }
    }
} 