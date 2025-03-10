using System;
using System.IO;
using System.Net.Sockets;

public class LoginHandler
{
    private NetworkStream inStream;
    private NetworkStream outStream;
    private Stream InStream;
    private Stream OutStream;

    public LoginHandler(NetworkStream inStream, NetworkStream outStream, Stream inStreamBuffer, Stream outStreamBuffer)
    {
        this.inStream = inStream;
        this.outStream = outStream;
        this.InStream = inStreamBuffer;
        this.OutStream = outStreamBuffer;
    }

    public bool HandleLogin(out string? MyName, out Cryption? InStreamDecryption, out Cryption? OutStreamDecryption)
    {
        MyName = null;
        InStreamDecryption = null;
        OutStreamDecryption = null;

        long serverSessionKey = 0, clientSessionKey = 0;
        // randomize server part of the session key
        serverSessionKey = ((long)(new Random().NextDouble() * 99999999D) << 32) + (long)(new Random().NextDouble() * 99999999D);

        try
        {
            FillInStream(2);
            if (InStream.ReadUnsignedByte() != 14)
            {
                Console.WriteLine("Expected login Id 14 from client.");
                return false;
            }
            // this is part of the username. Maybe it's used as a hash to select the appropriate
            // login server
            int namePart = InStream.ReadUnsignedByte();
            for (int i = 0; i < 8; i++) outStream.WriteByte(0); // is being ignored by the client

            // login response - 0 means exchange session key to establish encryption
            // Note that we could use 2 right away to skip the cryption part, but I think this
            // won't work in one case when the cryptor class is not set and will throw a NullPointerException
            outStream.WriteByte(0);

            // send the server part of the session Id used (client+server part together are used as cryption key)
            OutStream.WriteQWord(serverSessionKey);
            FlushOutStream();
            FillInStream(2);
            int loginType = InStream.ReadUnsignedByte(); // this is either 16 (new login) or 18 (reconnect after lost connection)
            if (loginType != 16 && loginType != 18)
            {
                Console.WriteLine("Unexpected login type " + loginType);
                return false;
            }
            int loginPacketSize = InStream.ReadUnsignedByte();
            int loginEncryptPacketSize = loginPacketSize - (36 + 1 + 1 + 2); // the size of the RSA encrypted part (containing password)
            //Console.WriteLine("LoginPacket size: " + loginPacketSize + ", RSA packet size: " + loginEncryptPacketSize);
            if (loginEncryptPacketSize <= 0)
            {
                Console.WriteLine("Zero RSA packet size!");
                return false;
            }
            FillInStream(loginPacketSize);
            if (InStream.ReadUnsignedByte() != 255 || InStream.ReadUnsignedWord() != 317)
            {
                Console.WriteLine("Wrong login packet magic ID (expected 255, 317)");
                return false;
            }
            int lowMemoryVersion = InStream.ReadUnsignedByte();
           //Console.WriteLine("Client type: " + ((lowMemoryVersion == 1) ? "low" : "high") + " memory version");
            for (int i = 0; i < 9; i++)
            {
               // Console.WriteLine("dataFileVersion[" + i + "]: 0x" + InStream.ReadDWord().ToString("X"));
                InStream.ReadDWord().ToString("X");
            }

            loginEncryptPacketSize--; // don't count length byte
            int tmp = InStream.ReadUnsignedByte();
            if (loginEncryptPacketSize != tmp)
            {
                Console.WriteLine("Encrypted packet data length (" + loginEncryptPacketSize + ") different from length byte thereof (" + tmp + ")");
                return false;
            }
            tmp = InStream.ReadUnsignedByte();
            if (tmp != 10)
            {
                Console.WriteLine("Encrypted packet Id was " + tmp + " but expected 10");
                return false;
            }
            clientSessionKey = InStream.ReadQWord();
            serverSessionKey = InStream.ReadQWord();
            //Console.WriteLine("UserId: " + InStream.ReadDWord());
            MyName = InStream.ReadString();
            string password = InStream.ReadString();
            //Console.WriteLine("Indent: " + MyName + ":" + password);

            int[] sessionKey = new int[4];
            sessionKey[0] = (int)(clientSessionKey >> 32);
            sessionKey[1] = (int)clientSessionKey;
            sessionKey[2] = (int)(serverSessionKey >> 32);
            sessionKey[3] = (int)serverSessionKey;

            for (int i = 0; i < 4; i++)
                //Console.WriteLine("inStreamSessionKey[" + i + "]: 0x" + sessionKey[i].ToString("X"));

            InStreamDecryption = new Cryption(sessionKey);
            for (int i = 0; i < 4; i++) sessionKey[i] += 50;

            for (int i = 0; i < 4; i++)
                //Console.WriteLine("outStreamSessionKey[" + i + "]: 0x" + sessionKey[i].ToString("X"));

            OutStreamDecryption = new Cryption(sessionKey);
            OutStream.PacketEncryption = OutStreamDecryption;

            outStream.WriteByte(2); // login response (1: wait 2seconds, 2=login successful, 4=ban :-)
            outStream.WriteByte(2); // mod level: 0=normal player, 1=player mod, 2=real mod
            outStream.WriteByte(0); // no log
        }
        catch (Exception ex)
        {
            Console.WriteLine("Server: Exception!");
            Console.WriteLine(ex);
            return false;
        }

        return true;
    }

    private void FlushOutStream()
    {
        try
        {
            if (outStream != null && OutStream != null)
            {
                outStream.Write(OutStream.Buffer, 0, OutStream.CurrentOffset);
                OutStream.CurrentOffset = 0; // reset
            }
            else
            {
                Console.WriteLine("Error: Output stream is null.");
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine("SocketException: Unable to write data to the transport connection.");
            Console.WriteLine($"Error Code: {ex.SocketErrorCode}");
            Console.WriteLine(ex);
        }
        catch (IOException ex)
        {
            Console.WriteLine("IOException: Unable to write data to the transport connection.");
            Console.WriteLine(ex);
        }
        catch (ObjectDisposedException ex)
        {
            Console.WriteLine("Error: The stream has been disposed.");
            Console.WriteLine(ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unexpected error occurred while writing to the stream.");
            Console.WriteLine(ex);
        }
    }

    private void FillInStream(int forceRead)
    {
        InStream.CurrentOffset = 0;
        inStream.Read(InStream.Buffer, 0, forceRead);
    }
}
