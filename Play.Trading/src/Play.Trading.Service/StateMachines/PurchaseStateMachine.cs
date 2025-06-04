using System;
using System.Threading.Tasks;
using MassTransit;
using Play.Identity.Contracts;
using Play.Inventory.Contracts;
using Play.Trading.Service.Contracts;
using Play.Trading.Service.Entities;
using Play.Trading.Service.Exceptions;
using Play.Common;
using Play.Trading.Service.SignalR;

namespace Play.Trading.Service.StateMachines
{
    public class PurchaseStateMachine : MassTransitStateMachine<PurchaseState>
    {
        private readonly MessageHub hub;
        public required State Accepted { get; init; }
        public required State ItemsGranted { get; init; }
        public required State Completed { get; init; }
        public required State Faulted { get; init; }

        public required Event<PurchaseRequested> PurchaseRequested { get; init; }
        public required Event<GetPurchaseState> GetPurchaseState { get; init; }
        public required Event<InventoryItemsGranted> InventoryItemsGranted { get; init; }
        public required Event<GilDebited> GilDebited { get; init; }
        public required Event<Fault<GrantItems>> GrantItemsFaulted { get; init; }
        public required Event<Fault<DebitGil>> DebitGilFaulted { get; init; }

        private void ConfigureEvents()
        {
            Event(() => PurchaseRequested);
            Event(() => GetPurchaseState);
            Event(() => InventoryItemsGranted);
            Event(() => GilDebited);
            Event(() => GrantItemsFaulted, x => x.CorrelateById(context =>
                context.Message.Message.CorrelationId));
            Event(() => DebitGilFaulted, x => x.CorrelateById(context =>
                context.Message.Message.CorrelationId));
        }

        public PurchaseStateMachine(MessageHub hub)
        {
            InstanceState(state => state.CurrentState);
            ConfigureInitialState();
            ConfigureAny();
            ConfigureAccepted();
            ConfigureItemsGranted();
            ConfigureCompleted();
            ConfigureFaulted();
            this.hub = hub;
        }

        private void ConfigureInitialState()
        {
            Initially(
                When(PurchaseRequested)
                    .ThenAsync(async context =>
                    {
                        var repository = context.GetPayload<IServiceProvider>()
                                            .GetRequiredService<IRepository<CatalogItem>>();

                        var itemId = context.Message.ItemId;
                        var quantity = context.Message.Quantity;
                        var userId = context.Message.UserId;

                        var item = await repository.GetAsync(itemId);

                        if (item == null)
                            throw new UnknownItemException(itemId);

                        context.Saga.PurchaseTotal = item.Price * quantity;
                        context.Saga.UserId = userId;
                        context.Saga.ItemId = itemId;
                        context.Saga.Quantity = quantity;
                        context.Saga.Received = DateTimeOffset.UtcNow;
                        context.Saga.LastUpdated = context.Saga.Received;
                    })
                    .Send(context => new GrantItems(
                        context.Saga.UserId,
                        context.Saga.ItemId,
                        context.Saga.Quantity,
                        context.Saga.CorrelationId
                    ))
                    .TransitionTo(Accepted)
                    .Catch<Exception>(ex =>
                        ex.Then(context =>
                        {
                            context.Saga.ErrorMessage = context.Exception.Message;
                            context.Saga.LastUpdated = DateTimeOffset.UtcNow;
                            context.Saga.CurrentState = Faulted.Name;
                        })
                        .TransitionTo(Faulted)
                        .ThenAsync(async context => await hub.SendStatusAsync(context.Instance))
                    )
            );
        }

        private void ConfigureAccepted()
        {
            During(Accepted,
                Ignore(PurchaseRequested),
                When(InventoryItemsGranted)
                    .Then(context =>
                    {
                        context.Saga.LastUpdated = DateTimeOffset.UtcNow;
                    })
                    .Send(context =>
                    {
                        if (!context.Saga.PurchaseTotal.HasValue)
                            throw new InvalidOperationException("Purchase total is not set");

                        return new DebitGil(
                            context.Saga.UserId,
                            context.Saga.PurchaseTotal.Value,
                            context.Saga.CorrelationId
                        );
                    })
                    .TransitionTo(ItemsGranted),
                When(GrantItemsFaulted)
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = context.Message.Exceptions[0].Message;
                        context.Saga.LastUpdated = DateTimeOffset.UtcNow;
                    })
                    .TransitionTo(Faulted)
                    .ThenAsync(async context => await hub.SendStatusAsync(context.Instance))
            );
        }

        private void ConfigureItemsGranted()
        {
            During(ItemsGranted,
                Ignore(PurchaseRequested),
                Ignore(InventoryItemsGranted),
                When(GilDebited)
                    .Then(context =>
                    {
                        context.Saga.LastUpdated = DateTimeOffset.UtcNow;
                    })
                    .TransitionTo(Completed)
                    .ThenAsync(async context => await hub.SendStatusAsync(context.Instance)),
                When(DebitGilFaulted)
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = context.Message.Exceptions[0].Message;
                        context.Saga.LastUpdated = DateTimeOffset.UtcNow;
                    })
                    .Send(context => new SubtractItems(
                        context.Saga.UserId,
                        context.Saga.ItemId,
                        context.Saga.Quantity,
                        context.Saga.CorrelationId
                    ))
                    .Catch<Exception>(ex =>
                        ex.Then(context =>
                        {
                            context.Saga.ErrorMessage = $"Failed to subtract items: {context.Exception.Message}";
                            context.Saga.LastUpdated = DateTimeOffset.UtcNow;
                        })
                    )
                    .TransitionTo(Faulted)
                    .ThenAsync(async context => await hub.SendStatusAsync(context.Instance))
            );
        }

        private void ConfigureCompleted()
        {
            During(Completed,
                Ignore(PurchaseRequested),
                Ignore(InventoryItemsGranted),
                Ignore(GilDebited)
            );
        }

        private void ConfigureAny()
        {
            DuringAny(
                When(GetPurchaseState)
                    .Respond(x => x.Saga)
            );
        }

        private void ConfigureFaulted()
        {
            During(Faulted,
                Ignore(PurchaseRequested),
                Ignore(InventoryItemsGranted),
                Ignore(GilDebited));
        }
    }
}