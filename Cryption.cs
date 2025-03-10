public class Cryption
{
    private int[] _cryptArray;
    private int[] _keySetArray;
    private int _keyArrayIdx;
    private int _cryptVar1;
    private int _cryptVar2;
    private int _cryptVar3;

    public Cryption(int[] keyArray)
    {
        _cryptArray = new int[256];
        _keySetArray = new int[256];
        for (int i = 0; i < keyArray.Length; i++)
            _keySetArray[i] = keyArray[i];

        InitializeKeySet();
    }

    public int GetNextKey()
    {
        if (_keyArrayIdx-- == 0)
        {
            GenerateNextKeySet();
            _keyArrayIdx = 255;
        }
        return _keySetArray[_keyArrayIdx];
    }

    public void GenerateNextKeySet()
    {
        _cryptVar2 += ++_cryptVar3;
        for (int i = 0; i < 256; i++)
        {
            int j = _cryptArray[i];
            switch (i & 3)
            {
                case 0: _cryptVar1 ^= _cryptVar1 << 13; break;
                case 1: _cryptVar1 ^= (int)((uint)_cryptVar1 >> 6); break;
                case 2: _cryptVar1 ^= _cryptVar1 << 2; break;
                case 3: _cryptVar1 ^= (int)((uint)_cryptVar1 >> 16); break;
            }
            _cryptVar1 += _cryptArray[(i + 128) & 0xFF];
            int k = _cryptArray[(j & 0x3FC) >> 2] + _cryptVar1 + _cryptVar2;
            _cryptArray[i] = k;
            _keySetArray[i] = _cryptVar2 = _cryptArray[(k >> 8 & 0x3FC) >> 2] + j;
        }
    }

    public void InitializeKeySet()
    {
        uint l, i1, j1, k1, l1, i2, j2, k2;
        l = i1 = j1 = k1 = l1 = i2 = j2 = k2 = 0x9E3779B9;

        for (int i = 0; i < 4; i++)
        {
            l ^= i1 << 11; k1 += l; i1 += j1;
            i1 ^= (uint)j1 >> 2; l1 += i1; j1 += k1;
            j1 ^= k1 << 8; i2 += j1; k1 += l1;
            k1 ^= (uint)l1 >> 16; j2 += k1; l1 += i2;
            l1 ^= i2 << 10; k2 += l1; i2 += j2;
            i2 ^= (uint)j2 >> 4; l += i2; j2 += k2;
            j2 ^= k2 << 8; i1 += j2; k2 += l;
            k2 ^= (uint)l >> 9; j1 += k2; l += i1;
        }

        for (int j = 0; j < 256; j += 8)
        {
            l += (uint)_keySetArray[j]; i1 += (uint)_keySetArray[j + 1];
            j1 += (uint)_keySetArray[j + 2]; k1 += (uint)_keySetArray[j + 3];
            l1 += (uint)_keySetArray[j + 4]; i2 += (uint)_keySetArray[j + 5];
            j2 += (uint)_keySetArray[j + 6]; k2 += (uint)_keySetArray[j + 7];
            l ^= i1 << 11; k1 += l; i1 += j1;
            i1 ^= (uint)j1 >> 2; l1 += i1; j1 += k1;
            j1 ^= k1 << 8; i2 += j1; k1 += l1;
            k1 ^= (uint)l1 >> 16; j2 += k1; l1 += i2;
            l1 ^= i2 << 10; k2 += l1; i2 += j2;
            i2 ^= (uint)j2 >> 4; l += i2; j2 += k2;
            j2 ^= k2 << 8; i1 += j2; k2 += l;
            k2 ^= (uint)l >> 9; j1 += k2; l += i1;
            _cryptArray[j] = (int)l; _cryptArray[j + 1] = (int)i1;
            _cryptArray[j + 2] = (int)j1; _cryptArray[j + 3] = (int)k1;
            _cryptArray[j + 4] = (int)l1; _cryptArray[j + 5] = (int)i2;
            _cryptArray[j + 6] = (int)j2; _cryptArray[j + 7] = (int)k2;
        }

        for (int k = 0; k < 256; k += 8)
        {
            l += (uint)_cryptArray[k]; i1 += (uint)_cryptArray[k + 1];
            j1 += (uint)_cryptArray[k + 2]; k1 += (uint)_cryptArray[k + 3];
            l1 += (uint)_cryptArray[k + 4]; i2 += (uint)_cryptArray[k + 5];
            j2 += (uint)_cryptArray[k + 6]; k2 += (uint)_cryptArray[k + 7];
            l ^= i1 << 11; k1 += l; i1 += j1;
            i1 ^= (uint)j1 >> 2; l1 += i1; j1 += k1;
            j1 ^= k1 << 8; i2 += j1; k1 += l1;
            k1 ^= (uint)l1 >> 16; j2 += k1; l1 += i2;
            l1 ^= i2 << 10; k2 += l1; i2 += j2;
            i2 ^= (uint)j2 >> 4; l += i2; j2 += k2;
            j2 ^= k2 << 8; i1 += j2; k2 += l;
            k2 ^= (uint)l >> 9; j1 += k2; l += i1;
            _cryptArray[k] = (int)l; _cryptArray[k + 1] = (int)i1;
            _cryptArray[k + 2] = (int)j1; _cryptArray[k + 3] = (int)k1;
            _cryptArray[k + 4] = (int)l1; _cryptArray[k + 5] = (int)i2;
            _cryptArray[k + 6] = (int)j2; _cryptArray[k + 7] = (int)k2;
        }

        GenerateNextKeySet();
        _keyArrayIdx = 256;
    }


}
