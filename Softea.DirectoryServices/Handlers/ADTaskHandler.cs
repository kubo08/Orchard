using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Web;
using NHibernate.Util;
using Orchard.ContentManagement;
using Softea.DirectoryServices.Models;
using Softea.DirectoryServices.Services;
using Orchard.Logging;
using Orchard.Tasks.Scheduling;

namespace Softea.DirectoryServices.Handlers
{
    public class ADTaskHandler : IADTaskHandler
    {
        private const string TaskType = "ADUpdaterTaks";
        private readonly IScheduledTaskManager _taskManager;
        private readonly IADUpdaterService _myService;
        private readonly IContentManager _contentManager;
        readonly ILdapDirectoryCache ldapDirectoryCache;

        public ILogger Logger { get; set; }

        public ADTaskHandler(IScheduledTaskManager taskManager,
            ILdapDirectoryCache ldapDirectoryCache, IADUpdaterService myService, IContentManager contentManager)
        {
            _myService = myService;
            _contentManager = contentManager;
            this.ldapDirectoryCache = ldapDirectoryCache;
            _taskManager = taskManager;
            Logger = NullLogger.Instance;
            try
            {
                DateTime firstDate = DateTime.UtcNow.AddHours(1);
                ScheduleNextTask(firstDate);
            }
            catch (Exception e)
            {
                this.Logger.Error(e, e.Message);
            }
        }

        //planovane tasky
        /// <summary>
        /// runs scheduled task to synchronize users with active directory.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Process(ScheduledTaskContext context)
        {
            if (context.Task.TaskType == TaskType)
            {
                try
                {
                    RunJob(null);
                }
                catch (DbException e)
                {
                    Logger.Error(string.Format("Nastala chyba pri praci z databazou, pravdepodobne sa uz vykonava jeden update. {0}", e.Message));
                }
            }
        }

        //trigger
        /// <summary>
        /// Runs the job to synchronize users.
        /// </summary>
        /// <param name="Id">The identifier.</param>
        /// <returns></returns>
        public int RunJob(string Id)
        {
            RemoveTasks();
            int? directoryId = null;
            if (Id != null)
            {
                directoryId = int.Parse(Id);
            }
            IList<ILdapDirectory> directories;
            if (!directoryId.HasValue)
            {
                directories = ldapDirectoryCache.GetDirectories().ToList();
            }
            else
            {
                directories = ldapDirectoryCache.GetDirectories().Where(i => i.Id == directoryId.Value).ToList();
            }

            var usersCount = 0;
            try
            {
                usersCount = _myService.RunJob(directories);
            }
            catch (DbException e)
            {
                Logger.Error(string.Format("Nastala chyba pri praci z databazou, pravdepodobne sa uz vykonava jeden update. {0}", e.Message));
            }
            catch (Exception e)
            {
                this.Logger.Error(e, e.Message);
                throw e;
            }
            finally
            {
                int period = 1;
                if (directories.Count > 0)
                {
                    period = directories.Min(i => i.UpdatePeriod);
                }
                DateTime nextTaskDate = DateTime.UtcNow.AddMinutes(period);
                ScheduleNextTask(nextTaskDate);
            }
            return usersCount;
        }

        /// <summary>
        /// Removes scheduled tasks.
        /// </summary>
        private void RemoveTasks()
        {
            try
            {
                _taskManager.DeleteTasks(null, task => (task.TaskType == TaskType));
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, ex.Message);
            }
        }

        /// <summary>
        /// Schedules the next task.
        /// </summary>
        /// <param name="date">The date.</param>
        private void ScheduleNextTask(DateTime date)
        {
            if (date > DateTime.UtcNow)
            {
                try
                {
                    _taskManager.DeleteTasks(null, task => (task.TaskType == TaskType));
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex, ex.Message);
                }
                this._taskManager.CreateTask(TaskType, date, null);
            }


        }
    }
}