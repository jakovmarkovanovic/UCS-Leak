﻿using System.Net.Sockets;
using UCS.Logic;
using UCS.Helpers;
using System;
using System.Diagnostics;
using UCS.Core;
using UCS.Core.Crypto;
using UCS.Core.Network;
using UCS.Helpers.Binary;
using UCS.Logic.Enums;

namespace UCS.Packets
{
    internal class Device
    {
        internal Socket Socket;
        internal Level Player;
        internal Token Token;
        internal Crypto Keys;
        public Device(Socket so)
        {
            this.Socket = so;
            this.Keys = new Crypto();
            this.SocketHandle = so.Handle;
            this.IncomingPacketsKey = new byte[Key._RC4_EndecryptKey.Length];
            this.OutgoingPacketsKey = new byte[Key._RC4_EndecryptKey.Length];

            Array.Copy(Key._RC4_EndecryptKey, this.IncomingPacketsKey, Key._RC4_EndecryptKey.Length);
            Array.Copy(Key._RC4_EndecryptKey, this.OutgoingPacketsKey, Key._RC4_EndecryptKey.Length);
        }
        public Device(Socket so, Token token)
        {
            this.Socket = so;
            this.Keys = new Crypto();
            this.Token = token;
            this.SocketHandle = so.Handle;
            this.IncomingPacketsKey = new byte[Key._RC4_EndecryptKey.Length];
            this.OutgoingPacketsKey = new byte[Key._RC4_EndecryptKey.Length];

            Array.Copy(Key._RC4_EndecryptKey, this.IncomingPacketsKey, Key._RC4_EndecryptKey.Length);
            Array.Copy(Key._RC4_EndecryptKey, this.OutgoingPacketsKey, Key._RC4_EndecryptKey.Length);
        }


        internal State PlayerState = Logic.Enums.State.DISCONNECTED;

        internal IntPtr SocketHandle;

        internal byte[] IncomingPacketsKey;
        internal byte[] OutgoingPacketsKey;

        internal string Interface;
        internal string AndroidID;
        internal string OpenUDID;
        internal string Model;
        internal string OSVersion;
        internal string MACAddress;
        internal string AdvertiseID;
        internal string VendorID;
        internal string IPAddress;

        internal uint ClientSeed;

        internal bool Connected => this.Socket.Connected && (!this.Socket.Poll(1000, SelectMode.SelectRead) || this.Socket.Available != 0);

        public static void TransformSessionKey(int clientSeed, byte[] sessionKey)
        {
            int[] buffer = new int[624];
            initialize_generator(clientSeed, buffer);
            int byte100 = 0;
            for (int i = 0; i < 100; i++)
            {
                byte100 = extract_number(buffer, i);
            }

            for (int i = 0; i < sessionKey.Length; i++)
            {
                sessionKey[i] ^= (byte)(extract_number(buffer, i + 100) & byte100);
            }
        }

        // Initialize the generator from a seed
        public static void initialize_generator(int seed, int[] buffer)
        {
            buffer[0] = seed;
            for (int i = 1; i < 624; ++i)
            {
                buffer[i] = (int)(1812433253 * ((buffer[i - 1] ^ (buffer[i - 1] >> 30)) + 1));
            }
        }
        // Extract a tempered pseudorandom number based on the index-th value,
        // calling generate_numbers() every 624 numbers

        public static int extract_number(int[] buffer, int ix)
        {
            if (ix == 0)
            {
                generate_numbers(buffer);
            }

            int y = buffer[ix];
            y ^= (y >> 11);
            y ^= (int)(y << 7 & (2636928640)); // 0x9d2c5680
            y ^= (int)(y << 15 & (4022730752)); // 0xefc60000
            y ^= (y >> 18);

            if ((y & (1 << 31)) != 0)
            {
                y = ~y + 1;
            }

            ix = (ix + 1) % 624;
            return y % 256;
        }

        public static void generate_numbers(int[] buffer)
        {
            for (int i = 0; i < 624; i++)
            {
                int y = (int)((buffer[i] & 0x80000000) + (buffer[(i + 1) % 624] & 0x7fffffff));
                buffer[i] = (int)(buffer[(i + 397) % 624] ^ (y >> 1));
                if ((y % 2) != 0)
                {
                    buffer[i] = (int)(buffer[i] ^ (2567483615));
                }
            }
        }

