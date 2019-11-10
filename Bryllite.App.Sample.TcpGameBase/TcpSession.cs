using Bryllite.Extensions;
using Bryllite.Utils.NabiLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bryllite.App.Sample.TcpGameBase
{
    public class TcpSession
    {
        public static readonly int HEADER_LENGTH = sizeof(int);
        public static readonly int BUFFER_LENGTH = 8192;

        // session id string
        public readonly string ID;

        // socket
        private readonly Socket socket;

        // connected?
        public bool Connected => socket.Connected;

        // read buffer
        private readonly byte[] buffer;

        // received data q
        private List<byte> received = new List<byte>();

        // header : packet size
        private byte[] header = new byte[HEADER_LENGTH];

        // event handler
        public Action<TcpSession, int> OnClosed;
        public Action<TcpSession, byte[]> OnReceived;
        public Action<TcpSession, byte[]> OnWritten;

        // remote address
        public string Remote { get; private set; }

        public TcpSession(Socket socket)
        {
            this.socket = socket;

            // read buffer
            buffer = new byte[BUFFER_LENGTH];

            // session id
            ID = Convert.ToBase64String(SecureRandom.GetBytes(32));

            // remote address
            IPEndPoint ep = (IPEndPoint)socket.RemoteEndPoint;
            Remote = $"{ep.Address.ToString()}:{ep.Port}";
        }

        public void Dispose()
        {
            // close socket
            if (Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        public void Start()
        {
            // start to read
            ReadHeader();
        }

        public void Stop(int reason)
        {
            if (Connected)
            {
                // invoke closed event
                OnClosed?.Invoke(this, reason);

                Dispose();
            }
        }

        private void OnError(string error)
        {
            Log.Warning("OnError(): Remote=", Color.DarkGreen, Remote, ", error=", error);
            Stop(-1);
        }

        private bool IsSocketError(SocketError socketErrorCode)
        {
            switch (socketErrorCode)
            {
                case SocketError.Success:
                case SocketError.IOPending:
                case SocketError.WouldBlock:
                    return false;

                default: break;
            }

            return true;
        }


        private void ReadHeader()
        {
            if (!Connected) return;

            try
            {
                socket.BeginReceive(header, 0, HEADER_LENGTH, SocketFlags.None, OnHandleReadHeader, header);
            }
            catch (SocketException e)
            {
                if (IsSocketError(e.SocketErrorCode))
                    OnError(e.Message);
            }
            catch (Exception e)
            {
                OnError(e.Message);
            }
        }

        private void OnHandleReadHeader(IAsyncResult ar)
        {
            if (!Connected) return;

            int readBytes = 0;
            try
            {
                readBytes = socket.EndReceive(ar);
                if (readBytes <= 0)
                {
                    Stop(0);
                    return;
                }

                // 항상 헤더 크기만큼 읽는다.
                // 헤더 사이즈 보다 작은 크기가 읽어지는 경우가 있나?
                Guard.Assert(readBytes == HEADER_LENGTH);

                // 메세지 크기
                int length = BitConverter.ToInt32(header, 0);
                Guard.Assert(length > 0 && length < buffer.Length);

                // 바디 읽기
                ReadBody();
            }
            catch (SocketException e)
            {
                if (IsSocketError(e.SocketErrorCode))
                {
                    if (readBytes <= 0)
                    {
                        Stop(0);
                        return;
                    }

                    OnError(e.Message);
                }
            }
            catch (Exception e)
            {
                OnError(e.Message);
            }
        }

        private void ReadBody()
        {
            if (!Connected) return;

            int estimated = BitConverter.ToInt32(header, 0);
            int size = Math.Min(estimated - received.Count, buffer.Length);

            try
            {
                socket.BeginReceive(buffer, 0, size, SocketFlags.None, OnHandleReadBody, this);
            }
            catch (SocketException e)
            {
                if (IsSocketError(e.SocketErrorCode))
                    OnError(e.Message);
            }
            catch (Exception e)
            {
                OnError(e.Message);
            }
        }

        private void OnHandleReadBody(IAsyncResult ar)
        {
            if (!Connected) return;

            int readBytes = 0;
            try
            {
                readBytes = socket.EndReceive(ar);
                if (readBytes <= 0)
                {
                    Stop(0);
                    return;
                }

                // 읽기 버퍼에 기록
                received.AddRange(buffer.Take(readBytes).ToArray());

                // 만약 다 읽었으면
                if (received.Count == BitConverter.ToInt32(header, 0))
                {
                    // 수신 콜백
                    OnReceived?.Invoke(this, received.ToArray());

                    // 수신 데이터 삭제
                    received.Clear();

                    // 헤더 읽기
                    ReadHeader();
                }
                else
                {
                    // 아니면 나머지 데이터 더 읽기
                    ReadBody();
                }
            }
            catch (SocketException e)
            {
                if (IsSocketError(e.SocketErrorCode))
                {
                    if (readBytes <= 0)
                    {
                        Stop(0);
                        return;
                    }

                    OnError(e.Message);
                }
            }
            catch (Exception e)
            {
                OnError(e.Message);
            }

        }

        public int Write(byte[] data)
        {
            if (!Connected || data.IsNullOrEmpty())
                return 0;

            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(data.Length));
            bytes.AddRange(data);
            byte[] bytesToSend = bytes.ToArray();

            try
            {
                socket.BeginSend(bytesToSend, 0, bytesToSend.Length, SocketFlags.None, OnHandleWrite, bytesToSend);
            }
            catch (SocketException e)
            {
                if (IsSocketError(e.SocketErrorCode))
                {
                    OnError(e.Message);
                    return -1;
                }
            }
            catch (Exception e)
            {
                OnError(e.Message);
                return -1;
            }

            return bytesToSend.Length;
        }

        private void OnHandleWrite(IAsyncResult ar)
        {
            if (!Connected) return;

            try
            {
                // writed bytes
                int writeBytes = socket.EndSend(ar);

                // bytes to write ( include header )
                byte[] bytesToSend = (byte[])ar.AsyncState;

                // is it possible if writing is not completed?
                Guard.Assert(bytesToSend.Length == writeBytes);

                // write completed callback
                OnWritten?.Invoke(this, bytesToSend.Skip(HEADER_LENGTH).ToArray());
            }
            catch (SocketException e)
            {
                if (IsSocketError(e.SocketErrorCode))
                    OnError(e.Message);
            }
            catch (Exception e)
            {
                OnError(e.Message);
            }
        }

    }
}
