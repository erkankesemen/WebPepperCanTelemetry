using System.Threading.Tasks;

namespace WebPepperCan.Services
{
    public interface IActivityService
    {
        Task LogActivity(string userId, string action, string description, string ipAddress, string userAgent);
    }
} 