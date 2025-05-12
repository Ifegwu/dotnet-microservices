using System;
using Play.Common;

namespace Play.Trading.Service.Entities
{
    public class Purchase : IEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }
    }
}