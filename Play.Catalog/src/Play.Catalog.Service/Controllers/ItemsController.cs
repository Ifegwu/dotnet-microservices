using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Common;

namespace Play.Catalog.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private const string AdminRole = "Admin";
        private readonly IRepository<Item> itemsRepository;
        private readonly IPublishEndpoint publicEndpoint;

        public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publicEndpoint)
        {
            this.itemsRepository = itemsRepository;
            this.publicEndpoint = publicEndpoint;
        }

        //GET /items
        [HttpGet]
        [Authorize(Policies.Read)]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            var items = (await itemsRepository.GetAllAsync())
                        .Select(item => item.AsDto());

            return Ok(items);
        }

        // GET /items/{id}
        [HttpGet("{id}")]
        [Authorize(Policies.Read)]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await itemsRepository.GetAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return item.AsDto();
        }

        // POST /items
        [HttpPost]
        [Authorize(Policies.Write)]
        public async Task<ActionResult<ItemDto>> PostAsync(CreatedItemDto createdItemDto)
        {
            var item = new Item
            {
                Id = Guid.NewGuid(),
                Name = createdItemDto.Name,
                Description = createdItemDto.Description,
                Price = createdItemDto.Price,
                CreatedDate = DateTimeOffset.UtcNow
            };

            await itemsRepository.CreateAsync(item);
            await publicEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description, item.Price));

            var itemDto = item.AsDto();
            return Created($"/items/{item.Id}", itemDto);
        }

        // PUT /items/{id}
        [HttpPut("{id}")]
        [Authorize(Policies.Write)]
        public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {
            var existingItem = await itemsRepository.GetAsync(id);
            if (existingItem == null)
            {
                return NoContent();
            }

            existingItem.Name = updateItemDto.Name;
            existingItem.Description = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await itemsRepository.UpdateAsync(existingItem);
            await publicEndpoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description, existingItem.Price));

            return NoContent();
        }

        // DELETE /items/{id}
        [HttpDelete("{id}")]
        [Authorize(Policies.Write)]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var item = await itemsRepository.GetAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            await itemsRepository.RemoveAsync(item.Id);
            await publicEndpoint.Publish(new CatalogItemDeleted(item.Id));

            return NoContent();
        }
    }
}