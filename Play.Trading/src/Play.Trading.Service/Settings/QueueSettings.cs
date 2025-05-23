using System;

namespace Play.Trading.Service.Settings
{
    public class QueueSettings
    {
        public required string GrantItemsQueueAddress { get; init; }
        public required string DebitGilQueueAddress { get; init; }
    }
}