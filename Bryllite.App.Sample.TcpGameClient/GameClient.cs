using Bryllite.App.Sample.TcpGameBase;
using Bryllite.Utils.NabiLog;
using Bryllite.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json.Linq;
using Bryllite.Cryptography.Aes;
using Bryllite.Rpc.Web4b.PoA;

namespace Bryllite.App.Sample.TcpGameClient
{
    public class GameClient
    {
        private readonly GameClientApp app;

        // connection
        private TcpConnection connection;

        // is connected?
        public bool Connected => connection.Connected;

        // uid
        public string Uid { get; private set; }

        // session key
        public string SessionKey { get; private set; }

        // user address
        public string UserAddress { get; private set; }

        // bryllite poa api
        public PoAHelper PoA;

        // on connected callback
        public Action<bool> OnConnectionEstablished;

        // on disconnected callback
        public Action<int> OnConnectionLost;

        public GameClient(GameClientApp app)
        {
            this.app = app;

            // tcp game connection
            connection = new TcpConnection()
            {
                OnConnected = OnConnected,
                OnDisconnected = OnDisconnected,
                OnMessage = OnMessage,
                OnWritten = OnWritten
            };

            // map message handlers
            OnMapMessageHandlers();
        }

        private void OnMapMessageHandlers()
        {
            MapMessageHandler("error", OnMessageError);
            MapMessageHandler("login.res", OnMessageLoginRes);
            MapMessageHandler("logout.res", OnMessageLogoutRes);
            MapMessageHandler("token.res", OnMessageTokenRes);
            MapMessageHandler("info.res", OnMessageInfoRes);
            MapMessageHandler("transfer.res", OnMessageTransferRes);
            MapMessageHandler("withdraw.res", OnMessageWithdrawRes);

            MapMessageHandler("shop.list.res", OnMessageShopListRes);
            MapMessageHandler("shop.buy.res", OnMessageShopBuyRes);

            MapMessageHandler("market.register.res", OnMessageMarketRegisterRes);
            MapMessageHandler("market.unregister.res", OnMessageMarketUnregisterRes);
            MapMessageHandler("market.list.res", OnMessageMarketListRes);
            MapMessageHandler("market.buy.res", OnMessageMarketBuyRes);

            MapMessageHandler("key.export.token.res", OnMessageKeyExportTokenRes);

            MapMessageHandler("ping", OnMessagePing);
        }


        public void Start(string remote)
        {
            connection.Start(remote);
        }

        public void Stop()
        {
            // stop api service
            PoA?.Stop();

            connection.Stop();
        }

        public bool Send(byte[] message)
        {
            return connection.Send(message) > 0;
        }


        public void Login(string uid, string passcode)
        {
            // uid 저장
            Uid = uid;

            // send login message
            Send(
                new GameMessage("login.req")
                .With("uid", uid)
                .With("passcode", passcode)
            );
        }

        public void Logout()
        {
            Send(
                new GameMessage("logout.req")
                .With("session", SessionKey)
            );
        }

        public void Info()
        {
            Send(
                new GameMessage("info.req")
                .With("session", SessionKey)
            );
        }

        public void Transfer(string to, decimal value)
        {
            Send(new GameMessage("transfer.req").With("session", SessionKey).With("to", to).With("value", value));
        }

        public void Withdraw(string to, decimal value)
        {
            Send(new GameMessage("withdraw.req").With("session", SessionKey).With("to", to).With("value", value));
        }

        public void ShopList()
        {
            Send(new GameMessage("shop.list.req").With("session", SessionKey));
        }

        public void ShopBuy(string itemcode)
        {
            Send(new GameMessage("shop.buy.req").With("session", SessionKey).With("itemcode", itemcode));
        }

        public void MarketRegister(string itemcode, decimal price)
        {
            Send(new GameMessage("market.register.req").With("session", SessionKey).With("itemcode", itemcode).With("price", price));
        }

        public void MarketUnregister(string order)
        {
            Send(new GameMessage("market.unregister.req").With("session", SessionKey).With("order", order));
        }

        public void MarketList()
        {
            Send(new GameMessage("market.list.req").With("session", SessionKey));
        }

        public void MarketBuy(string order)
        {
            Send(new GameMessage("market.buy.req").With("session", SessionKey).With("order", order));
        }

        public void ExportKey()
        {
            Send(new GameMessage("key.export.token.req").With("session", SessionKey));
        }

        private void OnConnected(TcpSession session, bool connected)
        {
            OnConnectionEstablished?.Invoke(connected);
        }

