using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;
using Softea.DirectoryServices.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Security;
using Orchard.Users.Models;
using Orchard.Users.Services;
using System.Web.Helpers;
using Orchard.Environment.Configuration;
using Orchard.Data;

namespace Softea.DirectoryServices.Services
{
    public class LdapMembershipService : IMembershipService
    {
        readonly IOrchardServices orchardServices;
        readonly IEncryptionService encryptionService;
        readonly ILdapServiceFactory ldapServiceFactory;
        readonly ILdapDirectoryCache ldapDirectoryCache;
        readonly Lazy<IUserService> userService;
        readonly Lazy<IMembershipService> originalMembershipService;
        readonly IRepository<UserLdapPartRecord> userLdapRepository;
        private readonly IAppConfigurationAccessor _appConfigurationAccessor;

        private const string PBKDF2 = "PBKDF2";
        private const string DefaultHashAlgorithm = "PBKDF2";

        public LdapMembershipService(
            IOrchardServices orchardServices,
            IEncryptionService encryptionService,
            ILdapServiceFactory ldapServiceFactory,
            ILdapDirectoryCache ldapDirectoryCache,
            Lazy<IUserService> userService,
            IAppConfigurationAccessor appConfigurationAccessor)
        {
            this.orchardServices = orchardServices;
            this.encryptionService = encryptionService;
            this.ldapServiceFactory = ldapServiceFactory;
            this.ldapDirectoryCache = ldapDirectoryCache;
            this.userService = userService;
            this._appConfigurationAccessor = appConfigurationAccessor;
            originalMembershipService = new Lazy<IMembershipService>(() =>
                this.orchardServices.WorkContext.Resolve<IEnumerable<IMembershipService>>().Single(x => !(x is LdapMembershipService)));

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public MembershipSettings GetSettings()
        {
            var settings = new MembershipSettings();
            // accepting defaults
            return settings;
        }

        public IUser CreateUser(CreateUserParams createUserParams)
        {
            return originalMembershipService.Value.CreateUser(createUserParams);
        }

        public IUser GetUser(string username)
        {
            return originalMembershipService.Value.GetUser(username);
        }

        // MIGRATION copied from Orchard.Users.Services.MembershipService
        public IUser ValidateUser(string userNameOrEmail, string password)
        {
            var user = FetchUser(userNameOrEmail);


            if (user.RegistrationStatus != UserStatus.Approved ||
                user.EmailStatus != UserStatus.Approved)
                return null;

            var userLdap = user.As<UserLdapPart>();
            if (userLdap != null && userLdap.LdapDirectoryId.HasValue)
            {
                // authenticating with LDAP directory
                var directory = ldapDirectoryCache.GetDirectory(userLdap.LdapDirectoryId.Value);
                bool authenticated = false;
                if (directory == null)
                {
                    var directories = ldapDirectoryCache.GetDirectories();

                    foreach (var ldapDirectory in directories)
                    {
                        if (AuthenticateWith(ldapServiceFactory.For(ldapDirectory), user.UserName, password))
                        {
                            authenticated = true;
                            break;
                        }
                    }
                }
                else
                {
                    if (AuthenticateWith(ldapServiceFactory.For(directory), user.UserName, password))
                    {
                        authenticated = true;
                    }
                }

                if (!authenticated)
                {
                    return null;
                }
            }
            else
            {
                // authenticating with the original method
                if (!ValidatePassword(user.As<UserPart>(), password))
                    return null;
            }

            return user;
        }

        UserPart FetchUser(string userName, bool acceptEmailToo = true)
        {
            var lowerName = userName == null ? "" : userName.ToLowerInvariant();

            var user = orchardServices.ContentManager.Query<UserPart, UserPartRecord>().Where(u => u.NormalizedUserName == lowerName).List().FirstOrDefault();

            if (acceptEmailToo && user == null)
                user = orchardServices.ContentManager.Query<UserPart, UserPartRecord>().Where(u => u.Email == lowerName).List().FirstOrDefault();

            return user;
        }

        bool AuthenticateWith(ILdapService ldapService, string userName, string password)
        {
            try
            {
                return ldapService.Authenticate(userName, password);
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred during authentication. Details: {0}", ex);
                return false;
            }
        }

        public void SetPassword(IUser user, string password)
        {
            var userLdap = user.As<UserLdapPart>();
            if (userLdap.LdapDirectoryId == null &&
                ValidateUser(user.UserName, userLdap.CurrentPassword) == null)
                throw new ApplicationException("Invalid username or password.");

            originalMembershipService.Value.SetPassword(user, password);

            if (userLdap.LdapDirectoryId != null)
            {
                var directory = ldapDirectoryCache.GetDirectory(userLdap.LdapDirectoryId.Value);
                var ldapService = ldapServiceFactory.For(directory);

                bool passwordChanged;
                try
                {
                    passwordChanged = ldapService.ChangePassword(user.UserName, userLdap.CurrentPassword, password);
                }
                catch (Exception ex)
                {
                    if (ex is ApplicationException || ex is System.DirectoryServices.Protocols.DirectoryException)
                        Logger.Error("An error occurred when changing password of user {0}. Details: {1}", user.UserName, ex);
                    throw new ApplicationException("Password could not be changed.", ex);
                }

                if (!passwordChanged)
                    throw new ApplicationException("Password could not be changed.");
            }
        }

        // MIGRATION copied from Orchard.Users.Services.MembershipService
        bool ValidatePassword(UserPart part, string password)
        {
            // Note - the password format stored with the record is used
            // otherwise changing the password format on the site would invalidate
            // all logins
            switch (part.PasswordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    return ValidatePasswordClear(part, password);
                case MembershipPasswordFormat.Hashed:
                    return ValidatePasswordHashed(part, password);
                case MembershipPasswordFormat.Encrypted:
                    return ValidatePasswordEncrypted(part, password);
                default:
                    throw new ApplicationException("Unexpected password format value");
            }
        }

        // MIGRATION copied from Orchard.Users.Services.MembershipService
        private static bool ValidatePasswordClear(UserPart partRecord, string password)
        {
            return partRecord.Password == password;
        }

        #region Orchard 1.9.2

        private bool ValidatePasswordHashed(UserPart userPart, string password)
        {
            var saltBytes = Convert.FromBase64String(userPart.PasswordSalt);

            bool isValid;
            if (userPart.HashAlgorithm == PBKDF2)
            {
                // We can't reuse ComputeHashBase64 as the internally generated salt repeated calls to Crypto.HashPassword() return different results.
                isValid = Crypto.VerifyHashedPassword(userPart.Password, Encoding.Unicode.GetString(CombineSaltAndPassword(saltBytes, password)));
            }
            else
            {
                isValid = SecureStringEquality(userPart.Password, ComputeHashBase64(userPart.HashAlgorithm, saltBytes, password));
            }

            // Migrating older password hashes to Default algorithm if necessary and enabled.
            if (isValid && userPart.HashAlgorithm != DefaultHashAlgorithm)
            {
                var keepOldConfiguration = _appConfigurationAccessor.GetConfiguration("Orchard.Users.KeepOldPasswordHash");
                if (String.IsNullOrEmpty(keepOldConfiguration) || keepOldConfiguration.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    userPart.HashAlgorithm = DefaultHashAlgorithm;
                    userPart.Password = ComputeHashBase64(userPart.HashAlgorithm, saltBytes, password);
                }
            }

            return isValid;
        }

        private static byte[] CombineSaltAndPassword(byte[] saltBytes, string password)
        {
            var passwordBytes = Encoding.Unicode.GetBytes(password);
            return saltBytes.Concat(passwordBytes).ToArray();
        }

        /// <summary>
        /// Compares two strings without giving hint about the time it takes to do so.
        /// </summary>
        /// <param name="a">The first string to compare.</param>
        /// <param name="b">The second string to compare.</param>
        /// <returns><c>true</c> if both strings are equal, <c>false</c>.</returns>
        private bool SecureStringEquality(string a, string b)
        {
            if (a == null || b == null || (a.Length != b.Length))
            {
                return false;
            }

            var aBytes = Encoding.Unicode.GetBytes(a);
            var bBytes = Encoding.Unicode.GetBytes(b);

            var bytesAreEqual = true;
            for (int i = 0; i < a.Length; i++)
            {
                bytesAreEqual &= (aBytes[i] == bBytes[i]);
            }

            return bytesAreEqual;
        }

        private static string ComputeHashBase64(string hashAlgorithmName, byte[] saltBytes, string password)
        {
            var combinedBytes = CombineSaltAndPassword(saltBytes, password);

            // Extending HashAlgorithm would be too complicated: http://stackoverflow.com/questions/6460711/adding-a-custom-hashalgorithmtype-in-c-sharp-asp-net?lq=1
            if (hashAlgorithmName == PBKDF2)
            {
                // HashPassword() already returns a base64 string.
                return Crypto.HashPassword(Encoding.Unicode.GetString(combinedBytes));
            }
            else
            {
                using (var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmName))
                {
                    return Convert.ToBase64String(hashAlgorithm.ComputeHash(combinedBytes));
                }
            }
        }

        #endregion

        // MIGRATION copied from Orchard.Users.Services.MembershipService
        private bool ValidatePasswordEncrypted(UserPart partRecord, string password)
        {
            return String.Equals(password, Encoding.UTF8.GetString(
                encryptionService.Decode(Convert.FromBase64String(partRecord.Password))), StringComparison.Ordinal);
        }
    }
}