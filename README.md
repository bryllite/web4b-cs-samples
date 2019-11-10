# web4b-cs-samples
web4b-cs samples

## Integrating Web4b-cs to your .NET project
* [GameServer](https://github.com/bryllite/web4b-cs-samples/blob/master/Bryllite.App.Sample.TcpGameServer/README.md)
* [GameClient](https://github.com/bryllite/web4b-cs-samples/blob/master/Bryllite.App.Sample.TcpGameClient/README.md)

## Sample GameServer Usage

### commands
~~~
> help
exit, quit, shutdown - close application
h, help              - show commands
cls                  - clear screen
config               - shows config
server.start         - ([port]): start game server with port
server.stop          - stop game server
server.sessions      - shows connected sessions
users                - shows game server's account list
user.new             - ([uid]): create new user account
user.view            - (uid): shows user account
user.del             - (uid): delete user account
items                - shows item list on shop
item.new             - (name, price): new item on shop
item.del             - (name): delete item on shop
item.issue           - (uid, name): issue item on shop to game user
item.withdraw        - (uid, name): withdraw item from game user
coinbase             - shows coinbase account information
coin.issue           - (uid, balance): issue coin to game user
coin.withdraw        - (uid, balance): withdraw coin from game user
~~~

### Creating user accounts
~~~
// creating new user with userid
> user.new('testuser')
password: ****
password confirm: ****
creating new user(testuser)=True

> user.new('testuser2')
password: ****
password confirm: ****
creating new user(testuser2)=True

// show created user information
> user.view('testuser')
testuser={
  "id": "testuser",
  "rdate": "2019-11-10 PM 11:27:39",
  "passhash": "0x4c8f18581c0167eb90a761b4a304e009b924f03b619a0c0e8ea3adfce20aee64",
  "address": "0x80b08a87314adb50326581e64dd117f8f9b43209",
  "balance": 0,
  "nonce": 0,
  "inventory": []
}
~~~
User address is deterministic by `gamekey` and user `id`.

### Creating items on shop
~~~
// creating new item on shop
> item.new('Excalibur', 10.0)
item(0x457863616c69627572) registered!

// show items list on shop
> items
{
  "code": "0x457863616c69627572",
  "name": "Excalibur",
  "price": 10.0
}
~~~

### Issuing coin to game user
~~~
// reward game users
> coin.issue('testuser', 100)
txid=0x7642ed8d0880bd742dcca8c1b2bf2575fbc19ce98bd3ed7213ae30cda245cb93
coinbase.balance=390040000000
testuser(0x80b08a87314adb50326581e64dd117f8f9b43209).balance=10000000000

> coin.issue('testuser2', 100)
txid=0x8d1de655c11cc79c9532de1afb23b206fa416623a28ce124310b4d1350a3c805
coinbase.balance=380040000000
testuser2(0xdb0c9ef25d9e5d436b00eecf1fbe755a8d6db1fd).balance=10000000000

// check user balance
> user.view('testuser')
testuser={
  "id": "testuser",
  "rdate": "2019-11-10 PM 11:27:39",
  "passhash": "0x4c8f18581c0167eb90a761b4a304e009b924f03b619a0c0e8ea3adfce20aee64",
  "address": "0x80b08a87314adb50326581e64dd117f8f9b43209",
  "balance": 10000000000,
  "nonce": 0,
  "inventory": []
}
~~~

## Sample GameClient Usage

### Commands
~~~
> help
exit, quit, shutdown - close application
h, help              - shows commands
cls                  - clear screen
config               - shows config
connect              - ([remote]): connect to game server
disconnect           - disconnect with game server
login                - ([uid]): request to login
logout               - request to logout
info                 - shows my information
coin.balance         - shows my coin balance
coin.transfer        - (toUid, value): transfer coin to other game user
coin.payout          - (to, value): payout coin to mainnet address
coin.history         - ([bool]): show transaction history
shop.list            - show items list on shop
shop.buy             - (itemcode): buy item on shop
market.register      - (itemcode, price): register item to market for sales
market.unregister    - (order): unregister item from market
market.buy           - (order): buy item from market
market.list          - shows registered items on market for sales
~~~

### Connect and Login
~~~
// just connect
> Connect()
[23:43:05.498] info/GameClient.OnConnected: OnConnected! connected=True

// request login with uid / password
> login
uid: testuser
password: ****
login success! session=dgp25ZQeazfuq/FLDQf1GsxdTpPP9VwRDOJtb5TKZsQ=, address=0x80b08a87314adb50326581e64dd117f8f9b43209
[23:43:48.902] debug/<ConnectAsync>d__16.MoveNext: connected!
~~~

### Check balance
~~~
// show game info ( receives from game server )
> info()
{
  "uid": "testuser",
  "rdate": "2019-11-10 PM 11:27:39",
  "address": "0x80b08a87314adb50326581e64dd117f8f9b43209",
  "balance": 100.0,
  "nonce": 0,
  "inventory": []
}

// show coin balance ( received from bridge service )
> coin.balance()
0x80b08a87314adb50326581e64dd117f8f9b43209=100 BRC
~~~

### Buying items from shop
~~~
// shows item list on shop
> shop.list()
shop=[
  {
    "code": "0x457863616c69627572",
    "name": "Excalibur",
    "price": 10.0
  }
]

// buy item with item code
> shop.buy('0x457863616c69627572')
item={
  "code": "0x457863616c69627572",
  "name": "Excalibur",
  "price": 10.0
}

// check coin balance and inventory
> info()
{
  "uid": "testuser",
  "rdate": "2019-11-10 PM 11:27:39",
  "address": "0x80b08a87314adb50326581e64dd117f8f9b43209",
  "balance": 90.0,
  "nonce": 1,
  "inventory": [
    {
      "code": "0x457863616c69627572",
      "name": "Excalibur",
      "price": 10.0
    }
  ]
}

~~~

### Selling items to market
~~~
// register item to sell on market with price
> market.register('0x457863616c69627572', 10.0)
market registered! order=0x9e31957333a7b46d6d2ea5dc56944a15a89efb7e

// market list
> market.list()
{
  "order": "0x9e31957333a7b46d6d2ea5dc56944a15a89efb7e",
  "seller": "testuser",
  "itemcode": "0x457863616c69627572",
  "itemname": "Excalibur",
  "price": 10.0
}
~~~

### Buying items from market

~~~
> connect
[23:58:49.280] info/GameClient.OnConnected: OnConnected! connected=True

// login as other user
> login testuser2
password: ****
login success! session=a5RGHyFFv/dU78z8i4fpDOByPlGvCAOqIhb/C8/Sicw=, address=0xdb0c9ef25d9e5d436b00eecf1fbe755a8d6db1fd
[23:58:55.050] debug/<ConnectAsync>d__16.MoveNext: connected!

// check market list
> market.list()
{
  "order": "0x9e31957333a7b46d6d2ea5dc56944a15a89efb7e",
  "seller": "testuser",
  "itemcode": "0x457863616c69627572",
  "itemname": "Excalibur",
  "price": 10.0
}

// check my coin balance and inventory
> info()
{
  "uid": "testuser2",
  "rdate": "2019-11-10 PM 11:56:54",
  "address": "0xdb0c9ef25d9e5d436b00eecf1fbe755a8d6db1fd",
  "balance": 100.0,
  "nonce": 0,
  "inventory": []
}

// buy item from market
> market.buy('0x9e31957333a7b46d6d2ea5dc56944a15a89efb7e')
market trade! item=Excalibur, price=10

// check market list
> market.list()
no sales in market

// check my coin balance and inventory
> info()
{
  "uid": "testuser2",
  "rdate": "2019-11-10 PM 11:56:54",
  "address": "0xdb0c9ef25d9e5d436b00eecf1fbe755a8d6db1fd",
  "balance": 90.0,
  "nonce": 1,
  "inventory": [
    {
      "code": "0x457863616c69627572",
      "name": "Excalibur",
      "price": 10.0
    }
  ]
}
~~~

