using System.Net.Sockets;

public class ClientUtilities
{
    private Stream OutStream;

    public ClientUtilities(Stream outStream)
    {
        OutStream = outStream;
    }


}
