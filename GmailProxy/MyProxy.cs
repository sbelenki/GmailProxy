using System;
using LumiSoft.Net.POP3.Server;
using LumiSoft.Net.POP3.Client;
using LumiSoft.Net.SMTP.Server;
using LumiSoft.Net.SMTP.Client;
using MyGoogleApi;
using System.Text;
using System.IO;

namespace GmailProxy {

    /// <summary>
    /// Summary description for Pop3Server.
    /// </summary>
    class MyProxy {
        private readonly POP3_Server pServer = new POP3_Server();
        private readonly SMTP_Server sServer = new SMTP_Server();

        static string ApplicationName = "GMail Proxy";
        GoogleServiceFactory gsf = new GoogleServiceFactory();
        GoogleGmail gg = new GoogleGmail();

        public MyProxy (string message) {
            // init POP3 server
            pServer.SessionLog +=new LumiSoft.Net.LogEventHandler(pServer_SessionLog);
            pServer.LogCommands = true;
            pServer.AuthUser +=new LumiSoft.Net.POP3.Server.AuthUserEventHandler(server_AuthUser);
            pServer.SessionEnd +=new EventHandler(pServer_SessionEnd);
            pServer.GetMessgesList += pServer_GetMessgesList;
            pServer.GetMessage += pServer_GetMessage;
            pServer.DeleteMessage += pServer_DeleteMessage;
            pServer.Port = 1213; // non-standard POP3 port
            // init SMTP server
            sServer.SessionLog += new LumiSoft.Net.LogEventHandler(sServer_SessionLog);
            sServer.LogCommands = true;
            sServer.AuthUser += new LumiSoft.Net.SMTP.Server.AuthUserEventHandler(sServer_AuthUser);
            sServer.StoreMessage += sServer_StoreMessage;
            sServer.Port = 1214; // non-standard SMTP port
            // init Google APIs
            gsf.Init(ApplicationName, "me", "example.com"); // init credentials for all services
            gg.Init(gsf); // init GMail API service for current user
            // start POP3 and SMTP servers
            pServer.Enabled = true;
            sServer.Enabled = true;
        }

        public void Stop () {
            // stop servers
            pServer.Enabled = false;
            sServer.Enabled = false;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            MyProxy myProxy = new MyProxy("My Pop3Server");
            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
            myProxy.Stop();
        }

        private void server_AuthUser(object sender, LumiSoft.Net.POP3.Server.AuthUser_EventArgs e) {
            e.Validated = true; // no user/password validation
            POP3_Session sess = e.Session;
        }

        private void sServer_AuthUser(object sender, LumiSoft.Net.SMTP.Server.AuthUser_EventArgs e) {
            e.Validated = true; // no user/password validation
            SMTP_Session sess = e.Session;
        }

        private void pServer_SessionEnd(object sender, EventArgs e) {
            POP3_Session sess = (POP3_Session)sender;
        }

        private void pServer_SessionLog(object sender, LumiSoft.Net.Log_EventArgs e) {
            Console.WriteLine("POP3: {0}", e.LogText);
        }

        private void sServer_SessionLog(object sender, LumiSoft.Net.Log_EventArgs e) {
            Console.WriteLine("SMTP: {0}", e.LogText);
        }

        private void pServer_GetMessgesList(object sender, GetMessagesInfo_EventArgs e)
        {
            // Get max 1000 messages with name/label UNREAD/UNREAD
            List<GoogleGmail.MyMessageInfo> allUnread = gg.GetLabelMessagesInfo("UNREAD", "UNREAD", 1000);
            int counter = 0;
            foreach (var msgInfo in allUnread)
            {
                ++counter;
                POP3_MessageInfo newMsg = new POP3_MessageInfo(msgInfo.Id, counter, msgInfo.Size);
                e.Messages.AddMessage(newMsg.MessegeID, (int)newMsg.MessageSize, msgInfo);
            }
        }

        private void pServer_GetMessage(object sender, GetMessage_EventArgs e)
        {
            string userName = e.UserName;
            string msgId = e.MessageID;

            string msgRaw = gg.GetMessageRaw(e.MessageID);
            e.MessageData = Encoding.ASCII.GetBytes(Utils.DecodeBase64URLSafe(msgRaw));
        }

        private void pServer_DeleteMessage(object sender, DeleteMessage_EventArgs e)
        {
            string userName = e.UserName;
            string msgId = e.MessageID;
            gg.TrashMessage(msgId);
        }

        private void sServer_StoreMessage(object sender, NewMail_EventArgs e)
        {
            string mailFrom = e.MailFrom;
            string[] mailTo = e.MailTo;
            long msgSize = e.MessageSize;
            MemoryStream msgStream = e.MessageStream;
            string msgId = gg.SendMessageRaw(Encoding.UTF8.GetString(msgStream.ToArray()));
            if (string.IsNullOrEmpty(msgId))
            {
                Console.WriteLine("Error sending new message.");
            }
            else
            {
                Console.WriteLine("Sent new Message ID: {0}" + msgId);
            }
        }
    }
}
