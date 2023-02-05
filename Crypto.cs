using System;
using System.Security.Cryptography;
using System.Text;

namespace PowerSpace
{
    internal class RC4Context
    {
        private static int _SkipSize = 4096;

        internal uint Pos1;
        internal uint Pos2;
        internal byte[] State = null;

        internal RC4Context(byte[] key)
        {
            byte aux;

            Pos1 = 0;
            Pos2 = 0;
            State = new byte[256];

            for (int i = 0; i < 256; ++i)
                State[i] = (byte)i;

            for (int i = 0, j = 0; i < 256; ++i)
            {
                j = (j + i + key[i % key.Length]) % 256;

                aux = State[i];

                State[i] = State[j];
                State[j] = aux;
            }

            Skip(_SkipSize);
        }

        internal void Skip(int len)
        {
            byte aux;

            for (int i = 0; i < len; ++i)
            {
                Pos1 = (Pos1 + 1) % 256;
                Pos2 = (Pos2 + State[Pos1]) % 256;

                aux = State[Pos1];

                State[Pos1] = State[Pos2];
                State[Pos2] = aux;
            }
        }
    
        internal void Encrypt(byte[] data)
        {
            byte aux;

            for (int i = 0; i < data.Length; ++i)
            {
                Pos1 = (Pos1 + 1) % 256;
                Pos2 = (Pos2 + State[Pos1]) % 256;

                aux = State[Pos1];

                State[Pos1] = State[Pos2];
                State[Pos2] = aux;

                data[i] ^= State[(State[Pos1] + State[Pos2]) % 256];
            }
        }

        internal void Decrypt(byte[] data)
        {
            Encrypt(data);
        }
    }

    internal class Secret
    {
        private byte[] _Key = {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        internal byte[] GetBytes(string base64)
        {
            byte[] data = Convert.FromBase64String(base64);

            for (int i = 0; i < base64.Length; ++i)
                data[i] ^= _Key[i % _Key.Length];

            return data;
        }

        internal string GetString(string base64)
        {
            return Encoding.Default.GetString(GetBytes(base64));
        }
    }
}
