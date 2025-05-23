using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Play.Identity.Service.Exceptions
{
    [Serializable]
    internal class InsufficientFundsException : Exception
    {
        public InsufficientFundsException(Guid userId, decimal gilToDebit)
            : base($"Not enough gil to debit {gilToDebit} from user {userId}")
        {
            this.UserId = userId;
            this.GilToDebit = gilToDebit;
        }

        public Guid UserId { get; }
        public decimal GilToDebit { get; }
    }
}
