using System.Web.Mvc;
using Softea.DirectoryServices.Handlers;

namespace Softea.DirectoryServices.Controllers
{
    public class ActiveDirectoryController : Controller
    {
        private readonly IADTaskHandler adTaskHandler;

        public ActiveDirectoryController(IADTaskHandler adTaskHandler)
        {
            this.adTaskHandler = adTaskHandler;
        }


        /// <summary>
        /// Updates the users from active directory.
        /// </summary>
        /// <param name="Id">The id of ldap directory.</param>
        /// <returns></returns>
        public int Update(string Id)
        {
            var usersCount = 0;
            try
            {
                usersCount = adTaskHandler.RunJob(Id);
            }
            catch
            {
                return -1;
            }
            return usersCount;
        }
    }
}