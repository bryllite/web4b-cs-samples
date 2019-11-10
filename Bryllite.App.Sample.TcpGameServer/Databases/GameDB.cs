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
        // db path
        private static readonly string BASEPATH = "gamedb";

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

        public GameDB(GameServerApp app)
        {
            this.app = app;

            // user db
            Users = new UserDB(Path.Combine(BASEPATH, "users"));

            // user db
            Items = new ItemDB(Path.Combine(BASEPATH, "items"));

            // inventory db
            Inventories = new InventoryDB(Path.Combine(BASEPATH, "inventories"));

            // market db
            Market = new MarketDB(Path.Combine(BASEPATH, "market"));
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
