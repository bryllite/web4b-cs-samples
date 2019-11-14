using Bryllite.Cryptography.Signers;
using Bryllite.Utils.NabiLog;
using Bryllite.Utils.Pbkdf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bryllite.App.Sample.WalletApp
{

    public class Account
    {
        // private key
        public PrivateKey Key;

        // is locked?
        public bool Locked => ReferenceEquals(Key, null);

        // address
        public Address Address
        {
            get
            {
                return KeyStore.GetValue("address", StringComparison.OrdinalIgnoreCase).ToString();
            }
        }

        // keystore
        public JObject KeyStore;

        public Account(JObject keystore)
        {
            KeyStore = keystore;
        }

        public Account(string json) : this(JObject.Parse(json))
        {
        }

        public void Lock()
        {
            Key = null;
        }

        public bool Unlock(string passphrase)
        {
            if (Locked)
            {
                try
                {
                    byte[] key = KeyStoreService.DecryptKeyStoreV3(KeyStore.ToString(), passphrase);
                    if (!ReferenceEquals(key, null))
                    {
                        Key = new PrivateKey(key);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Exception! ex=", ex);
                }
            }

            return false;
        }
    }
}
