namespace Core.ServiceBus
{
    public class EventBusOptions
    {
        public string Topic { get; set; }

        public string Subscription { get; set; }

        public string ConnectionString { get; set; }

        /// <summary>
        ///     Default value is 10.
        /// </summary>
        public int MaxConcurrentCalls { get; set; } = 10;

        /// <summary>
        ///     Default value is 10.
        /// </summary>
        public int MaxDeliveryCount { get; set; } = 10;

        /// <summary>
        ///     Default value is false.
        /// </summary>
        public bool UseSessions { get; set; } = false;

        public bool UseAzureServiceBus { get; set; }
    }
}
