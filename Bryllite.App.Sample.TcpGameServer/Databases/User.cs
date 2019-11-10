using Bryllite.Extensions;
using Bryllite.Utils.Rlp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bryllite.App.Sample.TcpGameServer
{
    public class User
    {
        // user id
        [JsonProperty("id")]
        public string Id;

        // password hash
        [JsonIgnore]
        public string PassHash;

        // register date
        [JsonProperty("rdate")]
        public string RegisterDate;

        // user rlp
        [JsonIgnore]
        public byte[] Rlp => ToRlp();

        public User(string id, string passhash)
        {
            Id = id;
            PassHash = passhash;
            RegisterDate = DateTime.Now.ToString();
        }

        private User(byte[] rlp)
        {
            var decoder = new RlpDecoder(rlp);

            Id = Encoding.UTF8.GetString(decoder.Next());
            PassHash = Encoding.UTF8.GetString(decoder.Next());
            RegisterDate = Encoding.UTF8.GetString(decoder.Next());
        }

        private byte[] ToRlp()
        {
            return RlpEncoder.EncodeList(Id, PassHash, RegisterDate);
        }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }

        public static User Parse(byte[] rlp)
        {
            return new User(rlp);
        }

        public static bool TryParse(byte[] rlp, out User user)
        {
            try
            {
                user = Parse(rlp);
                return true;
            }
            catch
            {
                user = null;
                return false;
            }
        }
    }
}
