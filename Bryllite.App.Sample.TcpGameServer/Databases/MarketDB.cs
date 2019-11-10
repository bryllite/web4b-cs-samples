using Bryllite.Database.TrieDB;
using Bryllite.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bryllite.App.Sample.TcpGameServer
{
    public class MarketDB : FileDB
    {
        public IEnumerable<Sales> All => ToList();

        public MarketDB(string path) : base(path)
        {
        }

        private static byte[] ToKey(string order)
        {
            return Encoding.UTF8.GetBytes(order);
        }

        public bool Contains(string order)
        {
            return !Get(ToKey(order)).IsNullOrEmpty();
        }

        public bool Insert(Sales sales)
        {
            return Put(ToKey(sales.Order), sales.Rlp);
        }

        public Sales Select(string order)
        {
            return Sales.TryParse(Get(ToKey(order)), out var sales) ? sales : null;
        }

        public bool Delete(string order)
        {
            return Del(ToKey(order));
        }

        public List<Sales> ToList()
        {
            List<Sales> list = new List<Sales>();
            foreach (var entry in this)
                list.Add(Sales.Parse(entry.Value));

            return list;
        }
    }
}
