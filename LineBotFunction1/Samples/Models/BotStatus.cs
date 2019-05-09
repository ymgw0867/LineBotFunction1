using Microsoft.WindowsAzure.Storage.Table;

namespace LineBotFunction1
{
    public class BotStatus : EventSourceState
    {
        public string Location { get; set; }
        public string CurrentApp { get; set; }

        public BotStatus() { }
    }
}
