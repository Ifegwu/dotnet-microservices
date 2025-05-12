using Play.Trading.Service.Dtos;
using Play.Trading.Service.StateMachines;
using Play.Common;
using MassTransit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Trading.Service.Contracts;
using System.Security.Claims;
namespace Play.Trading.Service.Controllers
{
    [ApiController]
    [Route("purchase")]
    [Authorize]
    public class PurchaseController : ControllerBase
    {
        private readonly IPublishEndpoint publishEndpoint;

        public PurchaseController(IPublishEndpoint publishEndpoint)
        {
            this.publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> Post(SubmitPurchaseDto purchase)
        {
            var userId = User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var correlationId = Guid.NewGuid();

            var message = new PurchaseRequested(
                Guid.Parse(userId),
                purchase.ItemId!.Value,
                purchase.Quantity,
                correlationId
            );

            await publishEndpoint.Publish(message);

            return Accepted();
        }
    }
}
