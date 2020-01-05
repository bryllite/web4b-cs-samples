using Bryllite.App.Sample.TcpGameBase;
using Bryllite.Utils.AppBase;
using Bryllite.Utils.Currency;
using Bryllite.Utils.NabiLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bryllite.App.Sample.TcpGameClient
{
    public class GameClientApp : AppBase
    {
        // game client
        public readonly GameClient GameClient;

        // game server address
        public readonly string GameUrl;

        public GameClientApp(string[] args) : base(args)
        {
            // server remote url
            GameUrl = config["game"].Value<string>("url");

            // game client
            GameClient = new GameClient(this)
            {
                OnConnectionEstablished = OnConnected,
                OnConnectionLost = OnDisconnected
            };
        }

        public override void OnMapCommandHandlers()
        {
            MapCommandHandler("connect", "([remote]): 게임 서버에 접속합니다", OnCommandConnect);
            MapCommandHandler("disconnect", "게임 서버 연결을 종료합니다", OnCommandDisconnect);
            MapCommandHandler("login", "게임 서버에 로그인을 요청합니다", OnCommandLogin);
            MapCommandHandler("info", "내 정보를 요청합니다", OnCommandInfo);

            MapCommandHandler("coin.balance", "내 코인 잔고를 확인합니다", OnCommandCoinBalance);
            MapCommandHandler("coin.transfer", "(toUid, value): 내 코인을 친구에게 이체합니다", OnCommandCoinTransfer);
            MapCommandHandler("coin.withdraw", "(to, value): 내 코인을 메인넷 주소로 출금합니다", OnCommandCoinWithdraw);
            MapCommandHandler("coin.history", "([bool]): 트랜잭션 히스토리를 출력합니다", OnCommandCoinHistory);

            // item shop
            MapCommandHandler("shop.list", "샵의 아이템 목록을 출력합니다", OnCommandShopList);
            MapCommandHandler("shop.buy", "(itemcode): 샵에서 아이템을 구매합니다", OnCommandShopBuy);

            // user market
            MapCommandHandler("market.register", "(itemcode, price): 아이템 판매를 등록합니다", OnCommandMarketRegister);
            MapCommandHandler("market.unregister", "(trid): 아이템 판매를 취소합니다", OnCommandMarketUnregister);
            MapCommandHandler("market.buy", "(trid): 판매중인 아이템을 구매합니다", OnCommandMarketBuy);
            MapCommandHandler("market.list", "아이템 판매 목록을 출력합니다", OnCommandMarketList);

            // key export
            MapCommandHandler("key.export", "유저키 내보내기를 위한 토큰을 요청합니다", OnCommandKeyExport);
        }


        public override bool OnAppInitialize()
        {
            Log.Info("GameClientApplication started");

            return true;
        }

        public override void OnAppCleanup()
        {
            GameClient.Stop();

            Log.Info("GameClientApplication terminated");
        }

        private void OnConnected(bool connected)
        {
            Log.Info("connected=", connected);
        }

        private void OnDisconnected(int reason)
        {
            Log.Info("connection lost! reason=", reason);
        }

        private void OnCommandConnect(string[] args)
        {
            string remote = args.Length > 0 ? args[0] : this.GameUrl;
            GameClient.Start(remote);
        }

        private void OnCommandLogin(string[] args)
        {
            // uid
            string uid = BConsole.ReadLine("uid: ");
            if (string.IsNullOrEmpty(uid))
                return;

            // password
            string password = BConsole.ReadPassword("password: ");
            if (string.IsNullOrEmpty(password))
                return;

            // encoded password
            string passcode = PasswordEncoder.Encode(password);

            // send login message
            GameClient.Login(uid, passcode);
        }

        private void OnCommandDisconnect(string[] args)
        {
            GameClient.Logout();
        }

        private void OnCommandInfo(string[] args)
        {
            if (!GameClient.Connected)
            {
                BConsole.WriteLine("not connected!");
                return;
            }

            // get user info
            GameClient.Info();
        }

        private void OnCommandCoinBalance(string[] args)
        {
            if (!GameClient.Connected)
            {
                BConsole.WriteLine("not connected!");
                return;
            }

            Task.Run(async () =>
            {
                var res = await GameClient.PoA.GetBalanceAsync();
                if (null != res.balance)
                    BConsole.WriteLine(GameClient.UserAddress, "=", Coin.ToCoin(res.balance.Value), " BRC");
                else Log.Warning("error: ", res.error);
            });
        }

        private void OnCommandCoinTransfer(string[] args)
        {
            if (!GameClient.Connected)
            {
                BConsole.WriteLine("not connected!");
                return;
            }

            string to = args[0];
            decimal value = decimal.Parse(args[1]);

            // transfer
            GameClient.Transfer(to, value);
        }

        private void OnCommandCoinWithdraw(string[] args)
        {
            if (!GameClient.Connected)
            {
                BConsole.WriteLine("not connected!");
                return;
            }

            string to = args[0];
            decimal value = decimal.Parse(args[1]);

            // withdraw
            GameClient.Withdraw(to, value);
        }

        private void OnCommandCoinHistory(string[] args)
        {
            long start = args.Length > 0 ? Convert.ToInt64(args[0]) : 0;
            bool desc = args.Length > 1 ? "desc" == args[1].ToLower() : false;
            int max = args.Length > 2 ? Convert.ToInt32(args[2]) : 100;

            Task.Run(async () =>
            {
                var res = await GameClient.PoA.GetTransactionHistoryAsync(start, desc, max);
                if (!ReferenceEquals(res.txs, null))
                    BConsole.WriteLine("history=", res.txs.ToString());
                else Log.Warning("error: ", res.error);
            });

        }


        private void OnCommandShopList(string[] args)
        {
            if (!GameClient.Connected)
            {
                BConsole.WriteLine("not connected!");
                return;
            }

            // buy item
            GameClient.ShopList();
        }

        private void OnCommandShopBuy(string[] args)
        {
            if (!GameClient.Connected)
            {
                BConsole.WriteLine("not connected!");
                return;
            }

            string itemcode = args[0];

            // buy item
            GameClient.ShopBuy(itemcode);
        }

        // 마켓에 내 아이템 판매를 등록한다
        private void OnCommandMarketRegister(string[] args)
        {
            if (!GameClient.Connected)
            {
                BConsole.WriteLine("not connected!");
                return;
            }

            string itemcode = args[0];
            decimal price = decimal.Parse(args[1]);

            // 아이템 판매 등록
            GameClient.MarketRegister(itemcode, price);
        }

        // 아이템 판매 등록 취소
        private void OnCommandMarketUnregister(string[] args)
        {
            if (!GameClient.Connected)
            {
                BConsole.WriteLine("not connected!");
                return;
            }

            string order = args[0];

            // 아이템 판매 등록 취소
            GameClient.MarketUnregister(order);
        }

        private void OnCommandMarketList(string[] args)
        {
            if (!GameClient.Connected)
            {
                BConsole.WriteLine("not connected!");
                return;
            }

            // 아이템 판매 목록
            GameClient.MarketList();
        }

        private void OnCommandMarketBuy(string[] args)
        {
            if (!GameClient.Connected)
            {
                BConsole.WriteLine("not connected!");
                return;
            }

            string order = args[0];

            // 아이템 구매
            GameClient.MarketBuy(order);
        }

        private void OnCommandKeyExport(string[] args)
        {
            if (!GameClient.Connected)
            {
                BConsole.WriteLine("not connected!");
                return;
            }

            // 키 내보내기 토큰 요청
            GameClient.ExportKey();
        }
    }
}
