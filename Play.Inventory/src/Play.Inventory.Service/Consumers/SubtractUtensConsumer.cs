using System;
using System.Threading.Tasks;
using MassTransit;
using Play.Common;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;
using Play.Inventory.Contracts;

namespace Play.Inventory.Service.Consumers
{
    public class SubtractUtensConsumer : IConsumer<SubtractUtens>
    {
        private readonly IRepository<InventoryItem> inventoryItemsRepository;
        private readonly IRepository<CatalogItem> catalogItemsRepository;

        public SubtractUtensConsumer(
            IRepository<InventoryItem> inventoryItemsRepository,
            IRepository<CatalogItem> catalogItemsRepository)
        {
            this.inventoryItemsRepository = inventoryItemsRepository;
            this.catalogItemsRepository = catalogItemsRepository;
        }

        public async Task Consume(ConsumeContext<SubtractUtens> context)
        {
            var message = context.Message;

            var catalogItem = await catalogItemsRepository.GetAsync(message.CatalogItemId);
            if (catalogItem == null)
            {
                throw new UnknownItemException(message.CatalogItemId);
            }

            var inventoryItem = await inventoryItemsRepository.GetAsync(
                item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);

            if (inventoryItem == null)
            {
                await context.RespondAsync<UtensSubtractionFailed>(new
                {
                    UserId = message.UserId,
                    CatalogItemId = message.CatalogItemId,
                    Reason = "Item not in inventory"
                });
                return;
            }

            if (inventoryItem.Quantity < message.Quantity)
            {
                await context.RespondAsync<UtensSubtractionFailed>(new
                {
                    UserId = message.UserId,
                    CatalogItemId = message.CatalogItemId,
                    Reason = "Insufficient quantity"
                });
                return;
            }

            inventoryItem.Quantity -= message.Quantity;
            await inventoryItemsRepository.UpdateAsync(inventoryItem);

            await context.RespondAsync<UtensSubtractionSucceeded>(new
            {
                UserId = message.UserId,
                CatalogItemId = message.CatalogItemId
            });
        }
    }
}