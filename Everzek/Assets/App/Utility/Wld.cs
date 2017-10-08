using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Using https://github.com/daeken/OpenEQ/blob/master/converter/wld.py for inspiration.

// Wld is used to load *.wld files and manage the details within it.
public class Wld  {

    bool old = false;
    int offset = 0;
    byte[] data;
    string stringTable;
    bool baked = false;
    List<int> frags;

    public Wld(byte[] contents)
    {
        data = contents;
        if (UInt() != 0x54503D02) throw new System.Exception("Invalid header data for wld file");
        old = UInt()  == 0x00015500;
        var fragCount = UInt();
        offset += 8;
        var hashlen = UInt();
        offset += 4;
        stringTable = DecodeString(hashlen);
        Debug.Log("Loading " + fragCount + " frags");
        for (int i = 0; i < fragCount; i++)
        {
            var size = UInt();
            var type = UInt();
            var nameoff = Int();
            string name = GetString(-nameoff);
        }
    }

    private UInt32 UInt() {
        UInt32 value = BitConverter.ToUInt32(data, offset);
        offset += 4;
        return value;
    }

    private Int32 Int()
    {
        Int32 value = BitConverter.ToInt32(data, offset);
        offset += 4;
        return value;
    }

    private string GetString(int offset)
    {
        if (offset == 0x1000000) return "";
        return "";
        //self.stringTable[i:].split('\0', 1)[0]
    }
    private string DecodeString(uint hashlen)
    {
        offset += (int)hashlen; 
        return "";
       // return ''.join(chr(ord(x) ^ Wld.xorkey[i % len(Wld.xorkey)]) for i, x in enumerate(s))
    }

}
