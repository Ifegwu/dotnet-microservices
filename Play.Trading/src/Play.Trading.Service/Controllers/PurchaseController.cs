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
        readonly IPublishEndpoint publishEndpoint;
        private readonly IRequestClient<GetPurchaseState> purchaseClient;

        public PurchaseController(IPublishEndpoint publishEndpoint, IRequestClient<GetPurchaseState> purchaseClient)
        {
            this.publishEndpoint = publishEndpoint;
            this.purchaseClient = purchaseClient;
        }

        [HttpGet("state/{correlationId}")]
        public async Task<IActionResult> GetStatusAsync(Guid correlationId)
        {
            var response = await purchaseClient.GetResponse<PurchaseState>(
                new GetPurchaseState(correlationId));

            var purchaseState = response.Message;

            var purchaseDto = new PurchaseDto(
                purchaseState.UserId,
                purchaseState.ItemId,
                purchaseState.PurchaseTotal,
                purchaseState.Quantity,
                purchaseState.CurrentState,
                purchaseState.ErrorMessage,
                purchaseState.Received,
                purchaseState.LastUpdated
            );

            return Ok(purchaseDto);
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

            return AcceptedAtAction(
                nameof(GetStatusAsync),
                new { correlationId },
                new { correlationId }
            );
        }
    }
}
