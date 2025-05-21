using System;
using System.Threading.Tasks;
using MassTransit;
using Play.Inventory.Contracts;
using Play.Trading.Service.Contracts;
using Play.Trading.Service.Entities;
using Play.Trading.Service.Exceptions;
using Play.Common;

namespace Play.Trading.Service.StateMachines
{
    public class PurchaseStateMachine : MassTransitStateMachine<PurchaseState>
    {
        public State Accepted { get; }
        public State ItemsGranted { get; }
        public State Completed { get; }
        public State Faulted { get; }

        public Event<PurchaseRequested> PurchaseRequested { get; }
        public Event<GetPurchaseState> GetPurchaseState { get; }

        public Event<InventoryItemsGranted> InvetoryItemsGranted { get; }
        public PurchaseStateMachine()
        {
            InstanceState(state => state.CurrentState);
            ConfigureEvents();
            ConfigureInitialState();
            ConfigureAny();
            ConfigureAccepted();
        }

        private void ConfigureEvents()
        {
            Event(() => PurchaseRequested);
            Event(() => GetPurchaseState);
            Event(() => InvetoryItemsGranted);
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
                    )
            );
        }

        private void ConfigureAccepted()
        {
            During(Accepted,
                When(InvetoryItemsGranted)
                    .Then(context =>
                    {
                        context.Saga.LastUpdated = DateTimeOffset.UtcNow;
                    })
                    .TransitionTo(ItemsGranted)
            );
        }
        private void ConfigureAny()
        {
            DuringAny(
                When(GetPurchaseState)
                    .Respond(x => x.Saga)
            );
        }
    }
}