//  Translated to C# and
//  Copyright (C) 2004-2005 Rocky Lo. All Rights Reserved.

/*
 * This is D3DES (V5.09) by Richard Outerbridge with the double and
 * triple-length support removed for use in VNC.  Also the bytebit[] array
 * has been reversed so that the most significant bit in each byte of the
 * key is ignored, not the least significant.
 *
 * These changes are
 * Copyright (C) 1999 AT&T Laboratories Cambridge. All Rights Reserved.
 *
 * This software is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 */

/* D3DES (V5.09) -
 *
 * A portable, public domain, version of the Data Encryption Standard.
 *
 * Written with Symantec's THINK (Lightspeed) C by Richard Outerbridge.
 * Thanks to: Dan Hoey for his excellent Initial and Inverse permutation
 * code;  Jim Gillogly & Phil Karn for the DES key schedule code; Dennis
 * Ferguson, Eric Young and Dana How for comparing notes; and Ray Lau,
 * for humouring me on.
 *
 * Copyright (c) 1988,1989,1990,1991,1992 by Richard Outerbridge.
 * (GEnie : OUTER; CIS : [71755,204]) Graven Imagery, 1992.
 */

using System;

namespace Vnc.Security
{
  public enum EncryptMode : short
  {
    EN0 = 0,
    DE1 = 1
  }

  public class D3DesKey
  {
    private static UInt16[] bytebit = new UInt16[8]
    {
      0x0001, 0x0002, 0x0004, 0x0008, 0x0010, 0x0020, 0x0040, 0x0080
    };

    private static UInt32[] bigbyte = new UInt32[24]
    {
      0x00800000, 0x00400000, 0x00200000, 0x00100000,
      0x00080000, 0x00040000, 0x00020000, 0x00010000,
      0x00008000, 0x00004000, 0x00002000, 0x00001000,
      0x00000800, 0x00000400, 0x00000200, 0x00000100,
      0x00000080, 0x00000040, 0x00000020, 0x00000010,
      0x00000008, 0x00000004, 0x00000002, 0x00000001
    };

    private static byte[] pc1 = new byte[56]
    {
      56, 48, 40, 32, 24, 16, 8, 0, 57, 49, 41, 33, 25, 17,
      9, 1, 58, 50, 42, 34, 26, 18, 10, 2, 59, 51, 43, 35,
      62, 54, 46, 38, 30, 22, 14, 6, 61, 53, 45, 37, 29, 21,
      13, 5, 60, 52, 44, 36, 28, 20, 12, 4, 27, 19, 11, 3
    };

    private static byte[] pc2 = new byte[48]
    {
      13, 16, 10, 23, 0, 4, 2, 27, 14, 5, 20, 9,
      22, 18, 11, 3, 25, 7, 15, 6, 26, 19, 12, 1,
      40, 51, 30, 36, 46, 54, 29, 39, 50, 44, 32, 47,
      43, 48, 38, 55, 33, 52, 45, 41, 49, 35, 28, 31
    };

    private static byte[] totrot = new byte[16]
    {
      1, 2, 4, 6, 8, 10, 12, 14, 15, 17, 19, 21, 23, 25, 27, 28
    };

    private UInt32[] knL = new UInt32[32];

    public UInt32 this[int index]
    {
      get
      {
        return knL[index];
      }
    }

    private void cookey(UInt32[] raw)
    {
      int j;
      for(int i = 0; i < 32; i += 2)
      {
        j = i + 1;
        knL[i] = (raw[i] & 0x00FC0000) << 6;
        knL[i] |= (raw[i] & 0x00000FC0) << 10;
        knL[i] |= (raw[j] & 0x00FC0000) >> 10;
        knL[i] |= (raw[j] & 0x00000FC0) >> 6;
        knL[j] = (raw[i] & 0x0003F000) << 12;
        knL[j] |= (raw[i] & 0x0000003F) << 16;
        knL[j] |= (raw[j] & 0x0003F000) >> 4;
        knL[j] |= (raw[j] & 0x0000003F);
      }
    }

    public D3DesKey(byte[] key, EncryptMode mode)
    {
      int l;
      int m;

      byte[] pc1m = new byte[56];
      byte[] pcr = new byte[56];
      UInt32[] kn = new UInt32[32];

      for(int j = 0; j < 56; j++)
      {
        l = pc1[j];
        m = l & 7;
        pc1m[j] = (byte)((key[l >> 3] & bytebit[m]) != 0? 1 : 0);
      }
      for(int i = 0; i < 16; i++)
      {
        int n;

        if(mode == EncryptMode.DE1)
          m = (15 - i) << 1;
        else if(mode == EncryptMode.EN0)
          m = i << 1;
        else
          throw new ArgumentException("mode", "Not a valid encryption mode");
        n = m + 1;
        kn[m] = 0;
        kn[n] = 0;
        for(int j = 0; j < 28; j++)
        {
          l = j + totrot[i];
          if(l < 28)
            pcr[j] = pc1m[l];
          else
            pcr[j] = pc1m[l - 28];
        }
        for(int j = 28; j < 56; j++)
        {
          l = j + totrot[i];
          if(l < 56)
            pcr[j] = pc1m[l];
          else
            pcr[j] = pc1m[l - 28];
        }
        for(int j = 0; j < 24; j++)
        {
          if(pcr[pc2[j]] != 0)
            kn[m] |= bigbyte[j];
          if(pcr[pc2[j + 24]] != 0)
            kn[n] |= bigbyte[j];
        }
      }
      cookey(kn);
    }
  }
}
