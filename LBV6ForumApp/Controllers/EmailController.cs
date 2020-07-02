using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using LBV6Library;
using LBV6Library.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Threading.Tasks;

namespace LBV6ForumApp.Controllers
{
    public class EmailController
    {
        public async Task SendTemplatedEmailAsync(EmailTemplate emailTemplate, string recipient, object[] parameters, string subject = null)
        {
            await SendTemplatedEmailAsync(emailTemplate, new List<string> { recipient }, parameters, subject);
        }

        public async Task SendTemplatedEmailAsync(EmailTemplate emailTemplate, List<string> recipients, object[] parameters, string subject = null)
        {
            if (!bool.Parse(ConfigurationManager.AppSettings["LB.EmailDeliveryEnabled"]))
                return;

            var htmlMaster = Templates._Master;
            string htmlTemplate;
            string textTemplate;
                
            switch (emailTemplate)
            {
                case EmailTemplate.PostModerationNotification:
                    htmlTemplate = Templates.PostModeratedNotificationHtml;
                    textTemplate = Templates.PostModeratedNotificationText;
                    subject = "Your post has been moderated - " + (DateTime.UtcNow.Ticks / 10000000);
                    break;
                case EmailTemplate.RegistrationEmailConfirmationRequired:
                    htmlTemplate = Templates.RegistrationEmailConfirmationRequiredHtml;
                    textTemplate = Templates.RegistrationEmailConfirmationRequiredText;
                    subject = "Thanks for joining londonbikers.com!";
                    break;
                case EmailTemplate.EmailConfirmationRequired:
                    htmlTemplate = Templates.EmailConfirmationRequiredHtml;
                    textTemplate = Templates.EmailConfirmationRequiredText;
                    subject = "Please confirm your e-mail address";
                    break;
                case EmailTemplate.Welcome:
                    htmlTemplate = Templates.WelcomeHtml;
                    textTemplate = Templates.WelcomeText;
                    subject = "Welcome to londonbikers.com!";
                    break;
                case EmailTemplate.ResetPasswordLink:
                    htmlTemplate = Templates.PasswordResetLinkHtml;
                    textTemplate = Templates.PasswordResetLinkText;
                    subject = "Password reset information";
                    break;
                case EmailTemplate.NewTopic:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicHtml;
                    textTemplate = Templates.NewTopicText;
                    break;
                case EmailTemplate.NewTopicWithPhoto:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicWithPhotoHtml;
                    textTemplate = Templates.NewTopicWithPhotoText;
                    break;
                case EmailTemplate.NewTopicWithPhotos:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicWithPhotosHtml;
                    textTemplate = Templates.NewTopicWithPhotosText;
                    break;
                case EmailTemplate.NewTopicWithRollups:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicWithRollupsHtml;
                    textTemplate = Templates.NewTopicWithRollupsText;
                    break;
                case EmailTemplate.NewTopicWithPhotoWithRollups:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicWithPhotoWithRollupsHtml;
                    textTemplate = Templates.NewTopicWithPhotoWithRollupsText;
                    break;
                case EmailTemplate.NewTopicWithPhotosWithRollups:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicWithPhotosWithRollupsHtml;
                    textTemplate = Templates.NewTopicWithPhotosWithRollupsText;
                    break;
                case EmailTemplate.NewTopicWithPhotoWithoutContent:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicWithPhotoWithoutContentHtml;
                    textTemplate = Templates.NewTopicWithPhotoWithoutContentText;
                    break;
                case EmailTemplate.NewTopicWithPhotoWithoutContentWithRollups:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicWithPhotoWithoutContentWithRollupsHtml;
                    textTemplate = Templates.NewTopicWithPhotoWithoutContentWithRollupsText;
                    break;
                case EmailTemplate.NewTopicWithPhotosWithoutContent:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicWithPhotosWithoutContentHtml;
                    textTemplate = Templates.NewTopicWithPhotosWithoutContentText;
                    break;
                case EmailTemplate.NewTopicWithPhotosWithoutContentWithRollups:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicWithPhotosWithoutContentWithRollupsHtml;
                    textTemplate = Templates.NewTopicWithPhotosWithoutContentWithRollupsText;
                    break;
                case EmailTemplate.NewTopicReply:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicReplyHtml;
                    textTemplate = Templates.NewTopicReplyText;
                    break;
                case EmailTemplate.NewTopicReplyWithPhoto:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicReplyWithPhotoHtml;
                    textTemplate = Templates.NewTopicReplyWithPhotoText;
                    break;
                case EmailTemplate.NewTopicReplyWithPhotos:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicReplyWithPhotosHtml;
                    textTemplate = Templates.NewTopicReplyWithPhotosText;
                    break;
                case EmailTemplate.NewTopicReplyWithRollups:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicReplyWithRollupsHtml;
                    textTemplate = Templates.NewTopicReplyWithRollupsText;
                    break;
                case EmailTemplate.NewTopicReplyWithPhotoWithRollups:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicReplyWithPhotoWithRollupsHtml;
                    textTemplate = Templates.NewTopicReplyWithPhotoWithRollupsText;
                    break;
                case EmailTemplate.NewTopicReplyWithPhotosWithRollups:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicReplyWithPhotosWithRollupsHtml;
                    textTemplate = Templates.NewTopicReplyWithPhotosWithRollupsText;
                    break;
                case EmailTemplate.NewTopicReplyWithPhotoWithoutContent:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicReplyWithPhotoWithoutContentHtml;
                    textTemplate = Templates.NewTopicReplyWithPhotoWithoutContentText;
                    break;
                case EmailTemplate.NewTopicReplyWithPhotoWithoutContentWithRollups:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicReplyWithPhotoWithoutContentWithRollupsHtml;
                    textTemplate = Templates.NewTopicReplyWithPhotoWithoutContentWithRollupsText;
                    break;
                case EmailTemplate.NewTopicReplyWithPhotosWithoutContent:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicReplyWithPhotosWithoutContentHtml;
                    textTemplate = Templates.NewTopicReplyWithPhotosWithoutContentText;
                    break;
                case EmailTemplate.NewTopicReplyWithPhotosWithoutContentWithRollups:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewTopicReplyWithPhotosWithoutContentWithRollupsHtml;
                    textTemplate = Templates.NewTopicReplyWithPhotosWithoutContentWithRollupsText;
                    break;
                case EmailTemplate.NewPrivateMessage:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlMaster = Templates._Master2;
                    htmlTemplate = Templates.NewPrivateMessageHtml;
                    textTemplate = Templates.NewPrivateMessageText;
                    break;
                case EmailTemplate.NewPrivateMessageWithRollups:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlMaster = Templates._Master2;
                    htmlTemplate = Templates.NewPrivateMessageWithRollupsHtml;
                    textTemplate = Templates.NewPrivateMessageWithRollupsText;
                    break;
                case EmailTemplate.NewPhotoComment:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewPhotoCommentHtml;
                    textTemplate = Templates.NewPhotoCommentText;
                    break;
                case EmailTemplate.NewPhotoCommentWithRollups:
                    if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
                    htmlTemplate = Templates.NewPhotoCommentWithRollupsHtml;
                    textTemplate = Templates.NewPhotoCommentWithRollupsText;
                    break;
                default:
                    throw new ArgumentException(@"Template name not recognised.", nameof(emailTemplate));
            }

            var htmlInnerContent = string.Format(htmlTemplate, parameters);
            var completeHtmlContent = string.Format(htmlMaster, ConfigurationManager.AppSettings["LB.EmailMediaUrl"], htmlInnerContent);
            var completeTextContent = string.Format(textTemplate, parameters);

            // debug email delivery?
            if (bool.Parse(ConfigurationManager.AppSettings["LB.DebugEmailDelivery"]))
            {
                recipients.Clear();
                recipients.Add(ConfigurationManager.AppSettings["LB.DebugEmailAddress"]);
            }

            await SendEmail(recipients, subject, completeHtmlContent, completeTextContent);
        }

