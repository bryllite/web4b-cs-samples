using Bryllite.App.Sample.TcpGameBase;
using Bryllite.Cryptography.Signers;
using Bryllite.Utils.NabiLog;
using Bryllite.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Bryllite.Utils.Currency;
using System.Threading.Tasks;
using Bryllite.Rpc.Web4b.Extensions;

namespace Bryllite.App.Sample.TcpGameServer
{
    public class GameServer
    {
        // application
        private readonly GameServerApp app;

        // tcp server
        private readonly TcpServer server;

        // gamedb
        private GameDB gamedb => app.GameDB;

        // game key
        private PrivateKey gamekey => app.GameKey;

        // coinbox
        private string coinbox => app.CoinBox;

        // bryllite api service
        private BrylliteApiForGameServer api => app.ApiService;

        // user sessions( session.key, uid )
        private Dictionary<string, string> sessions = new Dictionary<string, string>();
        public IEnumerable<string> Sessions
        {
            get
            {
                lock (sessions)
                    return sessions.Keys.ToArray();
            }
        }

        public GameServer(GameServerApp app)
        {
            this.app = app;

            // tcp server
            server = new TcpServer()
            {
                OnNewConnection = OnNewConnection,
                OnConnectionLost = OnConnectionLost,
                OnReceiveMessage = OnMessage
            };

            // map message handler
            MapMessageHandler("login.req", OnMessageLoginReq);
            MapMessageHandler("logout.req", OnMessageLogoutReq);
            MapMessageHandler("token.req", OnMessageTokenReq);
            MapMessageHandler("info.req", OnMessageInfoReq);
            MapMessageHandler("transfer.req", OnMessageTransferReq);
            MapMessageHandler("payout.req", OnMessagePayoutReq);

            // shop
            MapMessageHandler("shop.list.req", OnMessageShopListReq);
            MapMessageHandler("shop.buy.req", OnMessageShopBuyReq);

            // market
            MapMessageHandler("market.register.req", OnMessageMarketRegisterReq);
            MapMessageHandler("market.unregister.req", OnMessageMarketUnregisterReq);
            MapMessageHandler("market.list.req", OnMessageMarketListReq);
            MapMessageHandler("market.buy.req", OnMessageMarketBuyReq);

        }

        public string GetUidBySession(string scode)
        {
            lock (sessions)
                return sessions.TryGetValue(scode, out string uid) ? uid : null;
        }

        public void Start(int port)
        {
            // start tcp server
            server.Start(port);

        }

        public void Stop()
        {
            // stop tcp server
            server.Stop();
        }

        private void OnNewConnection(TcpSession session)
        {
            Log.Debug("new connection established! remote=", session.Remote);
        }

        private void OnConnectionLost(TcpSession session, int reason)
        {
            Log.Debug("connection lost! remote=", session.Remote);
        }

        // message handler type
        public delegate void GameMessageHandler(TcpSession session, GameMessage message);

        // message handler table
        private Dictionary<string, GameMessageHandler> handlers = new Dictionary<string, GameMessageHandler>();

        private GameMessageHandler GetMessageHandler(string messageId)
        {
            string id = messageId.ToLower();
            lock (handlers)
                return handlers.ContainsKey(id) ? handlers[id] : null;
        }

        private void MapMessageHandler(string messageId, GameMessageHandler handler)
        {
            string id = messageId.ToLower();
            lock (handlers)
                handlers[id] = handler;
        }

        private void OnMessage(TcpSession session, byte[] received)
        {
            // parse message
            if (!GameMessage.TryParse(received, out GameMessage message))
            {
                Log.Warning("can't parse game message! received=", Hex.ToString(received.Length));
                return;
            }

            // get message handler
            var handler = GetMessageHandler(message.MessageId);
            if (null == handler)
            {
                Log.Warning("unknown message! message.id=", message.MessageId);
                return;
            }

            // invoke message handler
            handler.Invoke(session, message);
        }

        private void OnMessageLoginReq(TcpSession session, GameMessage message)
        {
            string uid = message.Get<string>("uid");
            string passcode = message.Get<string>("passcode");

            var user = gamedb.Users.Select(uid, passcode);
            if (null == user)
            {
                session.Write(new GameMessage("error").With("message", "unknown uid or password mismatch"));
                return;
            }

            // session key
            string scode = session.ID;
            lock (sessions)
                sessions[scode] = uid;

            // login ok
            session.Write(
                new GameMessage("login.res")
                .With("scode", scode)
                .With("address", api.GetUserAddress(uid))
            );
        }

        private void OnMessageLogoutReq(TcpSession session, GameMessage message)
        {
            string scode = message.Get<string>("session");
            string uid = GetUidBySession(scode);
            if (string.IsNullOrEmpty(uid) || scode != session.ID)
            {
                session.Write(new GameMessage("error").With("message", "unknown session key"));
                return;
            }

            lock (sessions)
                sessions.Remove(scode);

            // logout ok
            session.Write(
                new GameMessage("logout.res")
            );
        }

