using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Dolph.SmartContentPicker.Fields;
using Orchard;
using Orchard.AntiSpam.Models;
using Orchard.ContentManagement;
using Orchard.ContentPicker.Fields;
using Orchard.Core.Common.Fields;
using Orchard.Core.Title.Models;
using Orchard.Fields.Fields;
using Orchard.Localization;
using Orchard.Localization.Models;
using Orchard.Localization.Services;
using Orchard.Logging;
using Orchard.Tasks.Indexing;
using Orchard.Themes;
using Softea.ResourceForm.Services;
using Softea.ResourceForm.ViewModels;

namespace Softea.ResourceForm.Controllers
{
    [Themed]
    public class ResourceFormController : Controller
    {
        private readonly IContentManager contentManager;
        private readonly ICultureManager cultureManager;
        private readonly IEmailService emailManager;
        private readonly IOrchardServices orchardServices;
        private readonly IIndexingTaskManager _indexingTasks;
        private string actualLanguage = "sk-SK";
        private const string ReCaptchaUrl = "http://www.google.com/recaptcha/api";
        private ILogger Logger;

        public ResourceFormController(IContentManager contentManager, ICultureManager cultureManager, IEmailService emailService, IOrchardServices services, IIndexingTaskManager indexingTasks)
        {
            this.contentManager = contentManager;
            this.cultureManager = cultureManager;
            this.emailManager = emailService;
            orchardServices = services;
            _indexingTasks = indexingTasks;

            Logger = NullLogger.Instance;
        }

        // GET: ResourceForm
        [HttpGet]
        public ActionResult Index(int? ResourceID)
        {
            actualLanguage = orchardServices.WorkContext.CurrentCulture;

            var model = new ResourceRequestViewModel
            {
                ResourceToAdd = new ResourceViewModel
                {
                    ResourceNamesList = new List<SelectListItem>(),
                    ResourceTypesList = new List<SelectListItem>()
                },
                actualLanguage = actualLanguage
            };
            model.ResourceToAdd.Count = 1;
            model.ResourceToAdd.ResourceTypesList = GetResourceTypes();
            model.ResourceToAdd.ResourceNamesList = GetResourceNames(int.Parse(model.ResourceToAdd.ResourceTypesList.FirstOrDefault().Value));
            model.AddedResources = new List<ResourceViewModel>();
            if (ResourceID.HasValue)
            {
                model.AddedResources.Add(AddResourceToModel(ResourceID.Value, 1));
            }


            model.AvailableLanguages = cultureManager.ListCultures().Where(x => x != actualLanguage);

            var settings = orchardServices.WorkContext.CurrentSite.As<ReCaptchaSettingsPart>();
            model.PublicKey = settings.PublicKey;
            model.ShowIfLogged = !settings.TrustAuthenticatedUsers;

            return View(model);
        }


        [HttpPost]
        public ActionResult Index(ResourceRequestViewModel model, string AddResource, string Send, string Cancel, string RemoveResource, int? RemoveId)
        {

            bool reset = false;
            var settings = orchardServices.WorkContext.CurrentSite.As<ReCaptchaSettingsPart>();
            actualLanguage = model.actualLanguage;
            model.ResourceToAdd.ResourceTypesList = GetResourceTypes();
            model.ResourceToAdd.ResourceNamesList = GetResourceNames(model.ResourceToAdd.ResourceType);
            model.AvailableLanguages = cultureManager.ListCultures().Where(x => x != actualLanguage);

            if (AddResource != null)
            {
                RemoveErrors();


                model.ResourceToAdd.ResourceType = int.Parse(model.ResourceToAdd.ResourceTypesList.FirstOrDefault().Value);
                model.ResourceToAdd.ResourceNamesList = GetResourceNames(int.Parse(model.ResourceToAdd.ResourceTypesList.FirstOrDefault().Value));
                ModelState.Remove("ResourceToAdd.ResourceType");
                ModelState.Remove("ResourceToAdd.Count");
                if (model.AddedResources == null)
                    model.AddedResources = new List<ResourceViewModel>();
                model.AddedResources.Add(AddResourceToModel(model.ResourceToAdd.ResourceName, model.ResourceToAdd.Count));
            }

            if (Send != null && ModelState.IsValid)
            {
                var valid = true;
                if (!User.Identity.IsAuthenticated || !settings.TrustAuthenticatedUsers)
                {
                    var result = ExecuteValidateRequest(
                        settings.PrivateKey,
                        orchardServices.WorkContext.HttpContext.Request.ServerVariables["REMOTE_ADDR"],
                        model.recaptcha_challenge_field,
                        model.recaptcha_response_field
                        );
                    valid = HandleValidateResponse(null, result);
                }

                if (!valid)
                {
                    ModelState.AddModelError("Captcha", ("You did not type the verification word correctly. Please try again."));
                }
                else
                {
                    ContentItem request;

                    request = contentManager.New("ResourceRequest");


                    request.As<TitlePart>().Title = model.Name;

                    contentManager.Create(request);

                    FillRequest(request, model);

                    contentManager.Publish(request);
                    _indexingTasks.CreateUpdateIndexTask(request);

                    var metadata = contentManager.GetItemMetadata(request);
                    var index = Request.Url.AbsoluteUri.IndexOf(Request.Url.AbsolutePath, StringComparison.Ordinal);
                    var basePath = Request.Url.AbsoluteUri.Substring(0, index);
                    if (basePath.EndsWith("/"))
                        basePath = basePath.Substring(0, basePath.Length - 1);
                    basePath += Request.ApplicationPath;
                    var path = (String.Format("{0}/{1}/{2}/{3}/{4}", basePath, metadata.DisplayRouteValues["area"], metadata.DisplayRouteValues["controller"], metadata.DisplayRouteValues["action"], metadata.DisplayRouteValues["Id"]));
                    try
                    {
                        var body = String.Format("Podrobnosti o požiadavke na rezerváciu kapacity nájdete na: {0}", path);

                        const string subject = "AgroBioTech portál: Bola vytvorená nová požiadavka na rezerváciu kapacity";
                        var recipients = GetRecipients(model);
                        emailManager.Send(body, subject, recipients);
                    }
                    catch (NullReferenceException)
                    {
                        //cele toto zmazat po fixnuti
                    }
                    model.Result = 1;
                }
            }
            if (RemoveResource != null)
            {
                if (RemoveId.HasValue)
                {
                    model.AddedResources.RemoveAt(RemoveId.Value);
                }
            }

            if (Cancel != null)
            {

            }

            if (model.AddedResources == null)
                model.AddedResources = new List<ResourceViewModel>();

            model.PublicKey = settings.PublicKey;
            model.ShowIfLogged = !settings.TrustAuthenticatedUsers;
            return View(model);
        }

