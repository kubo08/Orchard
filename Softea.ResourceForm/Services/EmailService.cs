using System;
using System.Collections.Generic;
using System.Linq;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Messaging.Services;
using Softea.ResourceForm.Models;

namespace Softea.ResourceForm.Services
{
    public interface IEmailService : IDependency
    {
        void Send(string EmailBody, string EmailSubject, string EmailRecipients);
    }
    public class EmailService : IEmailService
    {
        private readonly IMessageService _messageService;
        private readonly IOrchardServices services;

        public EmailService(
            IMessageService messageService,
            IOrchardServices services)
        {
            this.services = services;
            _messageService = messageService; ;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        /// <summary>
        /// Sends the email with form details.
        /// </summary>
        /// <param name="EmailBody">The email body.</param>
        /// <param name="EmailSubject">The email subject.</param>
        /// <param name="EmailRecipients">The email recipients.</param>
        /// <exception cref="System.Exception">Nastala chyba pri posielani emailu</exception>
        public void Send(string EmailBody, string EmailSubject, string EmailRecipients)
        {
            var body = EmailBody;
            var subject = EmailSubject;
            var recipients = EmailRecipients;
            var item = services.ContentManager.Query<ResourceFormPart>("ResourceForm").List().FirstOrDefault();

            if (recipients == String.Empty)
            {
                recipients = item.MasterEmail;
            }
            else
            {
                recipients += ", " + item.MasterEmail;
            }
            var parameters = new Dictionary<string, object> {
                {"Subject", subject},
                {"Body", body},
                {"Recipients", recipients}
            };
            try
            {
                _messageService.Send("Email", parameters);
            }
            catch (Exception ex)
            {
                throw new Exception("Nastala chyba pri posielani emailu", ex);
            }
        }
    }
}