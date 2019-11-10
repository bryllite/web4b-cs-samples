using Bryllite.Extensions;
using Bryllite.Utils.Rlp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bryllite.App.Sample.TcpGameServer
{
    public class Item
    {
        // item code
        [JsonProperty("code")]
        public string Code;

        // item name
        [JsonProperty("name")]
        public string Name;

        // item price
        [JsonProperty("price")]
        public decimal Price;

        [JsonIgnore]
        public byte[] Rlp => ToRlp();

        public Item(string code, string name, decimal value)
        {
            Code = code;
            Name = name;
            Price = value;
        }

        private Item(byte[] rlp)
        {
            var decoder = new RlpDecoder(rlp);

            Code = Encoding.UTF8.GetString(decoder.Next());
            Name = Encoding.UTF8.GetString(decoder.Next());
            Price = Hex.ToNumber<decimal>(decoder.Next());
        }

        private byte[] ToRlp()
        {
            return RlpEncoder.EncodeList(Code, Name, Price);
        }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }

        public static Item Parse(byte[] rlp)
        {
            return new Item(rlp);
        }

        public static bool TryParse(byte[] rlp, out Item item)
        {
            try
            {
                item = Parse(rlp);
                return true;
            }
            catch
            {
                item = null;
                return false;
            }
        }

    }
}
