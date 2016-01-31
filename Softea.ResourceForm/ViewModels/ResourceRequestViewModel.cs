using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Orchard.Localization;

namespace Softea.ResourceForm.ViewModels
{
    public class ResourceRequestViewModel
    {

        [Required]
        public string Name { get; set; }

        [Required]
        public string URL { get; set; }

        [Required]
        [RegularExpression("^([a-zA-Z0-9_\\-\\.]+)@((\\[[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.)|(([a-zA-Z0-9\\-]+\\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\\]?)$")]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string BusinessSubject { get; set; }

        public string Period { get; set; }

        public string Description { get; set; }

        public IList<ResourceViewModel> AddedResources { get; set; }

        public ResourceViewModel ResourceToAdd { get; set; }

        public string actualLanguage { get; set; }

        public IEnumerable<string> AvailableLanguages { get; set; }

        public int Result { get; set; }

        public string PublicKey { get; set; }

        public bool ShowIfLogged { get; set; }

        public string recaptcha_challenge_field { get; set; }

        public string recaptcha_response_field { get; set; }
    }
}