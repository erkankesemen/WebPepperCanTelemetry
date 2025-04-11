using WebPepperCan.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebPepperCan.Services
{
    public interface IOrganizationService
    {
        Task<List<Organization>> GetAllAsync();
    }
} 