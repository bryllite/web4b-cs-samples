using Bryllite.Extensions;
using Newtonsoft.Json.Linq;
using System;

namespace Bryllite.App.Sample.TcpGameBase
{
    public class GameMessage : JObject
    {
        // message id
        public static readonly string MESSAGE_ID = "id";

        // message id getter/setter
        public string MessageId
        {
            get
            {
                return Value<string>(MESSAGE_ID);
            }
            private set
            {
                this[MESSAGE_ID] = value;
            }
        }

        public GameMessage(string id) : base()
        {
            MessageId = id;
        }

        protected GameMessage(JObject obj) : base(obj)
        {
        }

        protected GameMessage(byte[] bytes) : this(BsonConverter.FromByteArray<JObject>(bytes))
        {
        }

        public GameMessage With<T>(string key, T value)
        {
            JsonExtension.Put(this, key, value);
            return this;
        }

        public byte[] ToByteArray()
        {
            return BsonConverter.ToByteArray<JObject>(this);
        }

        public new static GameMessage Parse(string json)
        {
            return new GameMessage(JObject.Parse(json));
        }

        public static GameMessage Parse(byte[] bytes)
        {
            return new GameMessage(bytes);
        }

        public static bool TryParse(string json, out GameMessage message)
        {
            try
            {
                message = Parse(json);
                return true;
            }
            catch
            {
                message = null;
                return false;
            }
        }

        public static bool TryParse(byte[] bytes, out GameMessage message)
        {
            try
            {
                message = Parse(bytes);
                return true;
            }
            catch
            {
                message = null;
                return false;
            }
        }


        public static implicit operator byte[] (GameMessage message)
        {
            return message?.ToByteArray();
        }

        public static implicit operator string(GameMessage message)
        {
            return message?.ToString();
        }

        public static implicit operator GameMessage(byte[] bytes)
        {
            return TryParse(bytes, out GameMessage message) ? message : null;
        }

        public static implicit operator GameMessage(string json)
        {
            return TryParse(json, out GameMessage message) ? message : null;
        }

    }
}
