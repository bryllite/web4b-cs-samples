using Bryllite.Utils.Rlp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bryllite.App.Sample.TcpGameServer
{
    // 사용자 인벤토리 ( 아이템 목록 )
    public class Inventory : List<string>
    {
        // rlp
        public byte[] Rlp => ToRlp();

        public Inventory(params string[] items) : base(items)
        {
        }

        private Inventory(byte[] rlp) : base()
        {
            var decoder = new RlpDecoder(rlp);
            for (int i = 0; i < decoder.Count; i++)
                Add(Encoding.UTF8.GetString(decoder.Next()));
        }

        private byte[] ToRlp()
        {
            return RlpEncoder.EncodeList(ToArray());
        }

        public override string ToString()
        {
            return new JArray(ToArray()).ToString();
        }

        public static Inventory Parse(byte[] bytes)
        {
            return new Inventory(bytes);
        }

        public static bool TryParse(byte[] rlp, out Inventory items)
        {
            try
            {
                items = Parse(rlp);
                return true;
            }
            catch
            {
                items = null;
                return false;
            }
        }
    }
}
