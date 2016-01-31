using Orchard.Logging;

namespace Softea.DirectoryServices.Utils
{
    public class Logger
    {
        public Logger()
        {
            Log = NullLogger.Instance;
        }

        public ILogger Log { get; set; }

        public void AddEvent(string eventText)
        {
            Log.Error(eventText);
        }
    }
}