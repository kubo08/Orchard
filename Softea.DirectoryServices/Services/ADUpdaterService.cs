using System;
using System.Collections.Generic;
using System.Linq;
using Softea.DirectoryServices.ADWrapper;
using Orchard.ContentManagement;
using Orchard.Core.Common.Fields;
using Softea.DirectoryServices.Models;
using Orchard.Security;
using Orchard.Logging;
using Orchard.Localization;
using Orchard.Users.Services;
using Orchard.Localization.Services;
using Orchard.Localization.Models;
using Orchard.Caching;
using System.IO;
using Orchard;
using Orchard.MediaLibrary.Services;
using Orchard.MediaLibrary.Fields;
using Orchard.FileSystems.Media;

namespace Softea.DirectoryServices.Services
{
    public class ADUpdaterService : IADUpdaterService
    {
        private IContentManager _contentManager;
        readonly IOrchardServices orchardServices;
        readonly Lazy<IMembershipService> originalMembershipService;
        readonly ICultureManager cultureManager;
        readonly ICacheManager cacheManager;
        readonly ILdapServiceFactory ldapServiceFactory;
        readonly IMediaLibraryService mediaLibraryService;
        readonly IStorageProvider storageProvider;
        //private IRepository<EmployeePartRecord> _employeeRepository;

