using System;
using Play.Common;

namespace Play.Trading.Service.Entities
{
    public class CatalogItem : IEntity
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public decimal Price { get; set; }
    }
}