        public unsafe void UpdateKey(byte[] sessionKey)
        {
            TransformSessionKey((int)ClientSeed, sessionKey);

            byte[] newKey = new byte[264];
            byte[] clientKey = sessionKey;
            int v7 = Key._RC4_PrivateKey.Length;
            int v9 = Key._RC4_PrivateKey.Length + sessionKey.Length;
            byte[] completeSessionKey = new byte[Key._RC4_PrivateKey.Length + sessionKey.Length];
            Array.Copy(Key._RC4_PrivateKey, 0, completeSessionKey, 0, v7); //memcpy(v10, v8, v7);
            Array.Copy(clientKey, 0, completeSessionKey, v7, sessionKey.Length); //memcpy(v10 + v7, clientKey, sessionKeySize);
            uint v11 = 0;
            uint v16;
            uint v12;//attention type
            byte v13;//attention type
            uint v14;
            byte* v15;
            uint v17;
            uint v18;
            byte v19;
            byte* v20;
            byte v21;
            uint v22;
            byte* v23;

            fixed (byte* v5 = newKey, v8 = Key._RC4_PrivateKey, v10 = completeSessionKey)
            {
                do
                {
                    *(byte*)(v5 + v11 + 8) = (byte)v11;
                    ++v11;
                }
                while (v11 != 256);
                *v5 = 0;
                *(v5 + 4) = 0;
                while (true)
                {
                    v16 = *v5;

                    //if (v16 == 255)//if ( *v5 > 255 )
                    //    break;
                    v12 = *((byte*)v10 + v16 % v9) + *(uint*)(v5 + 4);
                    *(uint*)v5 = v16 + 1;
                    v13 = *(byte*)(v5 + v16 + 8);
                    v14 = (byte)(v12 + *(byte*)(v5 + v16 + 8));
                    *(uint*)(v5 + 4) = v14;
                    v15 = v5 + v14;
                    *(byte*)(v5 + v16 + 8) = *(byte*)(v15 + 8);
                    *(byte*)(v15 + 8) = v13;
                    if (v16 == 255)//if ( *v5 > 255 )
                        break;
                }
                v17 = 0;
                *v5 = 0;
                *(v5 + 4) = 0;
                while (v17 < v9)
                {
                    ++v17;
                    v18 = *(uint*)(v5 + 4);
                    v19 = (byte)(*(uint*)v5 + 1);
                    *(uint*)v5 = v19;
                    v20 = v5 + v19;
                    v21 = *(byte*)(v20 + 8);
                    v22 = (byte)(v18 + v21);
                    *(uint*)(v5 + 4) = v22;
                    v23 = v5 + v22;
                    *(byte*)(v20 + 8) = *(byte*)(v23 + 8);
                    *(byte*)(v23 + 8) = v21;
                }
            }
            Array.Copy(newKey, IncomingPacketsKey, newKey.Length);
            Array.Copy(newKey, OutgoingPacketsKey, newKey.Length);
        }

        public void EnDecrypt(Byte[] key, Byte[] data)
        {
            int dataLen;

            if (data != null)
            {
                dataLen = data.Length;

                if (dataLen >= 1)
                {
                    do
                    {
                        dataLen--;
                        byte index = (byte)(key[0] + 1);
                        key[0] = index;
                        byte num2 = (byte)(key[4] + key[index + 8]);
                        key[4] = num2;
                        byte num3 = key[index + 8];
                        key[index + 8] = key[num2 + 8];
                        key[key[4] + 8] = num3;
                        byte num4 = key[((byte)(key[key[4] + 8] + key[key[0] + 8])) + 8];
                        data[(data.Length - dataLen) - 1] = (byte)(data[(data.Length - dataLen) - 1] ^ num4);
                    }
                    while (dataLen > 0);
                }
            }
        }
        /*
        public void Decrypt(Byte[] data)
        {
            EnDecrypt(this.IncomingPacketsKey, data);
        }

        public void Encrypt(Byte[] data)
        {
            EnDecrypt(this.OutgoingPacketsKey, data);
        }
        */

        public bool IsClientSocketConnected()
        {
            try
            {
                return !((Socket.Poll(1000, SelectMode.SelectRead) && (Socket.Available == 0)) || !Socket.Connected);
            }
            catch
            {
                return false;
            }
        }

        internal void Process(byte[] Buffer)
        {
            if (Buffer.Length >= 7)
            {
                using (Reader Reader = new Reader(Buffer))
                {
                    ushort Identifier = Reader.ReadUInt16();
                    Reader.Seek(1);
                    ushort Length = Reader.ReadUInt16();
                    ushort Version = Reader.ReadUInt16();
                    if (Buffer.Length - 7 >= Length)
                    {
                        if (MessageFactory.Messages.ContainsKey(Identifier))
                        {
                            Message message =  Activator.CreateInstance(MessageFactory.Messages[Identifier], this, Reader) as Message;

                            message.Identifier = Identifier;
                            message.Length = Length;
                            message.Version = Version;

                            message.Reader = Reader;

                            try
                            {
                                Debug.WriteLine(Utils.Padding(message.Device.Socket.RemoteEndPoint.ToString(), 15) + " --> " + message.GetType().Name);
                                Logger.Write("Message " + message.GetType().Name + " is handled");
                                message.Decrypt();
                                message.Decode();
                                message.Process();
                            }
                            catch (Exception Exception)
                            {
#if DEBUG
                                Console.WriteLine(Utils.Padding(Exception.GetType().Name, 15) + " : " + Exception.Message + ". [" + (this.Player != null ? this.Player.Avatar.HighID + ":" + this.Player.Avatar.LowID : "---") + ']' + Environment.NewLine + Exception.StackTrace);
#endif
                            }
                        }
                        else
                        {
                            Logger.Write("Message " + Identifier + " is unhandled");
                            Debug.WriteLine(Utils.Padding(this.GetType().Name, 15) +  " : Aborting, we can't handle the following message : ID " +  Identifier + ", Length " + Length + ", Version " +  Version + ".");

                            this.Keys.SNonce.Increment();
                        }

                        if ((Buffer.Length - 7) - Length >= 7)
                        {
                            this.Process(Reader.ReadBytes((Buffer.Length - 7) - Length));
                        }
                        else
                        {
                            this.Token.Reset();
                        }
                    }
                }
            }
        }
    }
}