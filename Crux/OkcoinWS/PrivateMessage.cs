using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Crux.OkcoinWS
{
    [DataContract]
    abstract class PrivateMessage
    {
        public static char[] HEX_DIGITS = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public static string APIKey { get; set; }

        public static string SecretKey { get; set; }

        [DataMember(Name = "event")]
        public string Event { get; set; }

        [DataMember(Name = "parameters")]
        public Dictionary<string, string> Parameters { get; set; }

        public string Sign
        {
            get
            {
                if (Parameters.Count == 0)
                {
                    return "";
                }

                var keys = new List<string>(Parameters.Keys);
                keys.Sort();
                string paramString = "";
                for (int i = 0; i < keys.Count; i++)
                {
                    paramString += $"{keys[i]}={Parameters[keys[i]]}&";
                }

                paramString = paramString + $"secret_key={SecretKey}";

                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                var hash = md5.ComputeHash(Encoding.Default.GetBytes(paramString));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append($"{HEX_DIGITS[(hash[i] & 0xf0) >> 4]}{HEX_DIGITS[hash[i] & 0xf]}");
                }
                return sb.ToString();
            }
        }

        public PrivateMessage()
        {
            Parameters = new Dictionary<string, string>();
        }

        public static void LoadKeyfile(string keyfile)
        {
            var lines = File.ReadAllLines(keyfile);
            APIKey = lines[0];
            SecretKey = lines[1];
        }
    }
}
