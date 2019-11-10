using Bryllite.Extensions;
using Bryllite.Utils.Currency;
using Bryllite.Utils.Rlp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bryllite.App.Sample.TcpGameServer
{
    public class Sales
    {
        // 판매 고유 아이디
        [JsonProperty("order")]
        public string Order;

        // 판매자 아이디
        [JsonProperty("seller")]
        public string Seller;

        // 판매 아이템 코드
        [JsonProperty("itemcode")]
        public string ItemCode;

        // 판매 아이템 이름
        [JsonProperty("itemname")]
        public string ItemName;

        // 판매 가격
        [JsonProperty("price")]
        public decimal Price;

        [JsonIgnore]
        public byte[] Rlp => ToRlp();

        public Sales(string seller, string itemcode, string itemname, decimal price)
        {
            Seller = seller;
            ItemCode = itemcode;
            ItemName = itemname;
            Price = price;

            Order = Hex.ToString(SecureRandom.GetBytes(20));
        }

        private Sales(byte[] rlp)
        {
            var decoder = new RlpDecoder(rlp);

            Order = Encoding.UTF8.GetString(decoder.Next());
            Seller = Encoding.UTF8.GetString(decoder.Next());
            ItemCode = Encoding.UTF8.GetString(decoder.Next());
            ItemName = Encoding.UTF8.GetString(decoder.Next());
            Price = Coin.ToCoin(Hex.ToNumber<ulong>(decoder.Next()));
        }

        private byte[] ToRlp()
        {
            return RlpEncoder.EncodeList(Order, Seller, ItemCode, ItemName, Coin.ToBeryl(Price));
        }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }

        public static Sales Parse(byte[] rlp)
        {
            return new Sales(rlp);
        }

        public static bool TryParse(byte[] rlp, out Sales sales)
        {
            try
            {
                sales = Parse(rlp);
                return true;
            }
            catch
            {
                sales = null;
                return false;
            }
        }
    }
}
