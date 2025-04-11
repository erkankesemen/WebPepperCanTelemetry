using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace WebPepperCan.Hubs
{
    public class DashboardHub : Hub
    {
        public async Task UpdateDashboardData(object data)
        {
            await Clients.All.SendAsync("UpdateDashboard", data);
        }
    }
} 