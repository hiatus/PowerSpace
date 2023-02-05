using System;
using System.ComponentModel.Design;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PowerSpace
{
    internal class Connection
    {
        internal const int MaxReceiveSize = 1024;

        private Socket _Socket = null;
        private readonly IPEndPoint _IPEndPoint = null;
        private readonly RC4Context _Crypter = null;

        internal Connection(IPAddress ip, int port, byte[] rc4Key = null)
        {
            _IPEndPoint = new IPEndPoint(ip, port);
            _Socket = new Socket(_IPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            if (rc4Key != null)
                _Crypter = new RC4Context(rc4Key);

            _Socket.Connect(_IPEndPoint);
        }

        ~Connection()
        {
            Close();
        }

        internal string ReceiveString()
        {
            int len;
            byte[] data;
            byte[] buffer = new byte[MaxReceiveSize];

            len = _Socket.Receive(buffer);
            data = new byte[len];

            Array.Copy(buffer, data, data.Length);
            _Crypter?.Decrypt(data);

            return Encoding.Default.GetString(data);
        }

        internal void SendString(string s)
        {
            int sent;
            byte[] data, aux;

            data = Encoding.Default.GetBytes(s);
            _Crypter?.Encrypt(data);

            sent = _Socket.Send(data);

            while (sent < data.Length)
            {
                aux = new byte[sent];
                Array.Copy(data, aux, aux.Length);
                _Crypter?.Encrypt(aux);

                sent += _Socket.Send(aux);
            }
        }

        internal void Close()
        {
            if (_Socket != null)
            {
                _Socket.Close();
                _Socket = null;
            }
        }
    }
}