        private void OnMessageTokenReq(TcpSession session, GameMessage message)
        {
            string scode = message.Get<string>("session");
            string uid = GetUidBySession(scode);
            if (string.IsNullOrEmpty(uid) || scode != session.ID)
            {
                session.Write(new GameMessage("error").With("message", "unknown session key"));
                return;
            }

            // access token
            string accessToken = api.GetPoAToken(uid, message.Get<string>("hash"), message.Get<string>("iv"));

            // access token
            session.Write(
                new GameMessage("token.res")
                .With("accessToken", accessToken)
            );
        }

        private void OnMessageInfoReq(TcpSession session, GameMessage message)
        {
            string scode = message.Get<string>("session");
            string uid = GetUidBySession(scode);
            if (string.IsNullOrEmpty(uid) || scode != session.ID)
            {
                session.Write(new GameMessage("error").With("message", "unknown session key"));
                return;
            }

            // select user
            var user = gamedb.Users.Select(uid);
            if (ReferenceEquals(user, null))
            {
                session.Write(new GameMessage("error").With("message", "user data not found"));
                return;
            }

            // user information
            JObject info = new JObject();
            info.Put("uid", uid);
            info.Put("rdate", user.RegisterDate);

            // user balance & nonce
            string address = api.GetUserAddress(uid);
            ulong balance = api.GetBalanceAsync(address).Result;
            ulong nonce = api.GetNonceAsync(address, false).Result;

            info.Put("address", address);
            info.Put("balance", Coin.ToCoin(balance));
            info.Put("nonce", nonce);

            // user inventory
            JArray inven = new JArray();
            foreach (var code in gamedb.Inventories.Select(uid))
            {
                var item = gamedb.Items.Select(code);
                inven.Add(JObject.Parse(item.ToString()));
            }

            info.Put("inventory", inven);

            session.Write(
                new GameMessage("info.res")
                .With("info", info)
            );
        }


        private void OnMessageTransferReq(TcpSession session, GameMessage message)
        {
            string scode = message.Get<string>("session");
            string uid = GetUidBySession(scode);
            if (string.IsNullOrEmpty(uid) || scode != session.ID)
            {
                session.Write(new GameMessage("error").With("message", "unknown session key"));
                return;
            }

            string signer = api.GetUserKey(uid);
            string to = api.GetUserAddress(message.Get<string>("to"));
            decimal value = message.Get<decimal>("value");

            // transfer
            string txid = api.TransferAsync(signer, to, value, 0).Result;
            session.Write(new GameMessage("transfer.res").With("txid", txid));
        }

        private void OnMessagePayoutReq(TcpSession session, GameMessage message)
        {
            string scode = message.Get<string>("session");
            string uid = GetUidBySession(scode);
            if (string.IsNullOrEmpty(uid) || scode != session.ID)
            {
                session.Write(new GameMessage("error").With("message", "unknown session key"));
                return;
            }

            string signer = api.GetUserKey(uid);
            string to = message.Get<string>("to");
            decimal value = message.Get<decimal>("value");

            // payout
            string txid = api.PayoutAsync(signer, to, value, 0).Result;
            session.Write(new GameMessage("payout.res").With("txid", txid));
        }

        private void OnMessageShopListReq(TcpSession session, GameMessage message)
        {
            string scode = message.Get<string>("session");
            string uid = GetUidBySession(scode);
            if (string.IsNullOrEmpty(uid) || scode != session.ID)
            {
                session.Write(new GameMessage("error").With("message", "unknown session key"));
                return;
            }

            var items = new JArray();
            foreach (var item in gamedb.Items.All)
                items.Add(JObject.Parse(item.ToString()));

            session.Write(new GameMessage("shop.list.res").With("items", items));
        }

        private void OnMessageShopBuyReq(TcpSession session, GameMessage message)
        {
            string scode = message.Get<string>("session");
            string uid = GetUidBySession(scode);
            if (string.IsNullOrEmpty(uid) || scode != session.ID)
            {
                session.Write(new GameMessage("error").With("message", "unknown session key"));
                return;
            }

            // item
            var item = gamedb.Items.Select(message.Get<string>("itemcode"));
            if (null == item)
            {
                session.Write(new GameMessage("error").With("message", "unknown item code"));
                return;
            }

            // user's balance -> coinbox
            string signer = api.GetUserKey(uid);
            string txid = api.TransferAsync(signer, coinbox, item.Price, 0).Result;
            if (string.IsNullOrEmpty(txid))
            {
                session.Write(new GameMessage("error").With("message", "txid not found"));
                return;
            }

            // item to user inventory
            var inventory = gamedb.Inventories.Select(uid);
            inventory.Add(item.Code);
            gamedb.Inventories.Update(uid, inventory);

            session.Write(new GameMessage("shop.buy.res").With("item", JObject.FromObject(item)));
        }

