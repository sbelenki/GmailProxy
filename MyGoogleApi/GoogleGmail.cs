using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using System.IO;
using System.Diagnostics;

namespace MyGoogleApi
{
   public class GmailAttachmentInfo
   {
      // properties
      public string Id { get; set; }         // attachment ID
      public string Filename { get; set; }   // attachment filename
      public string MessageId { get; set; }  // message ID for the attachment

      // constructors
      public GmailAttachmentInfo(string id, string filename, string messageId)
      {
         Id = id;
         Filename = filename;
         MessageId = messageId;
      }
   }

   public class GoogleGmail
   {
      public class MyLabelInfo
      {
         // properties
         public string Id { get; set; }   // label ID
         public string Name { get; set; } // label Name
      }

      public class MyMessageInfo
      {
         // properties
         public string Id { get; set; }      // message ID
         public string ContentType { get; set; }   // MIME content type
         public string From { get; set; }    // the message From address(es)
         public string To { get; set; }      // the message To address(es)
         public string Cc { get; set; }      // the message Cc address(es)
         public string Subject { get; set; } // the message Subject
         public string Date { get; set; }    // the creation sent date
         public int    Size { get; set; }    // the message size in octets

         // constructors
         public MyMessageInfo()
         {
            Id = "";
            ContentType = "";
            From = "";
            To = "";
            Cc = "";
            Subject = "";
            Date = "";
            Size = 0;
         }
      }

      public class MyMessage
      {
         // properties
         public MyMessageInfo Info { get; set; }   // the associated message metadata
         public string Text { get; set; }          // the message plain text parsed from all MIME parts
         public List<GmailAttachmentInfo> Attachments { get; set; }  // list of all attachments info

         // constructors
         public MyMessage(MyMessageInfo info)
         {
            Info = info;
            Text = "";
         }
      }

      GmailService service;
      public string UserId { get; set; }        // current user ("me" is the currently signed-in user)
      public string EmailAddress { get; set; }  // current user email retrieved from profile

      /// <summary>Indicates if the class was initialized.
      /// </summary>
      /// <returns>Returns true if the class was initialized.</returns>
      public bool IsInitialized()
      {
         return (service != null);
      }

      /// <summary>Init the class instance and fetch email address from the user profile.
      /// <see href="https://developers.google.com/gmail/api/v1/reference/users/getProfile">Users: getProfile</see>
      /// <param name="gsf">The factory containing user authorization info.</param>
      /// </summary>
      public void Init(GoogleServiceFactory gsf)
      {
         service = gsf.GmailService;

         UserId = gsf.UserId;

         if (service != null)
         {
            UsersResource.GetProfileRequest reqUser = service.Users.GetProfile(UserId);
            EmailAddress = reqUser.Execute().EmailAddress;
         }
      }

      /// <summary>Lists all labels in the user's mailbox.
      /// <see href="https://developers.google.com/gmail/api/v1/reference/users/labels/list">Users.labels: list</see>
      /// </summary>
      /// <returns>Returns list of all labels for the user.</returns>
      public List<MyLabelInfo> GetLabelNames()
      {
         Trace.WriteLine("GetLabelNames for " + EmailAddress);
         List<MyLabelInfo> retVal = new List<MyLabelInfo>();
         UsersResource.LabelsResource.ListRequest request = service.Users.Labels.List(UserId);

         // List all labels
         IList<Google.Apis.Gmail.v1.Data.Label> labels = request.Execute().Labels;

         if (labels == null || labels.Count == 0)
         {
            Trace.WriteLine("No labels found for " + EmailAddress);
         }
         else {
            foreach (var labelItem in labels)
            {
               Trace.WriteLine(labelItem.Name + "\t" + labelItem.Id + "\t" + labelItem.MessagesUnread);
               retVal.Add(new MyLabelInfo {Id = labelItem.Id, Name = labelItem.Name });
            }
         }
         return retVal;
      }