        #region private methods
        private static async Task SendEmail(List<string> recipients, string subject, string htmlContent, string textContent)
        {
            const string from = "londonbikers.com <noreply@londonbikers.com>";
            var destination = new Destination { ToAddresses = recipients };

            // Create the subject and body of the message.
            var contentSubject = new Content(subject);
            var htmlBody = new Content(htmlContent);
            var textBody = new Content(textContent);
            var body = new Body { Html = htmlBody, Text = textBody };

            // Create a message with the specified subject and body.
            var message = new Message(contentSubject, body);

            // Assemble the email.
            var request = new SendEmailRequest(from, destination, message);

            // Choose the AWS region of the Amazon SES endpoint you want to connect to. Note that your production 
            // access status, sending limits, and Amazon SES identity-related settings are specific to a given 
            // AWS region, so be sure to select an AWS region in which you set up Amazon SES. 
            // Examples of other regions that Amazon SES supports are USWest2 
            // and EUWest1. For a complete list, see http://docs.aws.amazon.com/ses/latest/DeveloperGuide/regions.html 
            var region = Amazon.RegionEndpoint.EUWest1;

            // Instantiate an Amazon SES client, which will make the service call.
            var client = new AmazonSimpleEmailServiceClient(ConfigurationManager.AppSettings["AWS.SES.AccessKeyId"], ConfigurationManager.AppSettings["AWS.SES.SecretAccessKey"], region);
            
            try
            {
                // Send the email
                var response = await client.SendEmailAsync(request);
                if (response.HttpStatusCode != HttpStatusCode.OK)
                    Logging.LogError(typeof(EmailController).FullName, $"SendEmail() - Not successful! MessageId: {response.MessageId}, HttpStatusCode: {response.HttpStatusCode}");
            }
            catch (System.Threading.ThreadAbortException ex)
            {
                Logging.LogError(typeof(EmailController).FullName, ex);
            }
        }
        #endregion
    }
}