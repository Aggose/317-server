using System.Text;

public class Stream
{
    public byte[] Buffer { get; set; }
    public int CurrentOffset { get; set; }
    public int BitPosition { get; set; }
    public Cryption PacketEncryption { get; set; }

    public Stream(byte[] buffer)
    {
        Buffer = buffer;
        CurrentOffset = 0;
        frameStack = new int[frameStackSize];
        //console.WriteLine($"Stream initialized with buffer of length {buffer.Length}");
    }

    public int ReadUnsignedByte()
    {
        int value = Buffer[CurrentOffset++] & 0xff;
        //console.WriteLine($"ReadUnsignedByte: {value}, CurrentOffset: {CurrentOffset}");
        return value;
    }

    public int readUnsignedByteS()
    {
        int value = 128 - Buffer[CurrentOffset++] & 0xff;
        //console.WriteLine($"readUnsignedByteS: {value}, CurrentOffset: {CurrentOffset}");
        return value;
    }

    public void ReadBytesReverseA(byte[] abyte0, int i, int j)
    {
        for (int k = (j + i) - 1; k >= j; k--)
            abyte0[k] = (byte)(Buffer[CurrentOffset++] - 128);
        //console.WriteLine($"ReadBytesReverseA: {BitConverter.ToString(abyte0)}, CurrentOffset: {CurrentOffset}");
    }

    public int ReadUnsignedWord()
    {
        CurrentOffset += 2;
        int value = ((Buffer[CurrentOffset - 2] & 0xff) << 8) + (Buffer[CurrentOffset - 1] & 0xff);
        //console.WriteLine($"ReadUnsignedWord: {value}, CurrentOffset: {CurrentOffset}");
        return value;
    }

    public int ReadUnsignedWordA()
    {
        CurrentOffset += 2;
        int value = ((Buffer[CurrentOffset - 2] & 0xff) << 8) + (Buffer[CurrentOffset - 1] - 128 & 0xff);
        //console.WriteLine($"ReadUnsignedWordA: {value}, CurrentOffset: {CurrentOffset}");
        return value;
    }

    public int ReadDWord()
    {
        CurrentOffset += 4;
        int value = ((Buffer[CurrentOffset - 4] & 0xff) << 24) + ((Buffer[CurrentOffset - 3] & 0xff) << 16) + ((Buffer[CurrentOffset - 2] & 0xff) << 8) + (Buffer[CurrentOffset - 1] & 0xff);
        //console.WriteLine($"ReadDWord: {value}, CurrentOffset: {CurrentOffset}");
        return value;
    }

    public long ReadQWord()
    {
        long l = ReadDWord() & 0xffffffffL;
        long l1 = ReadDWord() & 0xffffffffL;
        long value = (l << 32) + l1;
        //console.WriteLine($"ReadQWord: {value}, CurrentOffset: {CurrentOffset}");
        return value;
    }

    public string ReadString()
    {
        int i = CurrentOffset;
        while (Buffer[CurrentOffset++] != 10) ;
        string value = Encoding.UTF8.GetString(Buffer, i, CurrentOffset - i - 1);
        //console.WriteLine($"ReadString: {value}, CurrentOffset: {CurrentOffset}");
        return value;
    }

    public void WriteByte(int i)
    {
        Buffer[CurrentOffset++] = (byte)i;
        //console.WriteLine($"WriteByte: {i}, CurrentOffset: {CurrentOffset}");
    }
    public void WriteByteC(int i)
    {
        Buffer[CurrentOffset++] = (byte)(-i);
    }
    public void WriteByteA(int i)
    {
        Buffer[CurrentOffset++] = (byte)(i + 128);
        //console.WriteLine($"WriteByteA: {i}, CurrentOffset: {CurrentOffset}");
    }

    public void WriteBytes(byte[] abyte0, int i, int j)
    {
        for (int k = j; k < j + i; k++)
            Buffer[CurrentOffset++] = abyte0[k];
    }

    public void WriteWord(int i)
    {
        Buffer[CurrentOffset++] = (byte)(i >> 8);
        Buffer[CurrentOffset++] = (byte)i;
        //console.WriteLine($"WriteWord: {i}, CurrentOffset: {CurrentOffset}");
    }

    public void WriteWordA(int i)
    {
        Buffer[CurrentOffset++] = (byte)(i >> 8);
        Buffer[CurrentOffset++] = (byte)(i + 128);
        //console.WriteLine($"WriteWordA: {i}, CurrentOffset: {CurrentOffset}");
    }

    public void WriteDWord(int i)
    {
        Buffer[CurrentOffset++] = (byte)(i >> 24);
        Buffer[CurrentOffset++] = (byte)(i >> 16);
        Buffer[CurrentOffset++] = (byte)(i >> 8);
        Buffer[CurrentOffset++] = (byte)i;
        //console.WriteLine($"WriteDWord: {i}, CurrentOffset: {CurrentOffset}");
    }