      /// <summary>Get messages metadata for the label.
      /// <see href="https://developers.google.com/gmail/api/v1/reference/users/messages/list">Users.messages: list</see>
      /// </summary>
      /// <param name="labelName">Name of the label to return message for.</param>
      /// <param name="labelId">ID of the label to return message for.</param>
      /// <param name="maxResults">Maximum number of messages to return.</param>
      /// <returns>A list of retrieved messages..</returns>
      public List<MyMessageInfo> GetLabelMessagesInfo(string labelName, string labelId, int maxResults)
      {
         List<MyMessageInfo> retVal = new List<MyMessageInfo>();

         // Select messages with specified LabelIds: https://developers.google.com/gmail/api/v1/reference/users/messages/list
         Trace.WriteLine("GetLabelMessagesInfo: Label Name: " + labelName + " Label Id: " + labelId);
         UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List(UserId);
         request.LabelIds = labelId;
         request.MaxResults = maxResults;
         try
         {
            ListMessagesResponse response = request.Execute();
            IList<Google.Apis.Gmail.v1.Data.Message> messages = response.Messages;
            // TODO: implement paging - request.PageToken = response.NextPageToken;
            if (messages != null)
            {
               // TODO: implement Http batch request to fetch all Ids at once
               foreach (var msg in messages)
               {
                  retVal.Add(fetchMessageInfo(msg.Id));
               }
            }
         }
         catch (Exception ex)
         {
            Trace.WriteLine("Exception: " + ex.Message);
         }

         return retVal;
      }

      /// <summary>Get the message metadata (MessageInfo) for a specified message.
      /// <see href="https://developers.google.com/gmail/api/v1/reference/users/messages/get">Users.messages: get</see>
      /// </summary>
      /// <param name="msgId">The ID of the message to retrieve.</param>
      /// <returns>Returns MessageInfo structure containing the message metadata.</returns>
      private MyMessageInfo fetchMessageInfo(string msgId)
      {
         MyMessageInfo retVal = null;
         // Fetch each message info - subject, date etc: https://developers.google.com/gmail/api/v1/reference/users/messages/get
         Trace.WriteLine("fetchMessageInfo: " + msgId);
         var msgInfoReq = service.Users.Messages.Get(UserId, msgId);
         msgInfoReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
         //msgInfoReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
         msgInfoReq.MetadataHeaders = new Google.Apis.Util.Repeatable<string>(new[] { "content-type", "from", "to", "cc", "subject", "date" });
         Google.Apis.Gmail.v1.Data.Message msgInfo = msgInfoReq.Execute();
         retVal = new MyMessageInfo();
         retVal.Id = msgId;
         if (msgInfo.SizeEstimate.HasValue) { retVal.Size = msgInfo.SizeEstimate.Value; }
         for (int i = 0; i < msgInfo.Payload.Headers.Count(); ++i)
         {
            if (msgInfo.Payload.Headers[i].Name.ToLower() == "content-type") { retVal.ContentType = msgInfo.Payload.Headers[i].Value; }
            else if (msgInfo.Payload.Headers[i].Name.ToLower() == "from") { retVal.From = msgInfo.Payload.Headers[i].Value; }
            else if (msgInfo.Payload.Headers[i].Name.ToLower() == "to") { retVal.To = msgInfo.Payload.Headers[i].Value; }
            else if (msgInfo.Payload.Headers[i].Name.ToLower() == "cc") { retVal.Cc = msgInfo.Payload.Headers[i].Value; }
            else if (msgInfo.Payload.Headers[i].Name.ToLower() == "subject") { retVal.Subject = msgInfo.Payload.Headers[i].Value; }
            else if (msgInfo.Payload.Headers[i].Name.ToLower() == "date") { retVal.Date = msgInfo.Payload.Headers[i].Value; }
         }
         return retVal;
      }

