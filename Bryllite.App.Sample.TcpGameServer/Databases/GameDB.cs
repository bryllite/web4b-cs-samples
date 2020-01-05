using Bryllite.Extensions;
using Bryllite.Utils.Rlp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bryllite.App.Sample.TcpGameServer
{

    public class GameDB
    {
        // users db
        public readonly UserDB Users;

        // items db
        public readonly ItemDB Items;

        // inventory db
        public readonly InventoryDB Inventories;

        // market db
        public readonly MarketDB Market;

        // application
        private readonly GameServerApp app;

        public GameDB(GameServerApp app, string path)
        {
            this.app = app;

            // user db
            Users = new UserDB(Path.Combine(path, "users"));

            // user db
            Items = new ItemDB(Path.Combine(path, "items"));

            // inventory db
            Inventories = new InventoryDB(Path.Combine(path, "inventories"));

            // market db
            Market = new MarketDB(Path.Combine(path, "market"));
        }

        public void Start()
        {
            Users.Start();
            Items.Start();
            Inventories.Start();
            Market.Start();
        }

        public void Stop()
        {
            Users.Stop();
            Items.Stop();
            Inventories.Stop();
            Market.Stop();
        }
    }
}
