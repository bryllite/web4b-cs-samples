# GameServer Samples
This sample shows you how to integrate web4b-cs to your Game Server.

## Get Started

Bryllite API for .NET `web4b-cs` is provided by [nuget.org](https://www.nuget.org/packages/Bryllite.Rpc.Web4b/)

~~~c#
PM> Install-Package Bryllite.Rpc.Web4b
~~~

### Create ApiService Instance

* `BridgeUrl`: Bridge service endpoint.
* `GameKey`: Game server private key.
* `ShopAddress`: Game server shop address. 

~~~c#
string bridgeUrl = "ws://localhost:9627";
string gameKey = "{gameKey}";
string shopAddress = "{shopAddress}";

BrylliteApiForGameServer ApiService = new BrylliteApiForGameServer(bridgeUrl, gameKey, shopAddress);
~~~

See [Sample Code](https://github.com/bryllite/web4b-cs-samples/blob/ad8315f09662caa67f116e8763d548167fa44800/Bryllite.App.Sample.TcpGameServer/GameServerApp.cs#L63)


### GameServer Balance
* `GameServerAddress`: GameServer address.

~~~c#
string coinbase = new PrivateKey(gameKey).Address;
ulong balance = await ApiService.GetBalanceAsync(coinbase);
~~~

See [Sample Code](https://github.com/bryllite/web4b-cs-samples/blob/ad8315f09662caa67f116e8763d548167fa44800/Bryllite.App.Sample.TcpGameServer/GameServerApp.cs#L235)

### GameUser Balance
* `uid`: Game user's unique identifier.
* `address`: Game User's address.

**Get User Address**

[BrylliteApiForGameServer.GetUserAddress()](https://github.com/bryllite/web4b-cs/blob/daadad9c061912269aae32b46ad965a91c046b79/Rpc/Bryllite.Rpc.Web4b/Extensions/BrylliteApiForGameServer.cs#L47)
~~~c#
// uid to address
public string GetUserAddress(string uid)
{
    return gameKey.CKD(uid).Address;
}

public async Task<ulong> GetUserBalance(string uid)
{
    string address = GetUserAddress(uid);
    return await ApiService.GetBalanceAsync(address);
}
~~~

### Issuing coins to game users
The game server can issue coins to game users within the coinbase balance.
Use `BrylliteApiForGameServer.TransferAsync()` method.

~~~c#
public bool Issue(string uid, decimal coins)
{
    string txid = await ApiService.TransferAsync(GameKey, GetUserAddress(uid), Coin.ToBeryl(coins));
    return !string.IsNullOrEmpty(txid);
}
~~~
See [Sample Code](https://github.com/bryllite/web4b-cs-samples/blob/ad8315f09662caa67f116e8763d548167fa44800/Bryllite.App.Sample.TcpGameServer/GameServerApp.cs#L240)

### Transaction between game users
The game user(client) doesn't have a private key.
So the game server should handle user's transfer request instead.
When game user makes a transfer request to the game server, the transfer is processed after user authentication.

~~~c#
private async Task OnMessageTransferReq(TcpSession session, GameMessage message)
{
    string scode = message.Get<string>("session");
    string uid = GetUidBySession(scode);
    if (string.IsNullOrEmpty(uid) || scode != session.ID)
    {
        session.Write(new GameMessage("error").With("message", "unknown session key"));
        return;
    }

    // user's private key
    string signer = ApiService.GetUserKey(uid);
    // recipient address
    string to = ApiService.GetUserAddress(message.Get<string>("to"));
    // value to transfer
    decimal value = message.Get<decimal>("value");

    // transfer
    string txid = await ApiService.TransferAsync(signer, to, value, 0);

    // ack
    session.Write(new GameMessage("transfer.res").With("txid", txid));
}
~~~
See [Sample Code](https://github.com/bryllite/web4b-cs-samples/blob/ad8315f09662caa67f116e8763d548167fa44800/Bryllite.App.Sample.TcpGameServer/GameServer.cs#L262
)

### Withdraw coin outside the game
The game user's balance is only valid within the game.
In order to use it on a mainnet, user must withdraw it to an account outside the game.
The game server should handle user's payout request instead after authentication.

~~~c#
private async Task OnMessagePayoutReq(TcpSession session, GameMessage message)
{
    string scode = message.Get<string>("session");
    string uid = GetUidBySession(scode);
    if (string.IsNullOrEmpty(uid) || scode != session.ID)
    {
        session.Write(new GameMessage("error").With("message", "unknown session key"));
        return;
    }

    // user's private key
    string signer = ApiService.GetUserKey(uid);
    // mainnet address
    string to = message.Get<string>("to");
    // value to payout
    decimal value = message.Get<decimal>("value");

    // payout
    string txid = await ApiService.PayoutAsync(signer, to, value, 0);

    // ack
    session.Write(new GameMessage("payout.res").With("txid", txid));
}
~~~
See [Sample Code](https://github.com/bryllite/web4b-cs-samples/blob/ad8315f09662caa67f116e8763d548167fa44800/Bryllite.App.Sample.TcpGameServer/GameServer.cs#L281)

### Make sure payout request is complete
Unlike transfer requests between game users (In-Game Tx), payout requests are not processed immediately.
It takes some time for the block to be created on the Bryllite mainnet and the transaction to be included.
Use `BrylliteApiForGameServer.GetTransactionReceiptAsync()` method to check.

~~~c#
public async Task<JObject> WaitForTransactionConfirm(string txid, int timeout = 0)
{
    try
    {
        var sw = Stopwatch.StartNew();

        JObject receipt = null;
        while (receipt == null)
        {
            if (timeout > 0 && sw.ElapsedMilliseconds >= timeout)
                break;

            await Task.Delay(100);

            receipt = await GetTransactionReceiptAsync(txid);
        }

        return receipt;
    }
    catch (Exception ex)
    {
        Log.Warning("exception! ex=", ex);
        return null;
    }
}
~~~
See [Sample Code](https://github.com/bryllite/web4b-cs/blob/daadad9c061912269aae32b46ad965a91c046b79/Rpc/Bryllite.Rpc.Web4b/Extensions/BrylliteApiForGameServer.cs#L196)


### Buying items on game shop
The game user can use coins to buy items provided by the game service.
When a game user buy an item, the purchase amount is credited to `shopAddress`.

~~~c#
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

    // user's balance -> shopAddress
    string signer = ApiService.GetUserKey(uid);
    string txid = ApiService.TransferAsync(signer, shopAddress, item.Price, 0).Result;
    if (string.IsNullOrEmpty(txid))
    {
        session.Write(new GameMessage("error").With("message", "can't buy item"));
        return;
    }

    // item to user inventory
    var inventory = gamedb.Inventories.Select(uid);
    inventory.Add(item.Code);
    gamedb.Inventories.Update(uid, inventory);

    session.Write(new GameMessage("shop.buy.res").With("item", JObject.FromObject(item)));
}
~~~
See [Sample Code](https://github.com/bryllite/web4b-cs-samples/blob/ad8315f09662caa67f116e8763d548167fa44800/Bryllite.App.Sample.TcpGameServer/GameServer.cs#L317)


### Selling items to other game user
The game users can sell items acquired in the game by coins to other users.

~~~c#
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

        string signer = ApiService.GetUserKey(uid);
        string seller = ApiService.GetUserAddress(sales.Seller);

        // send payment to seller
        // commission to coinbase
        string txid = await ApiService.TransferAsync(signer, seller, Coin.ToCoin(payment), Coin.ToCoin(commission));
        if (string.IsNullOrEmpty(txid))
        {
            session.Write(new GameMessage("error").With("message", "tx failed"));
            return;
        }

        // wait for tx completed
        string hash = await ApiService.WaitForTransactionConfirm(txid, 1000);
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
~~~
See [Sample Code](https://github.com/bryllite/web4b-cs-samples/blob/ad8315f09662caa67f116e8763d548167fa44800/Bryllite.App.Sample.TcpGameServer/GameServer.cs#L450)

### Issuing AccessToken to game client for PoA(proof of attendance)
Game client receives request for prove game attendance every block time with `hash` and `iv` arguments.
When the client sends these arguments, the game server should issue a valid `accessToken` after authentication.

~~~c#
private void OnMessageTokenReq(TcpSession session, GameMessage message)
{
    string scode = message.Get<string>("session");
    string uid = GetUidBySession(scode);
    if (string.IsNullOrEmpty(uid) || scode != session.ID)
    {
        session.Write(new GameMessage("error").With("message", "unknown session key"));
        return;
    }

    // block hash
    string hash = message.Get<string>("hash");

    // initial vector for user
    string iv = message.Get<string>("iv");

    // access token
    string accessToken = ApiService.GetPoAToken(uid, hash, iv);

    // access token
    session.Write(
        new GameMessage("token.res")
        .With("accessToken", accessToken)
    );
}
~~~
See [Sample Code](https://github.com/bryllite/web4b-cs-samples/blob/ad8315f09662caa67f116e8763d548167fa44800/Bryllite.App.Sample.TcpGameServer/GameServer.cs#L193)

