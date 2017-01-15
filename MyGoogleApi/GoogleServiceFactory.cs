using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Diagnostics;
using Google.Apis.People.v1;
using Google.GData.Client;
using Google.GData.Contacts;

namespace MyGoogleApi
{
   public class GoogleServiceFactory
   {
      // properties
      public UserCredential Credential { get; set; }// cached current user credentials
      GDataRequestFactory requestFactory;          // used in the old GData Contacts APIs

      public string ApplicationName { get; set; }  // current application name
      public string UserId { get; set; }           // current user ID (or "me" special user)
      public string UserDomain { get; set; }       // current user domain (example.com) for Contacts service

      // returns new Google GmailService instance
      public GmailService GmailService {
         get
         {
            return new GmailService(new BaseClientService.Initializer()
            {
               HttpClientInitializer = Credential,
               ApplicationName = ApplicationName,
            });
         }
      }

      // returns new Google PeopleService instance
      public PeopleService PeopleService
      {
         get
         {
            return new PeopleService(new BaseClientService.Initializer()
            {
               HttpClientInitializer = Credential,
               ApplicationName = ApplicationName,
            });
         }
      }

      // returns new Google ContactsService instance
      public ContactsService ContactsService
      {
         get
         {
            return new ContactsService(ApplicationName)
            {
               RequestFactory = requestFactory
            };
         }
      }

      /// <summary>Init the class instance and refresh authorization token if it just to expire.
      /// <see href="https://developers.google.com/gmail/api/v1/reference/users/getProfile">Users: getProfile</see>
      /// <param name="applicationName">The application name.</param>
      /// <param name="userId">Current user ID or special user "me".</param>
      /// <param name="userDomain">Domain if the factory used for Contacts service.</param>
      /// </summary>
      public void Init(string applicationName, string userId, string userDomain)
      {
         // If modifying these scopes, delete your previously saved credentials
         // at ~/.credentials/gmail-proxy.json
         // For a Read-Only access string[] Scopes = { GmailService.Scope.GmailReadonly };
         // All available Gmail scopes: https://developers.google.com/gmail/api/auth/scopes
         string[] Scopes = { GmailService.Scope.GmailModify, PeopleService.Scope.ContactsReadonly, "http://www.google.com/m8/feeds/" };

         ApplicationName = applicationName;
         UserId = userId;
         UserDomain = userDomain;

         // Store User credentials
         using (var stream =
               new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
         {
            string credPath = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.Personal);
            credPath = Path.Combine(credPath, ".credentials/gmail-proxy.json");

            Credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true)).Result;
            Trace.WriteLine("Credential file saved to: " + credPath);
         }

         // Old GData framework of ContactsService doesn't refresh auth token automatically
         // Check the token and refresh manually if expired or almost expired
         long secToExpiration = AuthTokenSecToExpiration();
         Trace.WriteLine("Init: auth token expiring in " + secToExpiration.ToString() + " seconds");
         if (secToExpiration < 10)
         {
            Trace.WriteLine("Init: refreshing auth token ...");
            bool result = Credential.RefreshTokenAsync(CancellationToken.None).GetAwaiter().GetResult();
            Trace.WriteLine("Init: auth token expiring in " + AuthTokenSecToExpiration().ToString() + " seconds");
         }
         requestFactory = new GDataRequestFactory(null);
         requestFactory.CustomHeaders.Add("Authorization: Bearer " + Credential.Token.AccessToken);
      }

      /// <summary>Calculate time left till the authorization token expiration.
      /// <returns>Time to expiration in seconds (could be negative if expired)</returns>
      private long AuthTokenSecToExpiration()
      {
         // TODO: add checked to fix overflow rollover
         return (long)((Credential.Token.ExpiresInSeconds ?? 0) - (System.DateTime.Now - Credential.Token.Issued).TotalSeconds);
      }
   }
}
