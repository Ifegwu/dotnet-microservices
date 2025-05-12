using System;
using System.Runtime.Serialization;

namespace Play.Trading.Service.Contracts
{
    [DataContract]
    public record PurchaseRequested(Guid UserId, Guid ItemId, int Quantity, Guid CorrelationId);
}