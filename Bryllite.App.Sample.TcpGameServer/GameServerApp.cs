using Bryllite.App.Sample.TcpGameBase;
using Bryllite.Cryptography.Signers;
using Bryllite.Utils.AppBase;
using Bryllite.Utils.NabiLog;
using Bryllite.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Bryllite.Utils.Currency;
using Bryllite.Rpc.Web4b.Extensions;

namespace Bryllite.App.Sample.TcpGameServer
{
    public class GameServerApp : AppBase
    {
        // game db
        public readonly GameDB GameDB;

        // game server
        public readonly GameServer GameServer;

        // game service port
        public readonly int GamePort;

        // game key
        public readonly PrivateKey GameKey;

        // market commission
        public readonly decimal Commission;

        // coinbox address
        public readonly string CoinBox;

        // bryllite api service for gameserver
        public readonly BrylliteApiForGameServer ApiService;

        public GameServerApp(string[] args) : base(args)
        {
            // map command handlers
            OnMapCommandHandlers();

            // gamedb
            GameDB = new GameDB(this);

            // gameserver
            GameServer = new GameServer(this);

            // game port
            GamePort = config["game"].Value<int>("port");

            // game key
            GameKey = config["game"].Value<string>("key");

            // market commission
            Commission = config["game"].Value<decimal>("commission");

            // coinbox address
            CoinBox = config["coinbox"].Value<string>("address");

            // create bryllite api for gameserver
            string remote = config["bridge"].Value<string>("url");
            ApiService = new BrylliteApiForGameServer(remote, GameKey, CoinBox);
        }

        private void OnMapCommandHandlers()
        {
            MapCommandHandler("server.start", "([port]): 게임 서버를 시작합니다", OnCommandServerStart);
            MapCommandHandler("server.stop", "게임 서버를 정지합니다", OnCommandServerStop);
            MapCommandHandler("server.sessions", "접속중인 세션 정보를 출력합니다", OnCommandServerSessions);

            MapCommandHandler("users", "모든 게임 사용자 계정 목록을 출력합니다", OnCommandUsers);
            MapCommandHandler("user.new", "([uid]): 새로운 게임 사용자 계정을 생성합니다", OnCommandUserNew);
            MapCommandHandler("user.view", "(uid): 게임 사용자 계정 정보를 출력합니다", OnCommandUserView);
            MapCommandHandler("user.del", "(uid): 게임 사용자 계정을 삭제합니다", OnCommandUserDel);

            MapCommandHandler("items", "등록된 아이템 목록을 출력합니다", OnCommandItems);
            MapCommandHandler("item.new", "(name, price): 새로운 아이템을 등록합니다", OnCommandItemNew);
            MapCommandHandler("item.del", "(name): 아이템을 삭제합니다", OnCommandItemDel);
            MapCommandHandler("item.issue", "(uid, name): 사용자에게 아이템을 지급합니다", OnCommandItemIssue);
            MapCommandHandler("item.withdraw", "(uid, name): 사용자의 아이템을 회수합니다", OnCommandItemWithdraw);

            MapCommandHandler("coinbase", "코인 베이스 계좌 주소 정보를 출력합니다", OnCommandCoinbase);
            MapCommandHandler("coin.issue", "(uid, balance): 게임 사용자에게 코인을 지급합니다", OnCommandCoinIssue);
            MapCommandHandler("coin.withdraw", "(uid, balance): 게임 사용자의 코인을 회수합니다", OnCommandCoinWithdraw);
        }


        public override bool OnAppInitialize()
        {
            Log.Info("GameServerApplication started");

            // start game db
            GameDB.Start();

            // start game server
            GameServer.Start(GamePort);
            Log.Info("GameServer started on port: ", Color.DarkGreen, GamePort);

            // connection made
            ApiService.Connect();

            return true;
        }

        public override void OnAppCleanup()
        {
            // stop game server
            GameServer.Stop();

            // stop gamedb
            GameDB.Stop();

            Log.Info("GameServerApplication terminated");
        }

        public string GetAddressByUid(string uid)
        {
            return GameKey.CKD(uid).Address;
        }

        private void OnCommandServerStart(string[] args)
        {
            int port = args.Length > 0 ? Convert.ToInt32(args[0]) : this.GamePort;

            // start game server
            GameServer.Start(port);
            Log.Info("GameServer started on port: ", Color.DarkGreen, port);
        }

        private void OnCommandServerStop(string[] args)
        {
            // stop game server
            GameServer.Stop();
            Log.Info("GameServer stopped!");
        }

        private void OnCommandServerSessions(string[] args)
        {
            foreach (var session in GameServer.Sessions)
            {
                string uid = GameServer.GetUidBySession(session);
                BConsole.WriteLine(uid, "=", session);
            }
        }



        private void OnCommandUsers(string[] args)
        {
            foreach (var user in GameDB.Users.SelectAll())
            {
                var j = JObject.FromObject(user);
                j.Put("passhash", user.PassHash);
                BConsole.WriteLine(j.ToString());
            }
        }

