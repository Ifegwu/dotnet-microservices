using System;
using MassTransit;
using Play.Trading.Service.Contracts;

namespace Play.Trading.Service.StateMachines
{
    public class PurchaseStateMachine : MassTransitStateMachine<PurchaseState>
    {
        public State Accepted { get; } = null!;
        public State ItemsGranted { get; } = null!;
        public State Completed { get; } = null!;
        public State Faulted { get; } = null!;

        public Event<PurchaseRequested> PurchaseRequested { get; } = null!;
        public Event<GetPurchaseState> GetPurchaseState { get; } = null!;

        public PurchaseStateMachine()
        {
            InstanceState(state => state.CurrentState);
            ConfigureEvents();
            ConfigureInitialState();
            ConfigureAny();
        }

        private void ConfigureEvents()
        {
            Event(() => PurchaseRequested);
            Event(() => GetPurchaseState);
        }

        private void ConfigureInitialState()
        {
            Initially(
                When(PurchaseRequested)
                    .Then(context =>
                    {
                        var saga = context.Saga;
                        saga.UserId = context.Message.UserId;
                        saga.ItemId = context.Message.ItemId;
                        saga.Quantity = context.Message.Quantity;
                        saga.Received = DateTimeOffset.UtcNow;
                        saga.LastUpdated = saga.Received;
                    })
                    .TransitionTo(Accepted)
            );
        }
        private void ConfigureAny()
        {
            DuringAny(
                When(GetPurchaseState)
                    .Respond(x => x.Instance)
            );
        }
    }
}