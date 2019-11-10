using Bryllite.Database.TrieDB;
using Bryllite.Extensions;
using Bryllite.Utils.Rlp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bryllite.App.Sample.TcpGameServer
{
    public class InventoryDB : FileDB
    {
        public InventoryDB(string path) : base(path)
        {
        }

        private static byte[] ToKey(string uid)
        {
            return Encoding.UTF8.GetBytes(uid);
        }

        // user inventory exists?
        public bool Contains(string uid)
        {
            return !Get(ToKey(uid)).IsNullOrEmpty();
        }

        // user has item?
        public bool Contains(string uid, string code)
        {
            return Select(uid)?.Contains(code) ?? false;
        }

        // select user inventory
        public Inventory Select(string uid)
        {
            return Inventory.TryParse(Get(ToKey(uid)), out Inventory inventory) ? inventory : new Inventory();
        }

        // update user inventory
        public bool Update(string uid, Inventory inventory)
        {
            return Put(ToKey(uid), inventory.Rlp);
        }

        // remote user inventory
        public bool Delete(string uid)
        {
            return Del(ToKey(uid));
        }
    }
}
