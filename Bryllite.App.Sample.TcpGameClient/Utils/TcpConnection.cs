using Bryllite.App.Sample.TcpGameBase;
using Bryllite.Utils.NabiLog;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Bryllite.App.Sample.TcpGameClient
{
    public class TcpConnection
    {
        // tcp connection
        private TcpClient connection;

        // tcp session for this connection
        private TcpSession session;

        // event handler
        public Action<TcpSession, bool> OnConnected;
        public Action<TcpSession, int> OnDisconnected;
        public Action<TcpSession, byte[]> OnMessage;
        public Action<TcpSession, byte[]> OnWritten;

        // is connected?
        public bool Connected => connection?.Connected ?? false;

        public TcpConnection()
        {
        }

        public void Dispose()
        {
            session?.Dispose();
            connection?.Dispose();
        }

        public void Start(string remote)
        {
            Start(new Uri(remote));
        }

        public void Start(Uri remote)
        {
            connection = new TcpClient();
            connection.BeginConnect(remote.Host, remote.Port, OnHandleConnect, null);
        }

        public void Stop(int reason = 0)
        {
            if (session != null && session.Connected)
                session.Stop(reason);

            if (Connected)
                connection.Close();
        }

        public int Send(byte[] message)
        {
            return session?.Write(message) ?? -1;
        }

        private void OnHandleConnect(IAsyncResult ar)
        {
            try
            {
                connection.EndConnect(ar);

                // connected callback
                OnConnected?.Invoke(this, Connected);
                if (Connected)
                {
                    // session
                    session = new TcpSession(connection.Client)
                    {
                        OnClosed = OnSessionClosed,
                        OnReceived = OnSessionReceived,
                        OnWritten = OnSessionWritten
                    };

                    // start
                    session.Start();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("exception! ex=", ex);
            }
        }

        private void OnSessionClosed(TcpSession session, int reason)
        {
            // close callback
            OnDisconnected?.Invoke(this, reason);
        }

        private void OnSessionReceived(TcpSession session, byte[] message)
        {
            // message callback
            OnMessage?.Invoke(this, message);
        }

        private void OnSessionWritten(TcpSession session, byte[] message)
        {
            OnWritten?.Invoke(this, message);
        }

        public static implicit operator TcpSession(TcpConnection connection)
        {
            return connection?.session;
        }

    }
}
