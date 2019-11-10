using Bryllite.Cryptography.Hash;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bryllite.App.Sample.TcpGameBase
{
    // encrypt password string
    public static class PasswordEncoder
    {
        public static string Encode(string password)
        {
            return Hex.ToString(KeccakProvider.Hash256(Encoding.UTF8.GetBytes(password)));
        }
    }
}
