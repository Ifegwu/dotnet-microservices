using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Play.Trading.Service.SignalR
{
    public class UserIdProvider : IUserIdProvider
    {
        public virtual string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
    }
}