using Orchard.ContentManagement;

namespace Softea.ResourceForm.Models
{
    public class ResourceFormPart : ContentPart<ResourceFormPartRecord>, IResourceForm
    {
        int IResourceForm.ID
        {
            get { return Record.Id; }
        }

        public string MasterEmail
        {
            get { return Record.MasterEmail; }
            set { Record.MasterEmail = value; }
        }
    }
}