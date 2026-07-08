namespace Credit.Kafka.Messaging.Handlers
{
    public interface IBatchMultiConsumer_Start_Stop
    {
        //public void Service_Enable_Toggle(Dictionary<string,bool> newConfig);
        public Task Service_Enable_Toggle();
    }
}