        public ADUpdaterService(IContentManager contentManager,
            IOrchardServices orchardServices,
            ICultureManager cultureManager,
            ICacheManager cacheManager,
            ILdapServiceFactory ldapServiceFactory,
            IMediaLibraryService mediaLibraryService,
            IStorageProvider storageProvider)
        {
            _contentManager = contentManager;
            this.orchardServices = orchardServices;
            originalMembershipService = new Lazy<IMembershipService>(() =>
                this.orchardServices.WorkContext.Resolve<IEnumerable<IMembershipService>>().Single(x => x is MembershipService));
            this.cultureManager = cultureManager;
            this.cacheManager = cacheManager;
            this.ldapServiceFactory = ldapServiceFactory;
            this.mediaLibraryService = mediaLibraryService;
            this.storageProvider = storageProvider;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public int RunJob(IList<ILdapDirectory> directories)
        {

            var totalUsers = 0;

            foreach (var directory in directories)
            {

                var service = new ADServices(String.Format("LDAP://{0}/{1}", directory.Server.Host, directory.BaseDn), String.Format("LDAP://{0}", directory.Server.Host));

                var ldapService = ldapServiceFactory.For(directory);
                var users = service.GetUsers(directory);
                totalUsers += users.Count;
                RunJobWithUser(users, ldapService);

            }
            return totalUsers;
        }

        private void RunJobWithUser(IList<User> users, ILdapService ldapService)
        {
            var employeesCT = _contentManager.Query().ForType(Constants.CONTENTTYPENAME).List();
            var languages = cultureManager.ListCultures().ToList();

            foreach (var user in users)
            {
                var userId = Convert.ToBase64String(user.UserID);
                //get all employees with specified ID
                var orchardUsers = employeesCT.Where(contentItem => contentItem.Parts.Any(part => part.Fields.Any(f => (f.Name.ToLower() == Constants.EMPLOYEEIDFIELD.ToLower() && ((TextField)f).Value == userId)))).ToList();

                //get "main" employee - in default language
                var orchardUser = orchardUsers.FirstOrDefault(u => u.As<LocalizationPart>().Culture == null || u.As<LocalizationPart>().Culture.Culture == Constants.DEFAULTADLANGUAGE);
                //todo: oddelit jednotlive vytarania, kontrolovat ci uz neexistuje user, orchardUser je employee
                if (orchardUser == null)
                {
                    try
                    {
                        orchardUser = CreateUser(ldapService, user, null, Constants.DEFAULTADLANGUAGE);
                        if (orchardUsers.Count > 0)
                        {
                            SetParent(orchardUser, orchardUsers);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Nastala chyba pri vytvarani pouzivatela {0} v orcharde.", user.LoginName), ex);
                    }
                }
                else
                {
                    try
                    {
                        UpdateUser(orchardUser, user, orchardUsers);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Nastala chyba pri aktualizacii udajov pouzivatela: {0}", user.LoginName), ex);
                    }
                }

                //check and create if exist employees im other language variations

                try
                {
                    CheckAllUserLanguageVariantions(orchardUser, orchardUsers, languages, user);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Nastala chyba pri kontrole jazykovych variantov: {0}", user.LoginName), ex);
                }
            }
        }

        /// <summary>
        /// Sets the parent to child content items.
        /// </summary>
        /// <param name="parent">Content item with default language</param>
        /// <param name="childs">Content items with other language variations</param>
        private void SetParent(ContentItem parent, IList<ContentItem> childs)
        {
            foreach (var item in childs)
            {
                SetCulture(item, item.As<LocalizationPart>().Culture.Culture, parent);
            }
        }


        /// <summary>
        /// Checks all user language variantions.
        /// </summary>
        /// <param name="parentContentItem">The parent content item.</param>
        /// <param name="orchardUsers">The orchard users.</param>
        /// <param name="languages">The languages.</param>
        /// <param name="ADuser">a duser.</param>
        /// <exception cref="System.Exception">
        /// </exception>
        private void CheckAllUserLanguageVariantions(ContentItem parentContentItem, List<ContentItem> orchardUsers, List<string> languages, User ADuser)
        {
            if (orchardUsers.Count == languages.Count)
                return;
            foreach (var language in languages)
            {
                if (language == parentContentItem.As<LocalizationPart>().Culture.Culture)
                    continue;
                var user = orchardUsers.FirstOrDefault(u => u.As<LocalizationPart>().Culture.Culture == language);
                if (user != null)
                {
                    try
                    {
                        UpdateUser(parentContentItem, ADuser, orchardUsers);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Nastala chyba pri aktualizacii udajov pouzivatela: {0}, users: {1}", ADuser.LoginName, orchardUsers.Count), ex);
                    }
                    continue;
                }
                try
                {
                    CreateEmployee(ADuser, parentContentItem, language);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Chyba pri vytvarani zamestnanca, pocet orchard users: {0}", orchardUsers.Count), ex);
                }
            }

        }

        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Creates the user.
        /// </summary>
        /// <param name="ldapService">The LDAP service.</param>
        /// <param name="user">The user.</param>
        /// <param name="parentContentItem">The parent content item.</param>
        /// <param name="language">The language.</param>
        /// <returns></returns>
        private ContentItem CreateUser(ILdapService ldapService, User user, ContentItem parentContentItem, string language)
        {
            var orchardUser = CreateEmployee(user, parentContentItem, language);
            if (orchardUser == null)
            {
                Logger.Error("An error occured during creating employee: {0}, in DirectoryServices module!", user.Surname);
            }
            if (originalMembershipService.Value.GetUser(user.LoginName) == null)
            {
                if (!CreateNewUser(ldapService, user))
                {
                    Logger.Error("An error occurred during creating user: {0}, in ADModule.", user.Surname);
                }
            }

            return orchardUser;
        }

        /// <summary>
        /// Creates the employee.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="parentContentItem">The parent content item.</param>
        /// <param name="language">The language.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        /// </exception>
        private ContentItem CreateEmployee(User user, ContentItem parentContentItem, string language)
        {
            ContentItem orchardUser;
            try
            {
                orchardUser = _contentManager.New(Constants.CONTENTTYPENAME);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Nastala chyba pri vytvarani noveho employee content item pre usera: {0}", user.LoginName), ex);
            }
            orchardUser.As<Orchard.Core.Title.Models.TitlePart>().Title = user.FullName;
            try
            {
                orchardServices.ContentManager.Create(orchardUser, VersionOptions.Draft);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Nastala chyba pri volani create employee content item pre usera: {0}", user.LoginName), ex);
            }
            SetCulture(orchardUser, language, parentContentItem);

            try
            {
                UpdateUser(orchardUser, user);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Nastala chyba pri aktualizacii udajov pouzivatela: {0}", user.LoginName), ex);
            }

            return orchardUser;
        }

        /// <summary>
        /// Creates the new user.
        /// </summary>
        /// <param name="ldapService">The LDAP service.</param>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private bool CreateNewUser(ILdapService ldapService, User item)
        {
            var pass = RandomString(10);
            var user = originalMembershipService.Value.CreateUser(new CreateUserParams(item.LoginName, pass, null, null, null, true));
            if (user != null)
            {
                user.As<UserLdapPart>().LdapDirectoryId = ldapService.Directory.Id;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates the user.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <param name="orchardUsers">The orchard users.</param>
        /// <returns></returns>
        private bool UpdateUser(ContentItem item, User user, List<ContentItem> orchardUsers = null)
        {
            bool isUpdated = false;
            try
            {
                var title = item.As<Orchard.Core.Title.Models.TitlePart>();
                if (title.Title != user.FullName)
                {
                    isUpdated |= true;
                    title.Title = user.FullName;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Nastala chyba pri update title!", ex);
            }

            if (item.As<LocalizationPart>().Culture == null)
            {
                try
                {
                    SetCulture(item, Constants.DEFAULTADLANGUAGE, null);
                }
                catch (Exception ex)
                {
                    throw new Exception("Nastala chyba pri update culture!", ex);
                }
            }

            //XmlSerializer xs = new XmlSerializer(typeof(User));

            //using (var sw = new StringWriter())
            //{
            //    using (var xw = XmlWriter.Create(sw))
            //    {
            //        xs.Serialize(xw, user);
            //    }

            //    throw new Exception(String.Format("User data: {0}", sw.ToString()));
            //}
            ContentPart employee;
            try
            {
                var contentItem = ((dynamic)item);
                employee = (ContentPart)contentItem.Employee;
            }
            catch (Exception ex)
            {
                throw new Exception("Nastala chyba pri vytahovani employee z content item!", ex);
            }
            if (employee != null)
            {
                foreach (var field in employee.Fields)
                {
                    switch (field.Name.ToLower().Trim())
                    {
                        case Constants.DEPARTMENT:
                            try
                            {
                                isUpdated |= CheckValue(field, user.Department);
                            }
                            catch (Exception ex)
                            { throw new Exception("Nastala chyba pri update department!", ex); }
                            break;
                        case Constants.OFFICE:
                            try
                            {
                                isUpdated |= CheckValue(field, user.Office);
                            }
                            catch (Exception ex)
                            { throw new Exception("Nastala chyba pri update office!", ex); }
                            break;
                        case Constants.JOBTITLE:
                            try
                            {
                                isUpdated |= CheckValue(field, user.JobTitle);
                            }
                            catch (Exception ex)
                            { throw new Exception("Nastala chyba pri update jobtitle!", ex); }
                            break;
                        case Constants.PHONE:
                            try
                            {
                                isUpdated |= CheckValue(field, user.PhoneNo);
                            }
                            catch (Exception ex)
                            { throw new Exception("Nastala chyba pri update phone!", ex); }
                            break;
                        case Constants.PRIMARYEMAIL:
                            try
                            {
                                isUpdated |= CheckValue(field, user.Email);
                            }
                            catch (Exception ex)
                            { throw new Exception("Nastala chyba pri update primaryemail!", ex); }
                            break;
                        case Constants.SECONDARYEMAIL:
                            try
                            {
                                isUpdated |= CheckValue(field, user.SecondaryEmail);
                            }
                            catch (Exception ex)
                            { throw new Exception("Nastala chyba pri update secondaryemail!", ex); }
                            break;
                        case Constants.USERID:
                            try
                            {
                                //len koli novym pouzivatelom, vtedy je povodny field null
                                isUpdated |= CheckValue(field, Convert.ToBase64String(user.UserID));
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Nastala chyba pri update userid!", ex);
                            }
                            break;
                        case Constants.PHOTO:
                            try
                            {
                                if (user.Photo != null)
                                {
                                    var filename = String.Format("{0}.{1}", user.LoginName, Constants.PHOTOEXTENSION);

                                    var imageField = ((MediaLibraryPickerField)field);

                                    var anyMediaPart = !imageField.Ids.Any();

                                    if (anyMediaPart || !CompareFileContent(imageField.MediaParts.FirstOrDefault().FolderPath, imageField.MediaParts.FirstOrDefault().FileName, user.Photo))
                                    {

                                        var mediaFile = mediaLibraryService.ImportMedia(new MemoryStream(user.Photo), "Zamestnanci", filename);
                                        try
                                        {
                                            orchardServices.ContentManager.Create(mediaFile);
                                        }
                                        catch (Exception ex)
                                        {
                                            throw new Exception(String.Format("Nastala chyba pri pridavani fotky pre pouzivatela:{0}", user.LoginName), ex);
                                        }
                                        imageField.Ids = new[] { mediaFile.Id };
                                        isUpdated |= true;
                                        SetPhotos(orchardUsers, mediaFile.Id);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Nastala chyba pri updatovani fotky!", ex);
                            }
                            break;
                    }
                }
            }

            if (isUpdated)
                try
                {
                    orchardServices.ContentManager.Publish(item);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Nastala chyba pri publish usera: {0}, content item: {1}", user.LoginName, item.Parts.Count()), ex);
                }
            return isUpdated;
        }

        /// <summary>
        /// Sets the photos.
        /// </summary>
        /// <param name="users">The users.</param>
        /// <param name="photoId">The photo identifier.</param>
        private void SetPhotos(List<ContentItem> users, int photoId)
        {
            if (users == null)
                return;

            foreach (var user in users)
            {
                var contentItem = ((dynamic)user);
                var employee = (ContentPart)contentItem.Employee;
                var field = (MediaLibraryPickerField)employee.Fields.FirstOrDefault(f => f.Name.ToLower() == Constants.PHOTO);
                field.Ids = new[] { photoId };
            }
        }

        /// <summary>
        /// Sets the culture.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="CultureString">The culture string.</param>
        /// <param name="parentContentItem">The parent content item.</param>
        private void SetCulture(ContentItem item, string CultureString, ContentItem parentContentItem)
        {
            var languagePart = item.As<LocalizationPart>();
            languagePart.Culture = cultureManager.GetCultureByName(CultureString);
            if (parentContentItem != null)
            {
                languagePart.MasterContentItem = parentContentItem;
            }
        }

        private bool CheckValue(ContentField field, string value)
        {
            if (((TextField)field).Value != value)
            {
                ((TextField)field).Value = value;
                return true;
            }
            return false;
        }


        private IStorageFile TryGetFile(string relativePath, string filename)
        {
            var path = storageProvider.Combine(relativePath, filename);
            if (!storageProvider.FileExists(path))
                return null;

            var file = storageProvider.GetFile(path);
            return file;
        }

        /// <summary>
        /// Compares the content of the photo with old photo.
        /// </summary>
        /// <param name="relativePath">The relative path.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        private bool CompareFileContent(string relativePath, string filename, byte[] content)
        {
            var file = TryGetFile(relativePath, filename);
            return CompareFileContent(file, content);
        }

        /// <summary>
        /// Compares the content of the photo with old photo.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        private bool CompareFileContent(IStorageFile file, byte[] content)
        {
            if (file == null)
                return false;

            // The byte[] to save the data in
            var bytes = new byte[file.GetSize()];

            // Load a filestream and put its content into the byte[]
            using (Stream fs = file.OpenRead())
            {
                fs.Read(bytes, 0, bytes.Length);
            }

            return content.SequenceEqual(bytes);
        }
    }
}