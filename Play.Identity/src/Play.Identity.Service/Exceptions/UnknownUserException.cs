using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Play.Identity.Service.Exceptions
{
    [Serializable]
    internal class UnknownUserException : Exception
    {
        public UnknownUserException(Guid userId)
            : base($"Uknown user '{userId}'")
        {
            this.UserId = userId;
        }

        public Guid UserId { get; }
    }
}