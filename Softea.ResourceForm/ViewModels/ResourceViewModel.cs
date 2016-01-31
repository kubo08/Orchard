using System.Collections.Generic;
using System.Web.Mvc;

namespace Softea.ResourceForm.ViewModels
{
    public class ResourceViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int Count { get; set; }

        public int ResourceType { get; set; }

        public int ResourceName { get; set; }

        public int AvailableCapacity { get; set; }

        public bool IsAvailableExternally { get; set; }

        public string TimeUsageDescription { get; set; }

        public IList<int> emailList { get; set; }


        public IList<SelectListItem> ResourceTypesList { get; set; }

        public IList<SelectListItem> ResourceNamesList { get; set; }
    }
}