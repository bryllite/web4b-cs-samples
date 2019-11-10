using Bryllite.App.Sample.TcpGameBase;
using Bryllite.Utils.NabiLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bryllite.App.Sample.TcpGameServer
{
    public class TcpServer
    {
        // event handler
        public Action<TcpSession> OnNewConnection;
        public Action<TcpSession, int> OnConnectionLost;
        public Action<TcpSession, byte[]> OnReceiveMessage;
        public Action<TcpSession, byte[]> OnWriteCompleted;

        // listener
        private Socket listener;

        // sessions
        private Dictionary<string, TcpSession> sessions = new Dictionary<string, TcpSession>();

        // is server running?
        public bool Running { get; private set; }

        public TcpServer()
        {
        }

        // all connections
        public IEnumerable<TcpSession> Connections
        {
            get
            {
                lock (sessions)
                    return sessions.Values.ToArray();
            }
        }


        public void Start(int port)
        {
            if (Running) throw new Exception("already running");

            try
            {
                // listener
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, true);

                // bind socket
                listener.Bind(new IPEndPoint(IPAddress.Any, port));

                // listen
                listener.Listen(256);

                // start accept async
                BeginAccept();

                Running = true;
            }
            catch (Exception e)
            {
                Log.Warning("exception! e=", Color.DarkGray, e.Message);
            }
        }

        public void Stop()
        {
            if (!Running) return;
            Running = false;

            // close accept socket
            if (listener.IsBound)
                listener.Close();

            // close all sessions
            lock (sessions)
            {
                foreach (var s in sessions.Values)
                    s.Dispose();

                sessions.Clear();
            }
        }

        private void BeginAccept()
        {
            try
            {
                listener.BeginAccept(OnHandleAccept, null);
            }
            catch (Exception e)
            {
                Log.Warning("exception! e=", e.Message);
            }
        }

        private void OnHandleAccept(IAsyncResult ar)
        {
            try
            {
                // accepted socket
                Socket socket = listener.EndAccept(ar);
                if (ReferenceEquals(socket, null))
                    return;

                // process accepted socket
                OnAcceptSocket(socket);

                // begin accept again
                BeginAccept();
            }
            catch (ObjectDisposedException odex)
            {
                if (Running)
                    Log.Debug("ObjectDisposedException! odex=", odex.ToString());
            }
            catch (SocketException sex)
            {
                Log.Warning("SocketException! sex=", sex.ToString());
            }
            catch (Exception ex)
            {
                Log.Warning("exception! ex=", ex.ToString());
            }
        }

        private void OnAcceptSocket(Socket socket)
        {
            // new session
            TcpSession session = new TcpSession(socket)
            {
                OnClosed = OnClosed,
                OnReceived = OnReceived,
                OnWritten = OnWritten,
            };

            lock (sessions)
                sessions[session.ID] = session;

            // new connection callback
            OnNewConnection?.Invoke(session);

            // start session
            session.Start();
        }


        private void OnClosed(TcpSession session, int reason)
        {
            try
            {
                OnConnectionLost?.Invoke(session, reason);
            }
            catch (Exception ex)
            {
                Log.Warning("exception! ex=", ex);
            }
            finally
            {
                lock (sessions)
                    sessions.Remove(session.ID);
            }
        }

        private void OnReceived(TcpSession session, byte[] message)
        {
            try
            {
                OnReceiveMessage?.Invoke(session, message);
            }
            catch (Exception ex)
            {
                Log.Warning("exception! ex=", ex);
            }
        }

        private void OnWritten(TcpSession session, byte[] message)
        {
            try
            {
                OnWriteCompleted?.Invoke(session, message);
            }
            catch (Exception ex)
            {
                Log.Warning("exception! ex=", ex);
            }
        }
    }
}