        private void OnMessageMarketRegisterReq(TcpSession session, GameMessage message)
        {
            string scode = message.Get<string>("session");
            string uid = GetUidBySession(scode);
            if (string.IsNullOrEmpty(uid) || scode != session.ID)
            {
                session.Write(new GameMessage("error").With("message", "unknown session key"));
                return;
            }

            // itemcode & price
            string itemcode = message.Get<string>("itemcode");
            decimal price = message.Get<decimal>("price");

            // item info
            var item = gamedb.Items.Select(itemcode);
            if (null == item)
            {
                session.Write(new GameMessage("error").With("message", "unknown item code"));
                return;
            }

            // remove item from user inventory
            var inven = gamedb.Inventories.Select(uid);
            if (!inven.Remove(itemcode))
            {
                session.Write(new GameMessage("error").With("message", "not owned item"));
                return;
            }
            gamedb.Inventories.Update(uid, inven);

            // register market
            var sales = new Sales(uid, itemcode, item.Name, price);
            gamedb.Market.Insert(sales);

            session.Write(new GameMessage("market.register.res").With("order", sales.Order));
        }

        private void OnMessageMarketUnregisterReq(TcpSession session, GameMessage message)
        {
            string scode = message.Get<string>("session");
            string uid = GetUidBySession(scode);
            if (string.IsNullOrEmpty(uid) || scode != session.ID)
            {
                session.Write(new GameMessage("error").With("message", "unknown session key"));
                return;
            }

            // order no.
            string order = message.Get<string>("order");

            // sales
            var sales = gamedb.Market.Select(order);
            if (null == sales)
            {
                session.Write(new GameMessage("error").With("message", "unknown order"));
                return;
            }

            // owner?
            if (sales.Seller != uid)
            {
                session.Write(new GameMessage("error").With("message", "not owner"));
                return;
            }

            // remove sales from market
            if (!gamedb.Market.Delete(order))
            {
                session.Write(new GameMessage("error").With("message", "not cancellable"));
                return;
            }

            // get back to user
            var inven = gamedb.Inventories.Select(uid);
            inven.Add(sales.ItemCode);
            gamedb.Inventories.Update(uid, inven);

            session.Write(new GameMessage("market.unregister.res").With("order", order));
        }

        private void OnMessageMarketListReq(TcpSession session, GameMessage message)
        {
            string scode = message.Get<string>("session");
            string uid = GetUidBySession(scode);
            if (string.IsNullOrEmpty(uid) || scode != session.ID)
            {
                session.Write(new GameMessage("error").With("message", "unknown session key"));
                return;
            }

            var sales = new JArray();
            foreach (var sale in gamedb.Market.All)
                sales.Add(JObject.Parse(sale.ToString()));

            session.Write(new GameMessage("market.list.res").With("sales", sales));
        }

        private void OnMessageMarketBuyReq(TcpSession session, GameMessage message)
        {
            string scode = message.Get<string>("session");
            string uid = GetUidBySession(scode);
            if (string.IsNullOrEmpty(uid) || scode != session.ID)
            {
                session.Write(new GameMessage("error").With("message", "unknown session key"));
                return;
            }

            Task.Run(async () =>
            {
                // order no.
                string order = message.Get<string>("order");
                var sales = gamedb.Market.Select(order);
                if (null == sales)
                {
                    session.Write(new GameMessage("error").With("message", "unknown order"));
                    return;
                }

                // payment = price - commission
                ulong price = Coin.ToBeryl(sales.Price);
                ulong commission = (ulong)(price * app.Commission);
                ulong payment = price - commission;

                BConsole.WriteLine("price=", price);
                BConsole.WriteLine("commission=", commission);
                BConsole.WriteLine("payment=", payment);

                string signer = api.GetUserKey(uid);
                string seller = api.GetUserAddress(sales.Seller);

                // send tx
                string txid = await api.TransferAsync(signer, seller, Coin.ToCoin(payment), Coin.ToCoin(commission));
                if (string.IsNullOrEmpty(txid))
                {
                    session.Write(new GameMessage("error").With("message", "tx failed"));
                    return;
                }

                // wait for tx completed
                string hash = await api.WaitForTransactionConfirm(txid, 1000);
                if (string.IsNullOrEmpty(hash))
                {
                    session.Write(new GameMessage("error").With("message", "timeout"));
                    return;
                }

                // remove sales
                gamedb.Market.Delete(order);

                // item to buyer
                var inven = gamedb.Inventories.Select(uid);
                inven.Add(sales.ItemCode);
                gamedb.Inventories.Update(uid, inven);

                session.Write(new GameMessage("market.buy.res").With("itemname", sales.ItemName).With("price", sales.Price));
            });
        }
    }
}
