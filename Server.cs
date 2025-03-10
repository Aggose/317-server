using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;

public class Server
{
    public static bool ShutdownServer = false;
    private static TcpListener? clientListener = null;
    private static bool shutdownClientHandler;
    private static int serverListenerPort = 43594;
    public static int serverTick = 600;// 600ms interval
    private static System.Timers.Timer serverTimer;
    private static List<ClientHandler> clientHandlers = new List<ClientHandler>();

    public static int GetActivePlayerCount()
    {
        return clientHandlers.Count(handler => handler.ClientConnected);
    }

    public static void PrintActivePlayers()
    {
        var activePlayers = clientHandlers.Where(handler => handler.ClientConnected).ToList();
        Console.WriteLine($"Active Players: {activePlayers.Count}");
        foreach (var player in activePlayers)
        {
            Console.WriteLine($"PlayerName: {player.MyName}");
        }
    }


    public static async Task Main(string[] args)
    {
        // Start the server listener
        StartListener();

        // Initialize and start the server timer
        serverTimer = new System.Timers.Timer(serverTick); 
        serverTimer.Elapsed += OnServerTimerElapsed;
        serverTimer.AutoReset = true;
        serverTimer.Start();

        // Keep the main thread alive until the server is shut down
        while (!ShutdownServer)
        {

            await Task.Delay(100); // Small delay to prevent busy-waiting
        }

        // Stop the server timer
        serverTimer.Stop();
        serverTimer.Dispose();

        // Shut down the server
        StopListener();
    }

    private static void OnServerTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // Perform periodic server tasks here
        // For example, game updating stuff
        // Console.WriteLine("Server timer tick.");

        // Create a copy of the clientHandlers collection
        var clientHandlersCopy = clientHandlers.ToList();

        // Update all client handlers
        foreach (var clientHandler in clientHandlersCopy)
        {
            clientHandler.Update();

        }
    }

    public static async void StartListener()
    {
        if (clientListener == null)
        {
            try
            {
                shutdownClientHandler = false;
                clientListener = new TcpListener(IPAddress.Any, serverListenerPort);
                clientListener.Start();
                Console.WriteLine("Starting Woxen Server on " + ((IPEndPoint)clientListener.LocalEndpoint).Address + ":" + serverListenerPort);
                while (true)
                {
                    Socket s = await clientListener.AcceptSocketAsync();
                    s.NoDelay = true;
                    string connectingHost = ((IPEndPoint)s.RemoteEndPoint).Address.ToString();
                    if (true) // Allow all connections for now
                    {
                        Console.WriteLine("ClientHandler: Accepted from " + connectingHost + ":" + ((IPEndPoint)s.RemoteEndPoint).Port);
                        _ = HandleClientAsync(s); // Fire and forget
                    }
                    else
                    {
                        Console.WriteLine("ClientHandler: Rejected " + connectingHost + ":" + ((IPEndPoint)s.RemoteEndPoint).Port);
                        s.Close();
                    }
                }
            }
            catch (IOException ioe)
            {
                if (!shutdownClientHandler)
                    Console.WriteLine("Error: Unable to startup listener on " + serverListenerPort + " - port already in use?");
                else
                    Console.WriteLine("ClientHandler was shut down.");
            }
        }
    }

    private static async Task HandleClientAsync(Socket socket)
    {
        var clientHandler = new ClientHandler(socket);
        clientHandlers.Add(clientHandler);
        var clientUtilities = new ClientUtilities(clientHandler.OutStream);
        clientHandler.Run(clientUtilities);
    }

    public static void StopListener()
    {
        try
        {
            shutdownClientHandler = true;
            if (clientListener != null) clientListener.Stop();
            clientListener = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
