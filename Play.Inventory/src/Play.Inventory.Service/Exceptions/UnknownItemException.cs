using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Play.Inventory.Service.Exceptions
{
    [Serializable]
    internal class UnknownItemException : Exception
    {
        public UnknownItemException(Guid catalogItemId) : base($"Unknown item '{catalogItemId}'")
        {
            this.ItemId = catalogItemId;
        }
        public Guid ItemId { get; }
    }
}