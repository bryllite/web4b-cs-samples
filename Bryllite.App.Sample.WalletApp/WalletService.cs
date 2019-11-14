using Bryllite.Cryptography.Signers;
using Bryllite.Utils.NabiLog;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bryllite.App.Sample.WalletApp
{
    public class WalletService : Dictionary<string, Account>, IDisposable
    {
        public static readonly string WALLETS = "wallets.json";

        public WalletService()
        {
        }

        public void Dispose()
        {
            lock (this)
                Clear();
        }

        public bool Start()
        {
            return Load(WALLETS);
        }

        public void Stop()
        {
            Dispose();
        }

        private bool Load(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    var o = JObject.Parse(File.ReadAllText(file));
                    foreach (var entry in o)
                    {
                        string name = entry.Key;
                        string keystore = entry.Value.ToString();

                        lock (this)
                            this[name] = new Account(keystore);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("Exception! ex=", ex);
                return false;
            }
        }


        private bool Save(string file)
        {
            try
            {
                var all = new JObject();
                lock (this)
                {
                    foreach (var entry in this)
                    {
                        string name = entry.Key;
                        Account account = entry.Value;

                        all[name] = account.KeyStore;
                    }
                }

                File.WriteAllText(file, all.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("Exception! ex=", ex);
                return false;
            }
        }

        private bool Save()
        {
            return Save(WALLETS);
        }
    

        public Account GetAccount(string name)
        {
            lock(this)
                return TryGetValue(name, out var account) ? account : null;
        }

        public Account GetAccount(Address address)
        {
            lock (this)
            {
                foreach (var account in Values)
                    if (account.Address == address) return account;

                return null;
            }
        }

        // account exists?
        public bool Contains(string name)
        {
            return ContainsKey(name);
        }

        // account exists?
        public bool Contains(Address address)
        {
            return !ReferenceEquals(GetAccount(address), null);
        }

        // add account
        public Account Add(string name, string keystore)
        {
            bool flush = true;
            try
            {
                lock (this)
                    return this[name] = new Account(keystore);
            }
            catch (Exception ex)
            {
                flush = false;
                Log.Warning("Exception! ex=", ex);
                return null;
            }
            finally
            {
                if (flush)
                    Save();
            }
        }

        // delete account
        public bool Delete(string name)
        {
            bool flush = true;
            try
            {
                lock (this)
                    return Remove(name);
            }
            catch (Exception ex)
            {
                flush = false;
                Log.Warning("Exception! ex=", ex);
                return false;
            }
            finally
            {
                if (flush)
                    Save();
            }
        }

    }
}
