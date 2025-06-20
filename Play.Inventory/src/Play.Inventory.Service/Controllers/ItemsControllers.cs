using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Play.Common;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Clients;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Play.Inventory.Contracts;
using MassTransit;

namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    [Authorize]
    public class ItemsController : ControllerBase
    {
        private const string AdminRole = "Admin";
        private readonly IRepository<InventoryItem> inventoryItemsRepository;
        private readonly IRepository<CatalogItem> catalogItemsRepository;
        private readonly IPublishEndpoint publishEndpoint;
        // private readonly CatalogClient catalogClient;
        public ItemsController(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository, IPublishEndpoint publishEndpoint)
        {
            this.inventoryItemsRepository = inventoryItemsRepository ?? throw new ArgumentNullException(nameof(inventoryItemsRepository));
            this.catalogItemsRepository = catalogItemsRepository;
            this.publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (Guid.Parse(currentUserId) != userId)
            {
                if (!User.IsInRole(AdminRole))
                {
                    return Forbid();
                }
            }

            var inventoryItemEntities = await inventoryItemsRepository.GetAllAsync(item => item.UserId == userId);
            var itemIds = inventoryItemEntities.Select(item => item.CatalogItemId);
            var catalogItemEntities = await catalogItemsRepository.GetAllAsync(item => itemIds.Contains(item.Id));

            var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItemEntities.SingleOrDefault(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem?.Name ?? string.Empty, catalogItem?.Description ?? string.Empty);
            });

            return Ok(inventoryItemDtos);
        }

        [HttpPost]
        [Authorize(Roles = AdminRole)]
        public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
        {
            var inventoryItem = await inventoryItemsRepository.GetAsync(
                item => item.UserId == grantItemsDto.UserId && item.CatalogItemId == grantItemsDto.CatalogItemId);
            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = grantItemsDto.CatalogItemId,
                    UserId = grantItemsDto.UserId,
                    Quantity = grantItemsDto.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };
                await inventoryItemsRepository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity += grantItemsDto.Quantity;
                await inventoryItemsRepository.UpdateAsync(inventoryItem);
            }

            await publishEndpoint.Publish(new InventoryItemUpdated(
                inventoryItem.UserId,
                inventoryItem.CatalogItemId,
                inventoryItem.Quantity
            ));

            return Ok();

        }
    }

}