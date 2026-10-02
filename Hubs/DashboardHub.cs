using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Zentro.Hubs
{
    [Authorize]
    public class DashboardHub : Hub
    {
    }
}