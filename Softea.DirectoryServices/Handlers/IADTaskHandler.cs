using Orchard.Tasks.Scheduling;

namespace Softea.DirectoryServices.Handlers
{
    public interface IADTaskHandler : IScheduledTaskHandler
    {
        int RunJob(string Id);
    }
}
