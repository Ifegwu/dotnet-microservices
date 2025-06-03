using Play.Trading.Service.Dtos;
using Play.Trading.Service.StateMachines;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Trading.Service.Contracts;
using System.Security.Claims;
using Play.Common;
using Play.Trading.Service.Entities;
using Play.Inventory.Contracts;

namespace Play.Trading.Service.Controllers
{
    [ApiController]
    [Route("store")]
    [Authorize]
    public class StoreController : ControllerBase
    {
        private readonly IRepository<CatalogItem> catalogRepository;
        private readonly IRepository<ApplicationUser> usersRepository;
        private readonly IRepository<InventoryItem> inventoryRepository;

        public StoreController(
            IRepository<CatalogItem> catalogRepository,
            IRepository<ApplicationUser> usersRepository,
            IRepository<InventoryItem> inventoryRepository)
        {
            this.catalogRepository = catalogRepository;
            this.usersRepository = usersRepository;
            this.inventoryRepository = inventoryRepository;
        }

        [HttpGet]
        public async Task<ActionResult<StoreDto>> GetAsync()
        {
            var userId = User.FindFirstValue("sub");
            if (userId == null)
            {
                return Unauthorized();
            }

            var catalogItems = await catalogRepository.GetAllAsync();
            var inventoryItems = await inventoryRepository.GetAllAsync(
                item => item.UserId == Guid.Parse(userId)
            );
            var user = await usersRepository.GetAsync(Guid.Parse(userId));

            var storeDto = new StoreDto(
                catalogItems.Select(catalogItem => new StoreItemDto(
                    catalogItem.Id,
                    catalogItem.Name,
                    catalogItem.Description,
                    catalogItem.Price,
                    inventoryItems.FirstOrDefault(
                        inventoryItem => inventoryItem.CatalogItemId == catalogItem.Id
                    )?.Quantity ?? 0
                )),
                user?.Gil ?? 0
            );

            return Ok(storeDto);
        }
    }
}