        private void OnCommandUserNew(string[] args)
        {
            string uid = args.Length > 0 ? args[0] : BConsole.ReadLine("uid: ");
            if (string.IsNullOrEmpty(uid))
                return;

            // existing uid?
            if (GameDB.Users.Contains(uid))
            {
                BConsole.WriteLine("uid: ", Color.DarkGreen, uid, " already exists!");
                return;
            }

            // password
            string password = BConsole.ReadPassword("password: ");
            if (string.IsNullOrEmpty(password))
                return;

            // password confirm
            string confirm = BConsole.ReadPassword("password confirm: ");
            if (password != confirm)
            {
                BConsole.WriteLine("password mismatch with password confirm!");
                return;
            }

            // insert user with encrypted password
            string passcode = PasswordEncoder.Encode(password);
            bool result = GameDB.Users.Insert(new User(uid, passcode));
            BConsole.WriteLine("creating new user(", uid, ")=", result);
        }

        private void OnCommandUserView(string[] args)
        {
            string uid = args[0];
            var user = GameDB.Users.Select(uid);
            if (ReferenceEquals(user, null))
                return;

            // 사용자 정보
            JObject info = JObject.FromObject(user);
            info.Put("passhash", user.PassHash);

            // 사용자 계좌 잔고 조회
            string address = GameKey.CKD(uid).Address;
            ulong balance = ApiService.GetBalanceAsync(address).Result;
            ulong nonce = ApiService.GetNonceAsync(address, false).Result;

            info.Put("address", address);
            info.Put("balance", balance);
            info.Put("nonce", nonce);

            // 사용자 인벤토리
            JArray inven = new JArray();
            foreach (var itemcode in GameDB.Inventories.Select(uid))
            {
                var item = GameDB.Items.Select(itemcode);
                if (null != item)
                    inven.Add(JObject.FromObject(item));
            }
            info.Put("inventory", inven);

            BConsole.WriteLine(uid, "=", info);
        }

        private void OnCommandUserDel(string[] args)
        {
            string uid = args[0];
            bool result = GameDB.Users.Delete(uid);
            BConsole.WriteLine("user(", uid, ") ", (!result ? "not " : string.Empty), "deleted!");
        }


        private void OnCommandCoinbase(string[] args)
        {
            string address = GameKey.Address;
            ulong balance = ApiService.GetBalanceAsync(address).Result;

            BConsole.WriteLine(address, "=", Coin.ToCoin(balance).ToString("N"), " BRC (", balance.ToString("N"), ")");
        }

        private void OnCommandCoinIssue(string[] args)
        {
            string uid = args[0];
            decimal value = decimal.Parse(args[1]);
            string address = GetAddressByUid(uid);

            // 코인 지급
            string txid = ApiService.TransferAsync(GameKey, address, value, 0).Result;
            BConsole.WriteLine("txid=", txid);

            if (null != txid)
            {
                string hash = ApiService.WaitForTransactionConfirm(txid, 1000).Result;

                // coinbase 와 uid 잔액 출력
                BConsole.WriteLine("coinbase.balance=", ApiService.GetBalanceAsync(GameKey.Address).Result);
                BConsole.WriteLine(uid, "(", address, ").balance=", ApiService.GetBalanceAsync(address).Result);
            }
        }

        private void OnCommandCoinWithdraw(string[] args)
        {
            string uid = args[0];
            decimal value = decimal.Parse(args[1]);
            PrivateKey signer = GameKey.CKD(uid);
            string address = signer.Address;

            // 코인 회수
            string txid = ApiService.TransferAsync(signer, GameKey.Address, value, 0).Result;
            BConsole.WriteLine("txid=", txid);

            if (null != txid)
            {
                string hash = ApiService.WaitForTransactionConfirm(txid, 1000).Result;

                // coinbase 와 uid 잔액 출력
                BConsole.WriteLine("coinbase.balance=", ApiService.GetBalanceAsync(GameKey.Address).Result);
                BConsole.WriteLine(uid, "(", address, ").balance=", ApiService.GetBalanceAsync(address).Result);
            }
        }

        private void OnCommandItems(string[] args)
        {
            foreach (var item in GameDB.Items.SelectAll())
                BConsole.WriteLine(item);
        }

        private void OnCommandItemNew(string[] args)
        {
            string name = args[0];
            decimal price = decimal.Parse(args[1]);

            string code = GameDB.Items.Insert(name, price);
            if (!string.IsNullOrEmpty(code))
                BConsole.WriteLine("item(", code, ") registered!");
        }

        private void OnCommandItemDel(string[] args)
        {
            string name = args[0];

            bool result = GameDB.Items.DeleteByName(name);
            BConsole.WriteLine("deleting item(", name, ") ", result);
        }


        private void OnCommandItemIssue(string[] args)
        {
            string uid = args[0];
            string name = args[1];

            // 해당 아이템이 존재하는지?
            if (!GameDB.Items.ContainsByName(name))
            {
                BConsole.WriteLine("unknown item name");
                return;
            }

            // 아이템 정보
            var item = GameDB.Items.SelectByName(name);
            if (null == item)
            {
                BConsole.WriteLine("unknown item name");
                return;
            }

            // 사용자에게 아이템 지급
            var inven = GameDB.Inventories.Select(uid);
            inven.Add(item.Code);
            GameDB.Inventories.Update(uid, inven);

            BConsole.WriteLine(uid, ".inven=", inven);
        }

        private void OnCommandItemWithdraw(string[] args)
        {
            string uid = args[0];
            string name = args[1];

            var inven = GameDB.Inventories.Select(uid);
            bool result = inven.Remove(ItemDB.ToItemCode(name));
            GameDB.Inventories.Update(uid, inven);

            BConsole.WriteLine("result=", result);
            BConsole.WriteLine(uid, ".inven=", inven);
        }
    }
}