    public void WriteQWord(long l)
    {
        Buffer[CurrentOffset++] = (byte)(int)(l >> 56);
        Buffer[CurrentOffset++] = (byte)(int)(l >> 48);
        Buffer[CurrentOffset++] = (byte)(int)(l >> 40);
        Buffer[CurrentOffset++] = (byte)(int)(l >> 32);
        Buffer[CurrentOffset++] = (byte)(int)(l >> 24);
        Buffer[CurrentOffset++] = (byte)(int)(l >> 16);
        Buffer[CurrentOffset++] = (byte)(int)(l >> 8);
        Buffer[CurrentOffset++] = (byte)(int)l;
        //console.WriteLine($"WriteQWord: {l}, CurrentOffset: {CurrentOffset}");
    }

    public void WriteString(string s)
    {
        byte[] strBytes = Encoding.UTF8.GetBytes(s);
        Array.Copy(strBytes, 0, Buffer, CurrentOffset, strBytes.Length);
        CurrentOffset += strBytes.Length;
        Buffer[CurrentOffset++] = 10;
        //console.WriteLine($"WriteString: {s}, CurrentOffset: {CurrentOffset}");
    }

    public void CreateFrame(int i)
    {
        Buffer[CurrentOffset++] = (byte)(i + PacketEncryption.GetNextKey());
        //console.WriteLine($"CreateFrame: {i}, CurrentOffset: {CurrentOffset}");
    }

    public void WriteFrameSize(int i)
    {
        Buffer[CurrentOffset - i - 1] = (byte)i;
        //console.WriteLine($"WriteFrameSize: {i}, CurrentOffset: {CurrentOffset}");
    }

    public void WriteFrameSizeWord(int i)
    {
        Buffer[CurrentOffset - i - 2] = (byte)(i >> 8);
        Buffer[CurrentOffset - i - 1] = (byte)i;
        //console.WriteLine($"WriteFrameSizeWord: {i}, CurrentOffset: {CurrentOffset}");
    }

    private int frameStackPtr;
    private int frameStackSize = 10; // Example size, adjust as needed
    private int[] frameStack;

    public void CreateFrameVarSizeWord(int id)
    {
        Buffer[CurrentOffset++] = (byte)(id + PacketEncryption.GetNextKey());
        WriteWord(0); // place holder for size word
        if (frameStackPtr >= frameStackSize - 1)
        {
            throw new Exception("Stack overflow");
        }
        else
            frameStack[++frameStackPtr] = CurrentOffset;
    }
    public void EndFrameVarSizeWord()
    { // ends a variable sized frame
        if (frameStackPtr < 0)
            throw new Exception("Stack empty");
        else
            WriteFrameSizeWord(CurrentOffset - frameStack[frameStackPtr--]);
    }
    public void InitBitAccess()
    {
        BitPosition = CurrentOffset * 8;
        //console.WriteLine($"InitBitAccess: BitPosition: {BitPosition}");
    }
    public void WriteWordBigEndian(int i)
    {
        Buffer[CurrentOffset++] = (byte)i;
        Buffer[CurrentOffset++] = (byte)(i >> 8);
    }
    public void WriteWordBigEndianA(int i)
    {
        Buffer[CurrentOffset++] = (byte)(i + 128);
        Buffer[CurrentOffset++] = (byte)(i >> 8);
        //console.WriteLine($"WriteWordBigEndianA: {i}, CurrentOffset: {CurrentOffset}");
    }

    public void WriteBits(int numBits, int value)
    {
        int bytePos = BitPosition >> 3;
        int bitOffset = 8 - (BitPosition & 7);
        BitPosition += numBits;
        for (; numBits > bitOffset; bitOffset = 8)
        {
            Buffer[bytePos] &= (byte)~((1 << bitOffset) - 1);
            Buffer[bytePos++] |= (byte)((value >> (numBits - bitOffset)) & ((1 << bitOffset) - 1));
            numBits -= bitOffset;
        }
        if (numBits == bitOffset)
        {
            Buffer[bytePos] &= (byte)~((1 << bitOffset) - 1);
            Buffer[bytePos] |= (byte)(value & ((1 << bitOffset) - 1));
        }
        else
        {
            Buffer[bytePos] &= (byte)~(((1 << numBits) - 1) << (bitOffset - numBits));
            Buffer[bytePos] |= (byte)((value & ((1 << numBits) - 1)) << (bitOffset - numBits));
        }
        //console.WriteLine($"WriteBits: numBits: {numBits}, value: {value}, BitPosition: {BitPosition}");
    }

    public void FinishBitAccess()
    {
        CurrentOffset = (BitPosition + 7) / 8;
        //console.WriteLine($"FinishBitAccess: CurrentOffset: {CurrentOffset}");
    }

    public static int[] BitMaskOut = new int[32];
    static Stream()
    {
        for (int i = 0; i < 32; i++)
            BitMaskOut[i] = (1 << i) - 1;
        //console.WriteLine("Static constructor: BitMaskOut initialized");
    }
}