      /// <summary>Get the message specified by MessageInfo structure.
      /// <see href="https://developers.google.com/gmail/api/v1/reference/users/messages/get">Users.messages: get</see>
      /// </summary>
      /// <param name="msgInfo">Info for the message to retrieve.</param>
      /// <returns>Returns structure containing message info, plain text, and info on attachments.</returns>
      public MyMessage GetMessage(MyMessageInfo msgInfo)
      {
         MyMessage retVal = null;

         Trace.WriteLine("GetMessage: " + msgInfo.Id);
         var msgReq = service.Users.Messages.Get(UserId, msgInfo.Id);
         msgReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
         Google.Apis.Gmail.v1.Data.Message msg = msgReq.Execute();
         retVal = new MyMessage(msgInfo);
         List<GmailAttachmentInfo> attachments = new List<GmailAttachmentInfo>();
         retVal.Text = parseText(msg.Payload, ref attachments, msgInfo.Id);
         retVal.Attachments = attachments;

         return retVal;
      }

      /// <summary>Iteratively parse MIME message parts to retrieve plain text and attachments
      /// <see href="http://stackoverflow.com/questions/25677866/converting-gmail-messageparts-into-readable-text">Converting Gmail MessageParts into Readable Text</see>
      /// </summary>
      /// <param name="parts">The message parts</param>
      /// <param name="attachments">Reference to attachments collection.</param>
      /// <param name="messageId">Message ID to add to attachments info.</param>
      /// <returns>Returns parsed plain text.</returns>
      private string parseText(Google.Apis.Gmail.v1.Data.MessagePart parts, ref List<GmailAttachmentInfo> attachments, string messageId)
      {
         string result = "";

         if (parts.Parts != null)
         {
            foreach (Google.Apis.Gmail.v1.Data.MessagePart part in parts.Parts)
            {
               result = string.Format("{0}\n{1}", result, parseText(part, ref attachments, messageId));
            }
         }
         else if (parts.Body.Data != null && parts.Body.AttachmentId == null && parts.MimeType == "text/plain")
         {
            string codedBody = parts.Body.Data.Replace("-", "+");
            codedBody = codedBody.Replace("_", "/");
            byte[] data = Convert.FromBase64String(codedBody);
            result = Encoding.UTF8.GetString(data);
         }
         else if (parts.Body.AttachmentId != null)
         {
            // get attachment IDs and filenames
            attachments.Add(new GmailAttachmentInfo(parts.Body.AttachmentId, parts.Filename, messageId));
         }

         return result;
      }

      /// <summary>Gets the specified message attachment.
      /// <see href="https://developers.google.com/gmail/api/v1/reference/users/messages/attachments/get">Users.messages.attachments: get</see>
      /// </summary>
      /// <param name="msgId">ID of the message containing the attachment.</param>
      /// <param name="attId">The attachment ID.</param>
      /// <param name="filename">Name of the file to save the attachment.</param>
      /// <returns>Returns true if the operation successful.</returns>
      public bool GetAttachmentToFile(string msgId, string attId, string filename)
      {
         bool retVal = false;
         try
         {
            var attReq = service.Users.Messages.Attachments.Get(UserId, msgId, attId);
            Google.Apis.Gmail.v1.Data.MessagePartBody msg = attReq.Execute();

            // convert from MIME and write into the file
            string codedBody = msg.Data.Replace("-", "+").Replace("_", "/");
            byte[] data = Convert.FromBase64String(codedBody);
            File.WriteAllBytes(filename, data);
            retVal = true;
         }
         catch (Exception ex)
         {
            Trace.WriteLine("Exception: " + ex.Message);
         }
         return retVal;
      }

      /// <summary>Sends MIME message the specified recipients.
      /// <see href="https://developers.google.com/gmail/api/v1/reference/users/messages/send">Users.messages: send</see>
      /// </summary>
      /// <param name="rawMsg">MIME encoded (raw) message.</param>
      /// <returns>Returns GMail message ID if the operation was successful.</returns>
      public string SendMessageRaw(string rawMsg)
      {
         string retVal = ""; // new message ID if the sending succeeded
         var msgBody = new Google.Apis.Gmail.v1.Data.Message();
         msgBody.Raw = encodeBase64URLSafeString(rawMsg);
         var msgSendReq = service.Users.Messages.Send(msgBody, UserId);
         Google.Apis.Gmail.v1.Data.Message result = msgSendReq.Execute();

         Trace.WriteLine("SendMessage: " + result.Id);
         Debug.WriteLine("SendMessage: " + result.ToString());
         retVal = result.Id;

         return retVal;
      }

