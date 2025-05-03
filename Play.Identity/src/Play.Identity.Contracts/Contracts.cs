using System;

namespace Play.Identity.Contracts
{
    public record DebitGil(Guid UserId, Guid CatalogItemId);
    public record GilDebited(Guid CatalogItemId);
}
