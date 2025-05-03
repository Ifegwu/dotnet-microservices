using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Play.Common;
using Play.Identity.Service.Entities;
using Play.Inventory.Contracts;

namespace Play.Identity.Service.Consumers
{
    public class DebitGilConsumer : IConsumer<DebitGil>
    {
        private readonly UserManager<ApplicationUser> userManager;

        public DebitGilConsumer(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
        }

        public async Task Consume(ConsumeContext<DebitGil> context)
        {
            var message = context.Message;
            var user = await userManager.FindByIdAsync(message.UserId.ToString());

            if (user == null)
            {
                await context.RespondAsync<GilDebitFailed>(new
                {
                    UserId = message.UserId,
                    Reason = "User not found"
                });
                return;
            }

            if (user.Gil < message.Gil)
            {
                await context.RespondAsync<GilDebitFailed>(new
                {
                    UserId = message.UserId,
                    Reason = "Insufficient funds"
                });
                return;
            }

            user.Gil -= message.Gil;
            await userManager.UpdateAsync(user);

            await context.RespondAsync<GilDebitSucceeded>(new
            {
                UserId = message.UserId
            });
        }
    }
}