      /// <summary>Encodes message into MIME and sends to the specified recipients.
      /// <see href="https://developers.google.com/gmail/api/v1/reference/users/messages/send">Users.messages: send</see>
      /// </summary>
      /// <param name="mailMessage">Instances of the MailMessage class are used to construct e-mail messages that are transmitted to an SMTP server for delivery using the SmtpClient class.</param>
      /// <returns>Returns GMail message ID.</returns>
      public string SendMessage(System.Net.Mail.MailMessage mailMessage)
      {
         string retVal = ""; // new message ID if the sending succeeded

         // convert to MIME
         var mimeMsg = "";
         using (var reader = new StreamReader(mailMessage.RawMessage()))
         using (var writer = new StringWriter())
         {
            while (true)
            {
               var line = reader.ReadLine();
               if (line == null) break; // EOF

               if (line != "")
               {
                  // Header line
                  writer.WriteLine(line);
                  continue;
               }

               // End of headers, insert bcc, read body, then bail
               if (mailMessage.Bcc.Count > 0)
               {
                  writer.WriteLine("Bcc: " + mailMessage.Bcc.ToString());
               }
               writer.WriteLine("");
               writer.Write(reader.ReadToEnd());
               break;
            }

            mimeMsg = writer.ToString();
         }
         var msgBody = new Google.Apis.Gmail.v1.Data.Message();
         msgBody.Raw = encodeBase64URLSafeString(mimeMsg);
         var msgSendReq = service.Users.Messages.Send(msgBody, UserId);
         Google.Apis.Gmail.v1.Data.Message result = msgSendReq.Execute();

         Trace.WriteLine("SendMessage: " + result.Id);
         Debug.WriteLine("SendMessage: " + result.ToString());
         retVal = result.Id;

         return retVal;
      }

      /// <summary>Encode string into special "url-safe" base64 encode.</summary>
      /// <param name="input">String to encode.</param>
      /// <returns>Returns encoded string.</returns>
      private static string encodeBase64URLSafeString(string input)
      {
         var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
         return Convert.ToBase64String(inputBytes)
           .Replace('+', '-')
           .Replace('/', '_')
           .Replace("=", "");
      }

      /// <summary>Gets the specified message in the raw MIME format.
      /// <see href="https://developers.google.com/gmail/api/v1/reference/users/messages/get">Users.messages: get</see>
      /// </summary>
      /// <param name="msgId">The ID of the message to trash</param>
      /// <returns>Returns true if the operation was successful.</returns>
      public string GetMessageRaw(string msgId)
      {
         Trace.WriteLine("GetMessageRaw: " + msgId);
         var msgReq = service.Users.Messages.Get(UserId, msgId);
         msgReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
         Google.Apis.Gmail.v1.Data.Message msg = msgReq.Execute();
         return msg.Raw;
      }

      /// <summary>Moves the specified message into the TRASH special folder
      /// <see href="https://developers.google.com/gmail/api/v1/reference/users/messages/trash">Users.messages: trash</see>
      /// </summary>
      /// <param name="msgId">The ID of the message to trash</param>
      /// <returns>Returns true if the operation was successful.</returns>
      public bool TrashMessage(string msgId)
      {
         bool retVal = false;
         Trace.WriteLine("TrashMessage: " + msgId);
         try
         {
            var result = service.Users.Messages.Trash(UserId, msgId).Execute();
            retVal = true;
         }
         catch (Exception ex)
         {
            Trace.WriteLine("TrashMessage Exception: " + ex.Message);
         }
         return retVal;
      }
   }
}
