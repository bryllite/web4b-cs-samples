using Bryllite.Cryptography.Signers;
using Bryllite.Extensions;
using Bryllite.Rpc.Web4b;
using Bryllite.Utils.AppBase;
using Bryllite.Utils.Currency;
using Bryllite.Utils.NabiLog;
using Bryllite.Utils.Ntp;
using Bryllite.Utils.Pbkdf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Bryllite.App.Sample.WalletApp
{
    public class WalletApp : AppBase
    {
        // web4b api
        private Be4Api web4b;

        // wallet service
        private WalletService wallets;


        public WalletApp(string[] args) : base(args)
        {
            // web4b api
            web4b = new Be4Api(config["web4b"].Value<string>("provider"));

            // wallets
            wallets = new WalletService();

            Task.Run(async () =>
            {
                await web4b.GetTimeAsync();
            });

            // map command handlers
            OnMapCommandHandlers();
        }

        public override bool OnAppInitialize()
        {
            Log.Info("Hello, Bryllite!");

            // NTP synchronize
            NetTime.Synchronize();

            // start wallet service
            wallets.Start();

            return true;
        }

        public override void OnAppCleanup()
        {
            wallets.Stop();

            Log.Info("Bye, Bryllite!");
        }

        private void OnMapCommandHandlers()
        {
            // account management
            MapCommandHandler("accounts", "계좌 목록을 출력합니다", OnCommandAccounts);
            MapCommandHandler("accounts.view", "(name): 계좌의 세부 정보를 출력합니다", OnCommandAccountsView);
            MapCommandHandler("accounts.lock", "(name): 계좌를 잠금니다", OnCommandAccountsLock);
            MapCommandHandler("accounts.unlock", "(name): 계좌의 잠금을 해제합니다", OnCommandAccountsUnlock);
            MapCommandHandler("accounts.new", "([name]): 새로운 계좌를 생성합니다", OnCommandAccountsNew);
            MapCommandHandler("accounts.del", "(name): 계좌를 삭제합니다", OnCommandAccountsDel);
            MapCommandHandler("accounts.import", "([name]): 키를 가져옵니다", OnCommandAccountsImport);
            MapCommandHandler("accounts.export", "([name]): 키를 내보냅니다", OnCommandAccountsExport);

            // web4b api
            MapCommandHandler("getBalance", "(name | address, [number]): 계좌의 잔고를 조회합니다", OnCommandGetBalance);
            MapCommandHandler("getNonce", "(name | address, [number]): 계좌의 nonce를 조회합니다", OnCommandGetNonce);
            MapCommandHandler("createTx", "(from, to, value, [gas], [nonce]): 계좌 이체 트랜잭션의 Rlp를 출력합니다. from=name, to=address", OnCommandCreateTx);
            MapCommandHandler("sendTx", "(from, to, value, [gas], [nonce]): 계좌 이체를 수행합니다.", OnCommandSendTx);
            MapCommandHandler("sendTxAndWaitReceipt", "(from, to, value, [gas], [nonce]): 계좌 이체를 실행하고 처리될 때까지 대기합니다.", OnCommandSendTxAndWaitReceipt);
            MapCommandHandler("getTxHistory", "(name | address, [bool:tx]): 계좌의 트랜잭션 내역을 조회합니다", OnCommandGetTxHistory);
        }

        private void OnCommandAccounts(string[] args)
        {
            Task.Run(async () =>
            {
                foreach (var entry in wallets)
                {
                    var o = new JObject();
                    string name = entry.Key;
                    string address = entry.Value.Address;

                    o.Put("name", name);
                    o.Put("address", address);
                    o.Put("balance", Coin.ToCoin(await web4b.GetBalanceAsync(address, Be4Api.LATEST) ?? 0).ToString("N"));

                    BConsole.WriteLine(o);
                }
            });
        }

        private void OnCommandAccountsView(string[] args)
        {
            string name = args[0];
            if (!wallets.TryGetValue(name, out Account account))
            {
                BConsole.WriteLine("account (", name, ") not found");
                return;
            }

            string key = account.Key;
            if (string.IsNullOrEmpty(key)) key = "Locked";

            var o = new JObject();
            o.Put("name", name);
            o.Put<string>("address", account.Address);
            o.Put("secretKey", key);
            o.Put("keystore", account.KeyStore);

            BConsole.WriteLine(o);
        }


        private void OnCommandAccountsUnlock(string[] args)
        {
            string name = args[0];
            if (!wallets.TryGetValue(name, out Account account))
            {
                BConsole.WriteLine("account (", name, ") not found");
                return;
            }

            if (!account.Locked)
            {
                BConsole.WriteLine("account (", name, ") already unlocked");
                return;
            }

            string password = BConsole.ReadPassword("password: ");
            if (account.Unlock(password))
                BConsole.WriteLine(name, "(", account.Address, ") unlocked!");
        }

        private void OnCommandAccountsLock(string[] args)
        {
            string name = args[0];
            if (!wallets.TryGetValue(name, out Account account))
            {
                BConsole.WriteLine("account (", name, ") not found");
                return;
            }

            if (account.Locked)
            {
                BConsole.WriteLine("account (", name, ") already locked");
                return;
            }

            account.Lock();
            BConsole.WriteLine(name, "(", account.Address, ") locked!");
        }

        private void OnCommandAccountsNew(string[] args)
        {
            string name = args.Length > 0 ? args[0] : BConsole.ReadLine("name: ");
            if (string.IsNullOrEmpty(name)) return;

            if (wallets.Contains(name))
            {
                BConsole.WriteLine("account name already existing!");
                return;
            }

            string password = BConsole.ReadPassword("password: ");
            if (string.IsNullOrEmpty(password))
                return;

            string confirm = BConsole.ReadPassword("confirm: ");
            if (password != confirm)
            {
                BConsole.WriteLine("password mismatch");
                return;
            }

            // encrypted key
            string keystore = KeyStoreService.EncryptKeyStoreV3(PrivateKey.CreateKey(), password);

            var account = wallets.Add(name, keystore);
            BConsole.WriteLine("new account(", name, ")! address=", account.Address);
        }

        private void OnCommandAccountsDel(string[] args)
        {
            string name = args[0];
            if (!wallets.TryGetValue(name, out Account account))
            {
                BConsole.WriteLine("account (", name, ") not found");
                return;
            }

            if (account.Locked)
            {
                string passphrase = BConsole.ReadPassword("password: ");
                if (!account.Unlock(passphrase))
                {
                    BConsole.WriteLine("can't unlock keystore");
                    return;
                }
            }

            wallets.Delete(name);
            BConsole.WriteLine("account(", name, ") removed! address=", account.Address);
        }

        private void OnCommandAccountsImport(string[] args)
        {
            string name = args.Length > 0 ? args[0] : BConsole.ReadLine("name: ");
            if (string.IsNullOrEmpty(name))
                return;

            if (wallets.Contains(name))
            {
                BConsole.WriteLine("account name already existing!");
                return;
            }

            // password
            string passphrase = BConsole.ReadPassword("passphrase: ");
            if (string.IsNullOrEmpty(passphrase))
                return;

            // confirm
            string confirm = BConsole.ReadPassword("confirm: ");
            if (passphrase != confirm)
            {
                BConsole.WriteLine("password mismatch");
                return;
            }

            // private key
            PrivateKey key = BConsole.ReadPassword("key: ");

            // keystore
            string keystore = KeyStoreService.EncryptKeyStoreV3(key, passphrase);

            // add account
            var account = wallets.Add(name, keystore);

            BConsole.WriteLine(name, "(", account.Address, ") imported!");
        }

        private void OnCommandAccountsExport(string[] args)
        {
            string name = args.Length > 0 ? args[0] : BConsole.ReadLine("name: ");
            if (!wallets.TryGetValue(name, out Account account))
            {
                BConsole.WriteLine("account name (", name, ") not found");
                return;
            }

            string file = "export/" + name + "-" + account.Address + ".json";
            file.MakeSureDirectoryPathExists();
            File.WriteAllText(file, account.KeyStore.ToString());

            BConsole.WriteLine(name, "(", account.Address, ") exported! file=", file);
        }

        private void OnCommandGetBalance(string[] args)
        {
            Task.Run(async () =>
            {
                string address = args[0].IsHexString() ? args[0] : wallets.TryGetValue(args[0], out var account) ? (string)account.Address : null;
                string number = args.Length > 1 ? args[1] : Be4Api.LATEST;

                if (string.IsNullOrEmpty(address))
                {
                    BConsole.WriteLine("address not found");
                    return;
                }

                decimal balance = Coin.ToCoin(await web4b.GetBalanceAsync(address, number) ?? 0);
                BConsole.WriteLine(Color.DarkGreen, balance.ToString("N"), " BRC");
            });
        }

        private void OnCommandGetNonce(string[] args)
        {
            Task.Run(async () =>
            {
                string address = args[0].IsHexString() ? args[0] : wallets.TryGetValue(args[0], out var account) ? (string)account.Address : null;
                string number = args.Length > 1 ? args[1] : Be4Api.LATEST;

                if (string.IsNullOrEmpty(address))
                {
                    BConsole.WriteLine("address not found");
                    return;
                }

                ulong nonce = await web4b.GetTransactionCountAsync(address, number) ?? 0;
                BConsole.WriteLine(Color.DarkGreen, nonce);
            });
        }

        private void OnCommandCreateTx(string[] args)
        {
            string name = args[0];
            if (!wallets.TryGetValue(name, out Account sender))
            {
                BConsole.WriteLine("account name (", name, ") not found");
                return;
            }

            if (sender.Locked && !sender.Unlock(BConsole.ReadPassword("password: ")))
            {
                BConsole.WriteLine("can't unlock account");
                return;
            }

            Task.Run(async () =>
            {
                string to = wallets.TryGetValue(args[1], out var receiver) ? (string)receiver.Address : args[1];
                ulong value = Coin.ToBeryl(decimal.Parse(args[2]));
                ulong gas = args.Length > 3 ? Coin.ToBeryl(decimal.Parse(args[3])) : 0;
                ulong? nonce = args.Length > 4 ? (ulong?)Convert.ToInt64(args[4]) : null;

                var tx = await web4b.CreateTx(sender.Key, to, value, gas, nonce);

                BConsole.WriteLine("Rlp=", Hex.ToString(tx.Rlp));
            });
        }

        private void OnCommandSendTx(string[] args)
        {
            string name = args[0];
            if (!wallets.TryGetValue(name, out Account sender))
            {
                BConsole.WriteLine("account name (", name, ") not found");
                return;
            }

            if (sender.Locked && !sender.Unlock(BConsole.ReadPassword("password: ")))
            {
                BConsole.WriteLine("can't unlock account");
                return;
            }

            Task.Run(async () =>
            {
                string to = wallets.TryGetValue(args[1], out var receiver) ? (string)receiver.Address : args[1];
                ulong value = Coin.ToBeryl(decimal.Parse(args[2]));
                ulong gas = args.Length > 3 ? Coin.ToBeryl(decimal.Parse(args[3])) : 0;
                ulong? nonce = args.Length > 4 ? (ulong?)Convert.ToInt64(args[4]) : null;

                var txid = await web4b.SendTransactionAsync(sender.Key, to, value, gas, nonce);

                BConsole.WriteLine("txid=", txid);
            });
        }

        private void OnCommandSendTxAndWaitReceipt(string[] args)
        {
            string name = args[0];
            if (!wallets.TryGetValue(name, out Account sender))
            {
                BConsole.WriteLine("account name (", name, ") not found");
                return;
            }

            if (sender.Locked && !sender.Unlock(BConsole.ReadPassword("password: ")))
            {
                BConsole.WriteLine("can't unlock account");
                return;
            }

            Task.Run(async () =>
            {
                string to = wallets.TryGetValue(args[1], out var receiver) ? (string)receiver.Address : args[1];
                ulong value = Coin.ToBeryl(decimal.Parse(args[2]));
                ulong gas = args.Length > 3 ? Coin.ToBeryl(decimal.Parse(args[3])) : 0;
                ulong? nonce = args.Length > 4 ? (ulong?)Convert.ToInt64(args[4]) : null;

                var tx = await web4b.SendTransactionAndWaitReceiptAsync(sender.Key, to, value, gas, nonce);
                BConsole.WriteLine("tx=", tx);
            });
        }

        private void OnCommandGetTxHistory(string[] args)
        {
            Task.Run(async () =>
            {
                string address = wallets.TryGetValue(args[0], out var account) ? (string)account.Address : args[0];
                bool tx = args.Length > 1 ? Convert.ToBoolean(args[1]) : false;

                if (string.IsNullOrEmpty(address))
                {
                    BConsole.WriteLine("address not found");
                    return;
                }

                var txs = await web4b.GetTransactionsByAddressAsync(address, tx);
                BConsole.WriteLine("history=", txs.ToString());
            });
        }

    }
}
