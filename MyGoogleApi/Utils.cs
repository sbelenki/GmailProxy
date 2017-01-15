using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGoogleApi
{
   public class Utils
   {
      public static string MakeValidFileName(string fileName)
      {
         var builder = new StringBuilder();
         var invalid = System.IO.Path.GetInvalidFileNameChars();
         foreach (var cur in fileName)
         {
            if (!invalid.Contains(cur))
            {
               builder.Append(cur);
            }
         }
         return builder.ToString();
      }

      public static string DecodeBase64URLSafe(string input)
      {
         // TODO: use FromBase64ForUrlString from http://stackoverflow.com/questions/24779138/can-we-access-gmail-api-using-service-account
         string codedBody = input.Replace("-", "+").Replace('_', '/');
         byte[] data = Convert.FromBase64String(codedBody);
         return Encoding.UTF8.GetString(data);
      }
   }
}
