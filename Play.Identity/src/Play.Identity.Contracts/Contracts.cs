using System;

namespace Play.Identity.Contracts
{
    public record DebitGil(Guid UserId, decimal Gil, Guid CatalogItemId);
    public record GilDebited(Guid CatalogItemId);
}