        [ValidateAntiForgeryToken]
        public ActionResult ResourceTypeChanged(string selection)
        {
            var typeId = int.Parse(selection);
            var data = GetResourceNames(typeId);
            return Json(data);
        }

        /// <summary>
        /// Adds new resource to the form.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        [ValidateAntiForgeryToken]
        public ActionResult AddResource(ResourceRequestViewModel model)
        {
            return Json("");
        }

        /// <summary>
        /// Gets the recipients.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        private string GetRecipients(ResourceRequestViewModel model)
        {
            string recipients = "";
            var employees = contentManager.Query().ForType("Employee").List();
            if (model.AddedResources != null)
            {
                foreach (var resourceVievModel in model.AddedResources)
                {
                    if (resourceVievModel.emailList != null)
                    {
                        var ids = resourceVievModel.emailList;

                        if (ids != null)
                        {
                            foreach (var id in ids)
                            {
                                try
                                {
                                    int id1 = id;
                                    var employee = employees.FirstOrDefault(x => x.Id == id1);
                                    var employeeItem = (dynamic)employee;
                                    string email = employeeItem.Employee.Primaryemail.Value;
                                    if (email != null)
                                    {
                                        if (!recipients.Contains(email))
                                        {
                                            if (recipients != "")
                                                recipients += ", ";
                                            recipients += email;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("Nastala chyba pri nacitavani prijemcov pre email.");
                                }
                            }
                        }
                    }
                }
            }
            return recipients;
        }

        /// <summary>
        /// Adds the resource to model.
        /// </summary>
        /// <param name="ResourceID">The resource identifier.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private ResourceViewModel AddResourceToModel(int ResourceID, int count)
        {
            var resource = GetResource(ResourceID);

            var resourceItem = (dynamic)resource;
            var item = new ResourceViewModel
            {
                AvailableCapacity =
                    (int)resourceItem.Resource.AvailableCapacity.Value,
                Count = count,
                IsAvailableExternally =
                    (bool)resourceItem.Resource.IsAvailableExternally.Value,
                Name = resource.As<Orchard.Core.Title.Models.TitlePart>().Title,
                TimeUsageDescription = resourceItem.Resource.TimeUsageDescription.Value,
                Id = ResourceID,
                emailList = ((int[])resourceItem.Resource.DistributionList.Ids).ToList()
            };


            return item;
        }

        private void RemoveErrors()
        {
            if (ModelState.ContainsKey("Name"))
                ModelState["Name"].Errors.Clear();
            if (ModelState.ContainsKey("URL"))
                ModelState["URL"].Errors.Clear();
            if (ModelState.ContainsKey("Email"))
                ModelState["Email"].Errors.Clear();
            if (ModelState.ContainsKey("Phone"))
                ModelState["Phone"].Errors.Clear();
            if (ModelState.ContainsKey("BusinessSubject"))
                ModelState["BusinessSubject"].Errors.Clear();
        }

        /// <summary>
        /// Fills the request after form submit.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="model">The model.</param>
        private void FillRequest(ContentItem item, ResourceRequestViewModel model)
        {
            var contentItem = (dynamic)item;
            var part = (ContentPart)contentItem.ResourceRequest;
            foreach (var field in part.Fields)
            {
                switch (field.Name)
                {
                    case "URL":
                        ((TextField)field).Value = model.URL;
                        break;
                    case "email":
                        ((TextField)field).Value = model.Email;
                        break;
                    case "Phone":
                        ((TextField)field).Value = model.Phone;
                        break;
                    case "BusinessArea":
                        ((TextField)field).Value = model.BusinessSubject;
                        break;
                    case "RequiredPeriod":
                        ((TextField)field).Value = model.Period;
                        break;
                    case "RequestDescription":
                        ((TextField)field).Value = model.Description;
                        break;
                    case "Status":
                        ((EnumerationField)field).Value = "Nový";
                        break;
                    case "ResourceRequestEnumeration":

                        IList<int> Ids = new List<int>();
                        if (model.AddedResources == null)
                            model.AddedResources = new List<ResourceViewModel>();
                        foreach (var resource in model.AddedResources)
                        {
                            var request = contentManager.New("ResourceRequestEnumeration");
                            contentManager.Create(request);

                            var requestItem = (dynamic)request;
                            var requestPart = (ContentPart)requestItem.ResourceRequestEnumeration;
                            var requestField = requestPart.Fields.FirstOrDefault(x => x.Name == "Resource");

                            ((SmartContentPickerField)requestField).Ids = new[] { resource.Id };
                            requestField = requestPart.Fields.FirstOrDefault(x => x.Name == "Count");
                            ((NumericField)requestField).Value = resource.Count;
                            Ids.Add(request.Id);

                            contentManager.Publish(request);
                            _indexingTasks.CreateUpdateIndexTask(request);

                        }
                        ((ContentPickerField)field).Ids = Ids.ToArray();

                        break;
                }
            }
        }

        private ContentItem GetResource(int id)
        {
            return contentManager.Query().ForType("Resource").List().FirstOrDefault(x => x.Id == id);
        }

        #region Get drodownlist iems

        private IList<SelectListItem> GetResourceTypes()
        {
            var resourceTypes = contentManager.Query().ForType("ResourceType").List().Where(x => x.As<LocalizationPart>().Culture.Culture == actualLanguage);
            IEnumerable<SelectListItem> list = resourceTypes.Select(x =>
                new SelectListItem()
                {
                    Text = x.As<Orchard.Core.Title.Models.TitlePart>().Title,
                    Value = x.Id.ToString()
                });
            return list.ToList();
        }

        private IList<SelectListItem> GetResourceNames(int typeId)
        {
            //var list = new List<SelectListItem>();
            //switch (typeId)
            //{
            //    case "0":
            //        list.Add(new SelectListItem { Text = "Laboratorny pracovnik", Value = "0" });
            //        break;
            //    case "1":
            //        list.Add(new SelectListItem { Text = "Spektrofotometer", Value = "0" });
            //        break;
            //}
            var resources = contentManager.Query().ForType("Resource").List();

            var list = new List<SelectListItem>();
            foreach (var contentItem in resources)
            {
                //todo: kontrolovat jazyk
                var item = (dynamic)contentItem;
                int[] Ids = item.Resource.ResourceType.Ids;

                if (Ids.Contains(typeId))
                {
                    list.Add(new SelectListItem
                    {
                        Text = contentItem.As<Orchard.Core.Title.Models.TitlePart>().Title,
                        Value = contentItem.Id.ToString()
                    });
                }
            }
            return list;
        }

        #endregion

        private static string ExecuteValidateRequest(string privateKey, string remoteip, string challenge, string response)
        {
            WebRequest request = WebRequest.Create(ReCaptchaUrl + "/verify");
            request.Method = "POST";
            request.Timeout = 5000; //milliseconds
            request.ContentType = "application/x-www-form-urlencoded";

            var postData = String.Format(CultureInfo.InvariantCulture,
                "privatekey={0}&remoteip={1}&challenge={2}&response={3}",
                privateKey,
                remoteip,
                challenge,
                response
            );

            byte[] content = Encoding.UTF8.GetBytes(postData);
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(content, 0, content.Length);
            }
            using (WebResponse webResponse = request.GetResponse())
            {
                using (var reader = new StreamReader(webResponse.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        internal static bool HandleValidateResponse(HttpContextBase context, string response)
        {
            if (!String.IsNullOrEmpty(response))
            {
                string[] results = response.Split('\n');
                if (results.Length > 0)
                {
                    bool rval = Convert.ToBoolean(results[0], CultureInfo.InvariantCulture);
                    return rval;
                }
            }
            return false;
        }
    }
}