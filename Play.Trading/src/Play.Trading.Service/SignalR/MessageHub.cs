using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Play.Trading.Service.StateMachines;

namespace Play.Trading.Service.SignalR
{
    [Authorize]
    public class MessageHub : Hub
    {
        public async Task SendStatusAsync(PurchaseState status)
        {
            if (Clients != null && Context.UserIdentifier != null)
            {
                await Clients.User(Context.UserIdentifier)
                    .SendAsync("ReceivePurchaseStatus", status);
            }
        }
    }
}