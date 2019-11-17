# web4b-cs-samples
web4b-cs samples

## Integrating Web4b-cs to your .NET project
* [TcpGameServer](https://github.com/bryllite/web4b-cs-samples/blob/master/Bryllite.App.Sample.TcpGameServer/README.md)
* [TcpGameClient](https://github.com/bryllite/web4b-cs-samples/blob/master/Bryllite.App.Sample.TcpGameClient/README.md)
* [Wallet](https://github.com/bryllite/web4b-cs-samples/tree/master/Bryllite.App.Sample.WalletApp)
* [GameWallet](https://github.com/bryllite/web4b-cs-samples/tree/master/Bryllite.App.Sample.GameWalletApp)

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

## Sample Wallet Usage

### commands
~~~
> help
exit, quit, shutdown - close application
h, help              - shows commands
cls                  - clear screen
config               - shows config
accounts             - show accounts list
accounts.view        - (name): show account information
accounts.lock        - (name): lock account
accounts.unlock      - (name): unlock account with password
accounts.new         - ([name]): create new account ( random private key )
accounts.del         - (name): delete account
accounts.import      - ([name]): import account with private key
accounts.export      - ([name]): export account to keystore file
getBalance           - (name | address, [number]): show account balance
getNonce             - (name | address, [number]): show account transaction count
sendTx               - (from, to, value, [gas], [nonce]): send transaction
sendTxAndWaitReceipt - (from, to, value, [gas], [nonce]): send transaction and wait receipt
getTxHistory         - (name | address, [bool:tx]): show account transaction history
~~~

### Creating account
~~~
// creating account with random private key
> accounts.new
name: testaccount1
password: ****
confirm: ****
new account(testaccount1)! address=0x677cbb6aea361ad938f56b8c165fc9ff2455f1d6

// account detail ( keystore )
> accounts.view( testaccount1 )
{
  "name": "testaccount1",
  "address": "0x677cbb6aea361ad938f56b8c165fc9ff2455f1d6",
  "secretKey": "Locked",
  "keystore": {
    "version": 3,
    "id": "976f4654-c635-4848-bdcf-b8c90e76387e",
    "address": "0x677cbb6aea361ad938f56b8c165fc9ff2455f1d6",
    "crypto": {
      "ciphertext": "0x8e155f2b5cab637e037087217cbca192384ef07b00a6d7c5ff3fd74e5c78ce52",
      "cipherparams": {
        "iv": "0x82529551b496ceeffdb07eaa4c630858"
      },
      "cipher": "aes-128-ctr",
      "kdf": "scrypt",
      "kdfparams": {
        "n": 262144,
        "p": 1,
        "r": 8,
        "salt": "0x9b38b64b75436ea6cedc3788e5bf5467780cc02ef1123249c1d0c989b8f5d221",
        "dklen": 32
      },
      "mac": "0x12ccfb67d4ddc1a2df2692a984be2f5f598c95558e6c9305406966d8e5a23a8f"
    }
  }
}
~~~

### Importing account with private key
~~~
// importing account with private key
> accounts.import( coinbase )
passphrase: ****
confirm: ****
key: ******************************************************************
coinbase(0x2b8c9ac4d8783e0c16c950e1a6c0b4f73eb7f294) imported!
~~~

### Check account balance
~~~
// show account balance with account name
> getBalance( testaccount1 )
0.00 BRC

// show coinbase account balance imported 
> getBalance( coinbase )
7,390.00 BRC
~~~

### Transaction
~~~
// shoud unlock account first with password
> accounts.unlock( testaccount1 )
password: ****
testaccount1(0x677cbb6aea361ad938f56b8c165fc9ff2455f1d6) unlocked!

// send transaction ( failed because account doesn't have enough balance )
> sendTx( testaccount1, 0xd60813a699fac537d96b0232cc844a5a7043299c, 10 )
txid=Null

// unlock coinbase account
> accounts.unlock( coinbase )
password: ****
coinbase(0x2b8c9ac4d8783e0c16c950e1a6c0b4f73eb7f294) unlocked!

// send transaction coinbase to testaccount1 100 BRC
> sendTx( coinbase, 0x677cbb6aea361ad938f56b8c165fc9ff2455f1d6, 100 )
txid=0x47572668dccad096bfb6233f89e69ad563e42782ab820ae513def80df5b7161c

// check balance ( still 0 cause tx not confirmed yet )
> getBalance( testaccount1 )
0.00 BRC

// after 30 sec...
> getBalance( testaccount1 )
100.00 BRC
~~~

### Transaction History
~~~
// tx history of testaccount1 ( txhash only )
> getTxHistory( testaccount1 )
history=[
  "0x47572668dccad096bfb6233f89e69ad563e42782ab820ae513def80df5b7161c"
]

// tx history of testaccount1
> getTxHistory( testaccount1, true )
history=[
  {
    "hash": "0x47572668dccad096bfb6233f89e69ad563e42782ab820ae513def80df5b7161c",
    "chain": "0x00",
    "version": "0x00",
    "timestamp": "0x5dd1128d",
    "from": "0x2b8c9ac4d8783e0c16c950e1a6c0b4f73eb7f294",
    "to": "0x677cbb6aea361ad938f56b8c165fc9ff2455f1d6",
    "value": "0x02540be400",
    "gas": "0x00",
    "nonce": "0x1e",
    "input": "",
    "extra": "",
    "v": "0x01",
    "r": "0xbb8c535ab3420d2039ca90f70604d0f189eb91a38899e8a82c85c579e90236c9",
    "s": "0x4e4436e14157eac27c9d74ec020c1cbe1e7f7bd76345abc81f797d997602a6e1",
    "size": "0x6b",
    "processed": "0x5dd112d1"
  }
]
~~~


## Sample Game Wallet Usage

### commands
~~~
exit, quit, shutdown - close application
h, help              - shows commands
cls                  - clear screen
config               - shows config
accounts             - show game accounts list
accounts.view        - (name): show game account information
accounts.lock        - (name): lock game account
accounts.unlock      - (name): unlock game account with password
accounts.new         - ([name]): create game account with key export token
accounts.del         - (name): delete game account
getBalance           - (name | address, [number]): show game account balance
getNonce             - (name | address, [number]): show game account transaction count
transfer             - (from, to, value, [gas], [nonce]): send in-game transaction
payout               - (from, to, value, [gas], [nonce]): send ex-game transaction
payoutAndWaitReceipt - (from, to, value, [gas], [nonce]): payout and wait receipt
getTxHistory         - (name | address, [bool:tx]): show game account transaction history
~~~

### Importing game account with key export token

Use `key.export` commands In `TcpGameClient`
~~~
// key export token will be expire after 1 minute
> key.export()
key export token: 0x01de5e53e6fd1d0ca54ec74a7fcea5b7acc2c207fcdf549f7f5d65543e3f24da

> key.export
key export token: 0x4e7a79c53156baf941cb00feeccd1eeabdc25a3e616b671714b496e9a5591767
~~~

In GameWallet commands,
~~~
// export key ( token expired )
> accounts.new( testuser )
password: ****
confirm: ****
token: 0x01de5e53e6fd1d0ca54ec74a7fcea5b7acc2c207fcdf549f7f5d65543e3f24da
error: -33000:TokenExpired

// export key with password for lock
// exported private key will be stored in keystore
// user key is encrypted with ECDH/AES
> accounts.new( testuser )
password: ****
confirm: ****
token: 0x4e7a79c53156baf941cb00feeccd1eeabdc25a3e616b671714b496e9a5591767
user key received! wait a minute while encrypt key
jade(0x91de978c7bc2ead10b32965cbf8b664f650ffe1f) imported!

// show accounts
> accounts()
{
  "name": "testuser",
  "address": "0x91de978c7bc2ead10b32965cbf8b664f650ffe1f",
  "balance": "790.00"
}
~~~


### Transfer
~~~
// transfer brc to testuser2
// should unlock first
> accounts.unlock( testuser )
password: ****
testuser(0x91de978c7bc2ead10b32965cbf8b664f650ffe1f) unlocked!

// transfer by uid
> transfer( testuser, testuser2, 10 )
txid=0x2da8c5b6210be7ad89c3fa192fea8df8330a15bc0b1a4ba8ba03c6a422d2232e

// transfer processed immediately
> getBalance( testuser )
780.00 BRC
~~~


### Payout
~~~
// payout balance to mainnet wallet ( testaccount1 )
> payoutAndWaitReceipt( testuser, 0x677cbb6aea361ad938f56b8c165fc9ff2455f1d6, 10 )

// as time goes by...

// processed tx information
tx={
  "blockNumber": "0x4e",
  "blockHash": "0xa2bdb050c10735026e635c583806cb6e545d08e951edd7d5c82db902660c1646",
  "transactionIndex": "0x00",
  "hash": "0xd01e8c63cde6d8ca5a0f5c5c65b692e31049aea3789313127e7ce0e5ac56d35e",
  "chain": "0x00",
  "version": "0x00",
  "timestamp": "0x5dd1186c",
  "from": "0x2b8c9ac4d8783e0c16c950e1a6c0b4f73eb7f294",
  "to": "0x677cbb6aea361ad938f56b8c165fc9ff2455f1d6",
  "value": "0x3b9aca00",
  "gas": "0x00",
  "nonce": "0x1f",
  "input": "",
  "extra": "0xbb6ffb853fc03f8e5a9a80b16af4e1874e1c96a95ec3a0b59c9d8fe3fa753375",
  "v": "0x01",
  "r": "0x59084b59d0e94402bfcb5ac938a16991c4b844dfe6931686dfc3a5edd71c45ae",
  "s": "0x4775aeadb1680b326767a13b6cff8fd9fb232069c79ab5a96c0fd67657ea11eb",
  "size": "0x8a"
}
~~~
