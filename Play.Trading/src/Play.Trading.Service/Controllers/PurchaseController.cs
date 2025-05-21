using Play.Trading.Service.Dtos;
using Play.Trading.Service.StateMachines;
using MassTransit;
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

        public PurchaseController(
            IPublishEndpoint publishEndpoint,
            IRequestClient<GetPurchaseState> purchaseClient)
        {
            this.publishEndpoint = publishEndpoint;
            this.purchaseClient = purchaseClient;
        }

        [HttpGet("state/{correlationId}")]
        public async Task<ActionResult<PurchaseDto>> GetStatusAsync(Guid correlationId)
        {
            var response = await purchaseClient.GetResponse<PurchaseState>(
                new GetPurchaseState(correlationId));

            var purchaseState = response.Message;

            var purchase = new PurchaseDto(
                purchaseState.UserId,
                purchaseState.ItemId,
                purchaseState.PurchaseTotal,
                purchaseState.Quantity,
                purchaseState.CurrentState,
                purchaseState.ErrorMessage,
                purchaseState.Received,
                purchaseState.LastUpdated
            );

            return Ok(purchase);
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(SubmitPurchaseDto purchase)
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
