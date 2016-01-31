using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Orchard;
using Orchard.Localization;
using Orchard.ContentManagement;
using Orchard.Users.Models;
using Orchard.Security;
using Orchard.UI.Notify;
using Orchard.Themes;
using Orchard.Core.Settings.Models;
using Orchard.Utility.Extensions;
using Orchard.Mvc.Extensions;
using Orchard.Users.Services;
using System.Diagnostics.CodeAnalysis;
using Orchard.Users.Events;
using Softea.DirectoryServices.Models;
using Orchard.Logging;

namespace Softea.DirectoryServices.Controllers
{
    [HandleError, Themed]
    [ActionOverride("Orchard.Users", "Account")]
    public class OrchardUsersAccountOverrideController : Controller
    {
        readonly IMembershipService membershipService;
        readonly IUserService userService;
        readonly IEnumerable<IUserEventHandler> userEventHandlers;

        public OrchardUsersAccountOverrideController(
            IOrchardServices services,
            IMembershipService membershipService,
            IUserService userService,
            IEnumerable<IUserEventHandler> userEventHandlers)
        {
            Services = services;
            this.membershipService = membershipService;
            this.userService = userService;
            this.userEventHandlers = userEventHandlers;

            T = NullLocalizer.Instance;
        }

        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        // MIGRATION copied from Orchard.Users.Controllers.AccountController
        [AlwaysAccessible]
        public ActionResult RequestLostPassword()
        {
            // ensure users can request lost password
            var registrationSettings = Services.WorkContext.CurrentSite.As<RegistrationSettingsPart>();
            if (!registrationSettings.EnableLostPassword)
            {
                return HttpNotFound();
            }

            return View();
        }

        // MIGRATION copied from Orchard.Users.Controllers.AccountController
        [HttpPost]
        [AlwaysAccessible]
        public ActionResult RequestLostPassword(string username)
        {
            // ensure users can request lost password
            var registrationSettings = Services.WorkContext.CurrentSite.As<RegistrationSettingsPart>();
            if (!registrationSettings.EnableLostPassword)
            {
                return HttpNotFound();
            }

            if (String.IsNullOrWhiteSpace(username))
            {
                Services.Notifier.Error(T("Invalid username or E-mail"));
                return View();
            }

            var siteUrl = Services.WorkContext.CurrentSite.As<SiteSettingsPart>().BaseUrl;
            if (String.IsNullOrWhiteSpace(siteUrl))
            {
                siteUrl = HttpContext.Request.ToRootUrlString();
            }

            var lowerName = username.ToLowerInvariant();
            var user = Services.ContentManager
                .Query<UserPart, UserPartRecord>("User")
                .Where(u => u.NormalizedUserName == lowerName || u.Email == lowerName)
                .List().FirstOrDefault();

            if (user != null && user.As<UserLdapPart>().LdapDirectoryId == null)
                userService.SendLostPasswordEmail(username, nonce => Url.MakeAbsolute(Url.Action("LostPassword", "Account", new { Area = "Orchard.Users", nonce = nonce }), siteUrl));

            // always displaying this text, even if there is no user with the specified name or password reset cannot be requested by the user,
            // so that we won't give out any information about existing users
            Services.Notifier.Information(T("Check your e-mail for the confirmation link."));

            return RedirectToAction("LogOn");
        }

        // MIGRATION copied from Orchard.Users.Controllers.AccountController
        [Authorize]
        [AlwaysAccessible]
        public ActionResult ChangePassword()
        {
            ViewData["PasswordLength"] = MinPasswordLength;

            return View();
        }

        // MIGRATION copied from Orchard.Users.Controllers.AccountController
        [Authorize]
        [HttpPost]
        [AlwaysAccessible]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Exceptions result in password not being changed.")]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            ViewData["PasswordLength"] = MinPasswordLength;

            if (!ValidateChangePassword(currentPassword, newPassword, confirmPassword))
            {
                return View();
            }

            var user = Services.WorkContext.CurrentUser;
            var userLdap = user.As<UserLdapPart>();
            // passing current password to membershipService.SetPassword() in the UserLdapPart because IMembershipService interface cannot be changed
            userLdap.CurrentPassword = currentPassword;

            try
            {
                membershipService.SetPassword(user, newPassword);

                foreach (var userEventHandler in userEventHandlers)
                    userEventHandler.ChangedPassword(user);

                return RedirectToAction("ChangePasswordSuccess");
            }
            catch
            {
                Services.TransactionManager.Cancel();
                ModelState.AddModelError("_FORM", T("The current password is incorrect or the new password is invalid."));
                return ChangePassword();
            }
            finally
            {
                userLdap.CurrentPassword = null;
            }
        }

        // MIGRATION copied from Orchard.Users.Controllers.AccountController
        int MinPasswordLength
        {
            get
            {
                return membershipService.GetSettings().MinRequiredPasswordLength;
            }
        }

        // MIGRATION copied from Orchard.Users.Controllers.AccountController
        private bool ValidateChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (String.IsNullOrEmpty(currentPassword))
            {
                ModelState.AddModelError("currentPassword", T("You must specify a current password."));
            }
            if (newPassword == null || newPassword.Length < MinPasswordLength)
            {
                ModelState.AddModelError("newPassword", T("You must specify a new password of {0} or more characters.", MinPasswordLength));
            }

            if (!String.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                ModelState.AddModelError("_FORM", T("The new password and confirmation password do not match."));
            }

            return ModelState.IsValid;
        }

    }
}