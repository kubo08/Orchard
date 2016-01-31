using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement.Records;

namespace Softea.ResourceForm.Models
{
    public class ResourceFormPartRecord : ContentPartRecord
    {
        public virtual string MasterEmail { get; set; }
    }
}