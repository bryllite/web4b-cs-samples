using Bryllite.Database.TrieDB;
using Bryllite.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bryllite.App.Sample.TcpGameServer
{
    public class UserDB : FileDB
    {
        public IEnumerable<User> Users => SelectAll();

        public UserDB(string path) : base(path)
        {
        }

        private static byte[] ToKey(string uid)
        {
            return Encoding.UTF8.GetBytes(uid);
        }

        public bool Contains(string uid)
        {
            return !Get(ToKey(uid)).IsNullOrEmpty();
        }

        public User Select(string uid)
        {
            return User.TryParse(Get(ToKey(uid)), out var user) ? user : null;
        }

        public User Select(string uid, string passhash)
        {
            var user = Select(uid);
            return user?.PassHash == passhash ? user : null;
        }

        public IEnumerable<User> SelectAll()
        {
            List<User> users = new List<User>();
            foreach (var entry in this)
                users.Add(User.Parse(entry.Value));
            return users;
        }

        public bool Insert(User user)
        {
            if (Contains(user.Id)) return false;

            return Put(ToKey(user.Id), user.Rlp);
        }

        public bool Update(string uid, string passhash)
        {
            var user = Select(uid);
            if (null == user)
                return false;

            user.PassHash = passhash;
            return Put(ToKey(user.Id), user.Rlp);
        }


        public bool Delete(string uid)
        {
            return Del(ToKey(uid));
        }

    }
}
