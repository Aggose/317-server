using System.Net.Sockets;


public class ClientHandler
{
    private Socket mySock;
    private NetworkStream inStream;
    private NetworkStream outStream;
    public string? MyName = null; // name of the connecting client
    public Stream? InStream = null, OutStream = null;
    public Cryption? InStreamDecryption = null, OutStreamDecryption = null;
    public bool ClientConnected = false;
    private PacketHandler? packetHandler;
    private static readonly Random random = new();
    private static int nextPlayerId = 1;
    private ClientUtilities clientUtilities;
    public ClientHandler(Socket s)
    {
        nextPlayerId = nextPlayerId++;
        mySock = s;
        MyName = "";
        if (s != null)
        {
            try
            {
                inStream = new NetworkStream(s);
                outStream = new NetworkStream(s);
                clientUtilities = new ClientUtilities(OutStream); // Initialize clientUtilities here

            }
            catch (IOException ioe)
            {
                Console.WriteLine(ioe);
            }
        }
        else
        {
            inStream = null;
            outStream = null;
        }
    }

    int mapRegionX = 386, mapRegionY = 405;// the region the player is currently in
    int playerPosX = 50, playerPosY = 55; // player position in the region


    public void Run(ClientUtilities clientUtilities)
    {
        if (mySock == null) return; // no socket there - closed

        // we accepted a new connection
        OutStream = new Stream(new byte[100000]);
        OutStream.CurrentOffset = 0;
        InStream = new Stream(new byte[10000]);
        InStream.CurrentOffset = 0;

        clientUtilities = new ClientUtilities(OutStream);

        // Initialize PacketHandler
        packetHandler = new PacketHandler(inStream, InStream, InStreamDecryption);
        // handle the login stuff
        var loginHandler = new LoginHandler(inStream, outStream, InStream, OutStream);
        if (!loginHandler.HandleLogin(out MyName, out InStreamDecryption, out OutStreamDecryption))
        {
            Shutdown();
            return;
        }

        // End of login procedure


        // initiate loading of new map area
        OutStream.CreateFrame(73);
        OutStream.WriteWordA(mapRegionX);
        OutStream.WriteWord(mapRegionY);

        // players initialization
        OutStream.CreateFrame(81);
        OutStream.WriteWord(0); // placeholder for size of this packet.
        int ofs = OutStream.CurrentOffset;
        OutStream.InitBitAccess();

        // update this player
        OutStream.WriteBits(1, 1); // set to true if updating thisPlayer
        OutStream.WriteBits(2, 3); // updateType - 3=jump to pos
        OutStream.WriteBits(2, 0); // height level (0-3)
        OutStream.WriteBits(1, 1); // set to true, if discarding walking queue (after teleport e.g.)
        OutStream.WriteBits(1, 1); // set to true, if this player is not in local list yet???
        OutStream.WriteBits(7, playerPosY);   // y-position
        OutStream.WriteBits(7, playerPosX);   // x-position

        // update other players...?!
        OutStream.WriteBits(8, 0); // number of players to add
        // add new players???
        OutStream.WriteBits(11, 2047); // magic EOF
        OutStream.FinishBitAccess();
        OutStream.WriteByte(0); // ???? needed that to stop client from crashing
        OutStream.WriteFrameSizeWord(OutStream.CurrentOffset - ofs);

        int[] SideBarIds = {
        2423,3917,638,3213,1644,5608,12855,
        -1,5065,5715,2449,904,147,962,
        };

        for (int Bar = 0; Bar <= 13; Bar++)
        {
            SetSidebarInterface(Bar, SideBarIds[Bar]);
        }
        //Console.WriteLine("X: " + mapRegionX + ",Y: " + mapRegionY);

        ClientConnected = true;
        FlushOutStream();
    }

    public void DropItem(int slot)
    {

        /*OutStream.CreateFrame(87);        // drop item
            int droppedItem = OutStream.ReadUnsignedWordA();
            //OutStream.ReadUnsignedByte() + inStream.readUnsignedByte();
            OutStream.ReadUnsignedWordA();
        Console.WriteLine("dropItem: " + droppedItem + " Slot: " + slot);
        return;*/
        //println_debug("dropItem: "+droppedItem+" Slot: "+slot);
        Console.WriteLine("Drop Clicked");
    }
    public void SetSidebarInterface(int menuId, int form)
    {
        OutStream.CreateFrame(71);
        OutStream.WriteWord(form);
        OutStream.WriteByteA(menuId);
    }

    public void CreateGroundItem(int itemID, int itemAmount, int itemX, int itemY)
    {// Phate: Omg fucking sexy! creates item at absolute X and Y
        OutStream.CreateFrame(85);                              // Phate: Spawn ground item
                                                                //OutStream.WriteByteC((itemY - 8 * mapRegionY));
                                                                //OutStream.WriteByteC((itemX - 8 * mapRegionX));
        OutStream.WriteByteC(itemY);
        OutStream.WriteByteC(itemX);
        OutStream.CreateFrame(44);
        OutStream.WriteWordBigEndianA(itemID);
        OutStream.WriteWord(itemAmount);
        OutStream.WriteByte(0);                                 // x(4 MSB) y(LSB) coords
                                                                //System.out.println("CreateGroundItem "+itemID+" "+(itemX - 8 * c.mapRegionX)+","+(itemY - 8 * c.mapRegionY)+" "+itemAmount);
    }

