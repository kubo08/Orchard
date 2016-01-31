using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Softea.DirectoryServices.Models
{
    public struct LdapServer
    {
        public const string HostDisplayName = "Host";
        public const string PortDisplayName = "Port";
        public const string UseSslDisplayName = "Use SSL";
        public const string UseUdpDisplayName = "Protocol";

        public LdapServer(string value)
            : this()
        {
            if (string.IsNullOrEmpty(value))
                return;

            Uri uri;
            if (Uri.TryCreate(value, UriKind.Absolute, out uri))
            {
                Host = uri.Host;
                Port = (ushort)uri.Port;
                UseSsl = uri.Scheme.EndsWith("+ssl");
                UseUdp = uri.Scheme.StartsWith("udp");
            }
        }

        [DisplayName(HostDisplayName)]
        [Required, StringLength(255), RegularExpression(@"(^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$)|(^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\-]*[A-Za-z0-9])$)", ErrorMessage = "The field {0} must contain a valid host name.")]
        public string Host { get; set; }

        [DisplayName(PortDisplayName)]
        [Required, Range(typeof(ushort), "1", "65535")]
        public ushort? Port { get; set; }

        [DisplayName(UseSslDisplayName)]
        [Required]
        public bool UseSsl { get; set; }

        [DisplayName(UseUdpDisplayName)]
        [Required]
        public bool UseUdp { get; set; }

        public override string ToString()
        {
            return
                !string.IsNullOrEmpty(Host) ?
                string.Format("{0}{1}://{2}:{3}",
                    !UseUdp ? "tcp" : "udp",
                    !UseSsl ? string.Empty : "+ssl",
                    Host,
                    Port) :
                null;
        }
    }
}