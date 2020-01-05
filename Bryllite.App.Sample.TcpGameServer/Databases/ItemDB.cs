using Bryllite.Database.TrieDB;
using Bryllite.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bryllite.App.Sample.TcpGameServer
{
    public class ItemDB : FileDB
    {
        public IEnumerable<Item> All => SelectAll();

        public ItemDB(string path) : base(path)
        {
        }

        private static byte[] ToKey(string code)
        {
            return Encoding.UTF8.GetBytes(code);
        }

        public static string ToItemCode(string name)
        {
            return Hex.ToString(Encoding.UTF8.GetBytes(name));
        }

        public bool Contains(string code)
        {
            return !Get(ToKey(code)).IsNullOrEmpty();
        }

        public bool ContainsByName(string name)
        {
            return Contains(ToItemCode(name));
        }

        public Item Select(string code)
        {
            return Item.TryParse(Get(ToKey(code)), out var item) ? item : null;
        }

        public Item SelectByName(string name)
        {
            return Select(ToItemCode(name));
        }

        public IEnumerable<Item> SelectAll()
        {
            var items = new List<Item>();
            foreach (var entry in AsEnumerable())
                items.Add(Item.Parse(entry.Value));
            return items;
        }

        public bool Insert(Item item)
        {
            if (Contains(item.Code)) return false;

            return Put(ToKey(item.Code), item.Rlp);
        }

        public string Insert(string name, decimal price)
        {
            var item = new Item(ToItemCode(name), name, price);
            return Insert(item) ? item.Code : string.Empty;
        }

        public bool Update(string code, decimal price)
        {
            var item = Select(code);
            if (null == item)
                return false;

            item.Price = price;
            return Put(ToKey(item.Code), item.Rlp);
        }

        public bool Delete(string code)
        {
            return Del(ToKey(code));
        }

        public bool DeleteByName(string name)
        {
            return Delete(ToItemCode(name));
        }

    }
}
