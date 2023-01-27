namespace CommandsService.EventProcessing
{
    public interface IEventProcessor
    {
        void ProcessEvent(string message); // we gon deserialize the msg and do something with this event
    }
}