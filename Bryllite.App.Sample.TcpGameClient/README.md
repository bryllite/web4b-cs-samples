# GameClient Samples
This sample shows you how ti integrate `web4b-cs` to your .NET game client.

## Get Started

Bryllite API for .NET `web4b-cs` is provided by [nuget.org](https://www.nuget.org/packages/Bryllite.Rpc.Web4b/)

~~~c#
PM> Install-Package Bryllite.Rpc.Web4b
~~~

### Create ApiService Instance
* `BridgeUrl`: Bridge service endpoint.
* `PoAUrl`: Bridge service endpoint for PoA ( supported only websocket )
* `OnPoARequest`: callback method to process PoA Request.

~~~c#
// called when you receive poa request
// shoud returns valid accessToken from game server.
private async Task<string> OnPoARequest(string hash, string iv)
{
    // send PoA arguments to game server and receives accessToken
    return await GameConnection.GetAccessToken(hash, iv);
}

// bridge endpoint
string bridgeUrl = "ws://localhost:9627";

// poa endpoint
string poaUrl = "ws://localhost:4742";

// create api instance
BrylliteApiForGameClient ApiService = new BrylliteApiForGameClient(bridgeUrl, poaUrl, OnPoARequest);

~~~

See [Sample Code](https://github.com/bryllite/web4b-cs-samples/blob/1b046477d50363590bd0eae3b009d642da1f431c/Bryllite.App.Sample.TcpGameClient/GameClient.cs#L54)


### Start ApiService after user login
* `uid`: game user's unique identifier.
* `address`: game user's address received by the game server.

~~~c#
private void OnMessageLoginRes(TcpSession session, GameMessage message)
{
    // session code & user address
    string uid = message.Get<string>("uid");
    string address = message.Get<string>("address");

    // start bryllite api service
    ApiService.Start(uid, address);
}
~~~

See [Sample Code](https://github.com/bryllite/web4b-cs-samples/blob/1b046477d50363590bd0eae3b009d642da1f431c/Bryllite.App.Sample.TcpGameClient/GameClient.cs#L226)


### User Balance
The game user can check user's coin balance.

~~~c#
ulong balance = await ApiService.GetBalanceAsync();
~~~

See [Sample Code](https://github.com/bryllite/web4b-cs-samples/blob/1b046477d50363590bd0eae3b009d642da1f431c/Bryllite.App.Sample.TcpGameClient/GameClientApp.cs#L150)

### User Transaction History
The game user can check user's transaction history.

~~~c#
bool txidOnly = false;
// txs history
JArray history = await ApiService.GetTransactionHistoryAsync(!txidOnly);
~~~

See [Sample Code](https://github.com/bryllite/web4b-cs-samples/blob/1b046477d50363590bd0eae3b009d642da1f431c/Bryllite.App.Sample.TcpGameClient/GameClientApp.cs#L183)