    public void ShowInterface(int interfaceid)
    {
        OutStream.CreateFrame(97);
        OutStream.WriteWord(interfaceid);
        //flushOutStream();
    }

    public void GiveItem(int id, int amount)
    {
        OutStream.CreateFrameVarSizeWord(34);
        OutStream.WriteWord(3214);
        OutStream.WriteByte(4);
        OutStream.WriteWord(id + 1);
        OutStream.WriteByte(amount);//this is correct place but its giving wrong value?
        OutStream.EndFrameVarSizeWord();
    }



    public void Update()
    {
        if (!ClientConnected)
        {
            return;
        }

        // Handle incoming packets
        while (packetHandler != null && packetHandler.InterpreteIncomingPackets()) ;
        if (!ClientConnected) return;

        GiveItem(995, 100);
        //DropItem(995);
        int packetType = packetHandler.PacketType;

        switch (packetType)
        {
            case 0:
                Console.WriteLine("Reset idle packet");
                break;
            case 12:
                Console.WriteLine("Clicked item on ground");
                break;
            case 45:
                Console.WriteLine("Use bank");
                break;
            case 51:
                Console.WriteLine("Open door");
                break;
            case 87:
                Console.WriteLine("Mouse click");
                break;
            case 241: //Mouse Clicks
                int ins = InStream.ReadDWord();
                Console.WriteLine("Mouse clicks");
                break;
            case 147:
                Console.WriteLine("Use quickly bank");
                ShowInterface(5292);
                break;
            default:
                Console.WriteLine("Unknown packet type: " + packetType);
                break;
        }
        //Server.PrintActivePlayers();
        /*
        //test
        // some n00by walking packet
        OutStream.CreateFrame(81);
        OutStream.WriteWord(0);         // placeholder for size of this packet.
        int ofs = OutStream.CurrentOffset;

        // update thisPlayer
        OutStream.InitBitAccess();
        OutStream.WriteBits(1, 1);      // set to true if updating thisPlayer

        // walk code
        OutStream.WriteBits(2, 1);      // updateType - 1=walk in direction
        OutStream.WriteBits(3, 1);      // direction
        OutStream.WriteBits(1, 0);      // set to true, if this player is not in local list yet???



        /*
        // run code
        OutStream.WriteBits(2, 2);      // updateType - 2=run in direction
        OutStream.WriteBits(3, 2);      // direction step 1
        OutStream.WriteBits(3, 1);      // direction step 2
        OutStream.WriteBits(1, 0);      // set to true, if this player is not in local list yet???
        */
        /*

        OutStream.WriteBits(8, 0);      // no other players...
        OutStream.FinishBitAccess();

        OutStream.WriteFrameSizeWord(OutStream.CurrentOffset - ofs);    // write size

        //Console.WriteLine("X: " + mapRegionX + ",Y: " + mapRegionY);
        //test
        */
        //

        // Handle outgoing packets
        try
        {
            FlushOutStream();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Server: Exception!");
            Console.WriteLine(ex);
            ClientConnected = false;
        }

        // Check if the socket is still connected
        if (mySock != null && !mySock.Connected)
        {
            Console.WriteLine("Client lost connection: socket disconnected");
            ClientConnected = false;
        }
    }

    public ClientUtilities GetClientUtilities()
    {
        return clientUtilities;
    }



    public void FlushOutStream()
    {
        try
        {
            if (OutStream != null && outStream != null)
            {
                // Write the buffer to the network stream
                outStream.Write(OutStream.Buffer, 0, OutStream.CurrentOffset);
                OutStream.CurrentOffset = 0; // reset
            }
            else
            {
                Console.WriteLine("Error: Output stream is null.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in FlushOutStream: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
            ClientConnected = false;
        }
    }


    public void Shutdown()
    {
        ClientConnected = false;
        Disconnect();
    }

    public void Kill()
    {
        ClientConnected = false;
        Disconnect();
    }

    private void Disconnect()
    {
        if (mySock != null)
        {
            Console.WriteLine($"ClientHandler: Disconnecting client {MyName}");
            mySock.Close();
            mySock = null;
        }
    }


    // just testing...
    public void CreateNoobyItems()
    {
        // send all items combined to larger clusters
        for (int x = 0; x < 11; x++)
        {
            for (int y = 0; y < 11; y++)
            {
                OutStream.CreateFrame(60);
                OutStream.WriteWord(0);         // placeholder for size of this packet.
                int ofs = OutStream.CurrentOffset;

                OutStream.WriteByte(y * 8);     // baseY
                OutStream.WriteByteC(x * 8);    // baseX
                                                // here come the actual packets
                for (int kx = 0; kx < 8; kx++)
                {
                    for (int ky = 0; ky < 8; ky++)
                    {
                        OutStream.WriteByte(44);        // formerly createFrame, but its just a plain byte in this encapsulated packet
                        OutStream.WriteWordBigEndianA(random.Next(1000));    // objectType
                        OutStream.WriteWord(1);                     // amount
                        OutStream.WriteByte(kx * 16 + ky);                      // x(4 MSB) y(LSB) coords
                    }
                }

                OutStream.WriteFrameSizeWord(OutStream.CurrentOffset - ofs);
            }
        }
    }



}
