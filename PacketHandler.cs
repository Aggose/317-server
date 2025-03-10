using System;
using System.IO;
using System.Net.Sockets;

public class PacketHandler
{
    private NetworkStream inStream;
    private Stream InStream;
    private Cryption InStreamDecryption;
    private int PacketSize = 0, PacketType = 0;
    private int TimeOutCounter = 0;
    public bool ClientConnected = false;

    public PacketHandler(NetworkStream inStream, Stream inStreamBuffer, Cryption inStreamDecryption)
    {
        this.inStream = inStream;
        this.InStream = inStreamBuffer;
        this.InStreamDecryption = inStreamDecryption;
    }

    public bool InterpreteIncomingPackets()
    {
        try
        {
            int avail = inStream.DataAvailable ? 1 : 0;
            if (avail == 0) return false;

            if (PacketType == -1)
            {
                PacketType = inStream.ReadByte() & 0xff;
                if (InStreamDecryption != null)
                    PacketType = (PacketType - InStreamDecryption.GetNextKey()) & 0xff;
                avail--;
                PacketSize = avail;
            }

            FillInStream(PacketSize);
            TimeOutCounter = 0;

            PacketType = -1;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Server: Exception!");
            Console.WriteLine(ex);
            ClientConnected = false;
        }
        return true;
    }

    private void FillInStream(int forceRead)
    {
        InStream.CurrentOffset = 0;
        inStream.Read(InStream.Buffer, 0, forceRead);
    }
}
