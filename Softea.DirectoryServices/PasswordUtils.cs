using System;
using System.Text;
using Orchard.Security;

namespace Softea.DirectoryServices
{
    public static class PasswordUtils
    {
        public static string DecodePassword(string value, IEncryptionService encryptionService, Func<string> failureHandler)
        {
            try
            {
                return
                    string.IsNullOrWhiteSpace(value) ?
                    string.Empty :
                    Encoding.UTF8.GetString(encryptionService.Decode(Convert.FromBase64String(value)));
            }
            catch
            {
                return failureHandler();
            }
        }

        public static string EncodePassword(string value, IEncryptionService encryptionService)
        {
            return
                string.IsNullOrWhiteSpace(value) ?
                string.Empty :
                Convert.ToBase64String(encryptionService.Encode(Encoding.UTF8.GetBytes(value)));
        }
    }
}