        private void OnDisconnected(TcpSession session, int reason)
        {
            OnConnectionLost?.Invoke(reason);
        }

        private void OnWritten(TcpSession session, byte[] message)
        {
            //Log.Info("OnWritten! message=", message.Length);
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

        private void OnMessageError(TcpSession session, GameMessage message)
        {
            BConsole.WriteLine("error=", message);
        }

        private void OnMessageLoginRes(TcpSession session, GameMessage message)
        {
            // session code & user address
            SessionKey = message.Get<string>("scode");
            UserAddress = message.Get<string>("address");
            string poaUrl = message.Get<string>("poaUrl");
            string rpcUrl = message.Get<string>("rpcUrl");

            // start bryllite api service
            PoA = new PoAHelper(poaUrl, OnPoARequest, rpcUrl);
            PoA.Start(Uid, UserAddress);

            BConsole.WriteLine("login success! session=", SessionKey, ", address=", UserAddress, ", poaUrl=", poaUrl, ", rpcUrl=", rpcUrl);
        }

        private void OnMessageLogoutRes(TcpSession session, GameMessage message)
        {
            // 연결을 종료한다
            Stop();

            BConsole.WriteLine("logout success!");
        }

        private void OnMessageTokenRes(TcpSession session, GameMessage message)
        {
            poaToken = message.Get<string>("accessToken");
        }

        private string poaToken;

        private async Task<string> OnPoARequest(string hash, string iv)
        {
            poaToken = null;

            // game server에 access token을 요청한다.
            Send(
                new GameMessage("token.req")
                .With("session", SessionKey)
                .With("hash", hash)
                .With("iv", iv)
            );

            // 최대 5초 동안 accessToken 수신 대기한다
            var sw = Stopwatch.StartNew();
            while (string.IsNullOrEmpty(poaToken) && sw.ElapsedMilliseconds < 5000)
                await Task.Delay(10);

            Log.Debug("accessToken: ", poaToken);
            return poaToken;
        }


        private void OnMessageInfoRes(TcpSession session, GameMessage message)
        {
            // info
            BConsole.WriteLine(message.Get<JObject>("info"));
        }

        private void OnMessageTransferRes(TcpSession session, GameMessage message)
        {
            // txid
            string txid = message.Get<string>("txid");
            BConsole.WriteLine("txid=", txid);
        }

        private void OnMessageWithdrawRes(TcpSession session, GameMessage message)
        {
            // txid
            string txid = message.Get<string>("txid");
            BConsole.WriteLine("txid=", txid);
        }

        private void OnMessageShopListRes(TcpSession session, GameMessage message)
        {
            // items
            var items = message.Get<JArray>("items");
            BConsole.WriteLine("shop=", items);
        }

        private void OnMessageShopBuyRes(TcpSession session, GameMessage message)
        {
            // txid
            var item = message.Get<JObject>("item");
            BConsole.WriteLine("item=", item);
        }

        private void OnMessageMarketRegisterRes(TcpSession session, GameMessage message)
        {
            string order = message.Get<string>("order");
            BConsole.WriteLine("market registered! order=", order);
        }

        private void OnMessageMarketUnregisterRes(TcpSession session, GameMessage message)
        {
            string order = message.Get<string>("order");
            BConsole.WriteLine("market unregistered! order=", order);
        }

        private void OnMessageMarketListRes(TcpSession session, GameMessage message)
        {
            var sales = message.Get<JArray>("sales");
            if (sales.Count == 0)
            {
                BConsole.WriteLine("no sales in market");
                return;
            }

            foreach (var sale in sales)
                BConsole.WriteLine(sale);
        }

        private void OnMessageMarketBuyRes(TcpSession session, GameMessage message)
        {
            string name = message.Get<string>("itemname");
            decimal price = message.Get<decimal>("price");

            BConsole.WriteLine("market trade! item=", name, ", price=", price);
        }

        private void OnMessageKeyExportTokenRes(TcpSession session, GameMessage message)
        {
            // AES decrypt with session key
            byte[] encrypted = Hex.ToByteArray(message.Get<string>("token"));

            if (Aes256.TryDecrypt(Encoding.UTF8.GetBytes(SessionKey), encrypted, out var plain))
                BConsole.WriteLine("key export token: ", Hex.ToString(plain));
        }

        private void OnMessagePing(TcpSession session, GameMessage ping)
        {
            long timestamp = ping.Value<long>("timestamp");
            Send(new GameMessage("pong").With("timestamp", timestamp));
        }

    }
}
