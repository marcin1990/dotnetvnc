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
  public class D3Des
  {
    private static UInt32[] sp1 = new UInt32[64]
    {
      0x01010400, 0x00000000, 0x00010000, 0x01010404,
      0x01010004, 0x00010404, 0x00000004, 0x00010000,
      0x00000400, 0x01010400, 0x01010404, 0x00000400,
      0x01000404, 0x01010004, 0x01000000, 0x00000004,
      0x00000404, 0x01000400, 0x01000400, 0x00010400,
      0x00010400, 0x01010000, 0x01010000, 0x01000404,
      0x00010004, 0x01000004, 0x01000004, 0x00010004,
      0x00000000, 0x00000404, 0x00010404, 0x01000000,
      0x00010000, 0x01010404, 0x00000004, 0x01010000,
      0x01010400, 0x01000000, 0x01000000, 0x00000400,
      0x01010004, 0x00010000, 0x00010400, 0x01000004,
      0x00000400, 0x00000004, 0x01000404, 0x00010404,
      0x01010404, 0x00010004, 0x01010000, 0x01000404,
      0x01000004, 0x00000404, 0x00010404, 0x01010400,
      0x00000404, 0x01000400, 0x01000400, 0x00000000,
      0x00010004, 0x00010400, 0x00000000, 0x01010004
    };

    private static UInt32[] sp2 = new UInt32[64]
    {
      0x80108020, 0x80008000, 0x00008000, 0x00108020,
      0x00100000, 0x00000020, 0x80100020, 0x80008020,
      0x80000020, 0x80108020, 0x80108000, 0x80000000,
      0x80008000, 0x00100000, 0x00000020, 0x80100020,
      0x00108000, 0x00100020, 0x80008020, 0x00000000,
      0x80000000, 0x00008000, 0x00108020, 0x80100000,
      0x00100020, 0x80000020, 0x00000000, 0x00108000,
      0x00008020, 0x80108000, 0x80100000, 0x00008020,
      0x00000000, 0x00108020, 0x80100020, 0x00100000,
      0x80008020, 0x80100000, 0x80108000, 0x00008000,
      0x80100000, 0x80008000, 0x00000020, 0x80108020,
      0x00108020, 0x00000020, 0x00008000, 0x80000000,
      0x00008020, 0x80108000, 0x00100000, 0x80000020,
      0x00100020, 0x80008020, 0x80000020, 0x00100020,
      0x00108000, 0x00000000, 0x80008000, 0x00008020,
      0x80000000, 0x80100020, 0x80108020, 0x00108000
    };

    private static UInt32[] sp3 = new UInt32[64]
    {
      0x00000208, 0x08020200, 0x00000000, 0x08020008,
      0x08000200, 0x00000000, 0x00020208, 0x08000200,
      0x00020008, 0x08000008, 0x08000008, 0x00020000,
      0x08020208, 0x00020008, 0x08020000, 0x00000208,
      0x08000000, 0x00000008, 0x08020200, 0x00000200,
      0x00020200, 0x08020000, 0x08020008, 0x00020208,
      0x08000208, 0x00020200, 0x00020000, 0x08000208,
      0x00000008, 0x08020208, 0x00000200, 0x08000000,
      0x08020200, 0x08000000, 0x00020008, 0x00000208,
      0x00020000, 0x08020200, 0x08000200, 0x00000000,
      0x00000200, 0x00020008, 0x08020208, 0x08000200,
      0x08000008, 0x00000200, 0x00000000, 0x08020008,
      0x08000208, 0x00020000, 0x08000000, 0x08020208,
      0x00000008, 0x00020208, 0x00020200, 0x08000008,
      0x08020000, 0x08000208, 0x00000208, 0x08020000,
      0x00020208, 0x00000008, 0x08020008, 0x00020200
    };

    private static UInt32[] sp4 = new UInt32[64]
    {
      0x00802001, 0x00002081, 0x00002081, 0x00000080,
      0x00802080, 0x00800081, 0x00800001, 0x00002001,
      0x00000000, 0x00802000, 0x00802000, 0x00802081,
      0x00000081, 0x00000000, 0x00800080, 0x00800001,
      0x00000001, 0x00002000, 0x00800000, 0x00802001,
      0x00000080, 0x00800000, 0x00002001, 0x00002080,
      0x00800081, 0x00000001, 0x00002080, 0x00800080,
      0x00002000, 0x00802080, 0x00802081, 0x00000081,
      0x00800080, 0x00800001, 0x00802000, 0x00802081,
      0x00000081, 0x00000000, 0x00000000, 0x00802000,
      0x00002080, 0x00800080, 0x00800081, 0x00000001,
      0x00802001, 0x00002081, 0x00002081, 0x00000080,
      0x00802081, 0x00000081, 0x00000001, 0x00002000,
      0x00800001, 0x00002001, 0x00802080, 0x00800081,
      0x00002001, 0x00002080, 0x00800000, 0x00802001,
      0x00000080, 0x00800000, 0x00002000, 0x00802080
    };

    private static UInt32[] sp5 = new UInt32[64]
    {
      0x00000100, 0x02080100, 0x02080000, 0x42000100,
      0x00080000, 0x00000100, 0x40000000, 0x02080000,
      0x40080100, 0x00080000, 0x02000100, 0x40080100,
      0x42000100, 0x42080000, 0x00080100, 0x40000000,
      0x02000000, 0x40080000, 0x40080000, 0x00000000,
      0x40000100, 0x42080100, 0x42080100, 0x02000100,
      0x42080000, 0x40000100, 0x00000000, 0x42000000,
      0x02080100, 0x02000000, 0x42000000, 0x00080100,
      0x00080000, 0x42000100, 0x00000100, 0x02000000,
      0x40000000, 0x02080000, 0x42000100, 0x40080100,
      0x02000100, 0x40000000, 0x42080000, 0x02080100,
      0x40080100, 0x00000100, 0x02000000, 0x42080000,
      0x42080100, 0x00080100, 0x42000000, 0x42080100,
      0x02080000, 0x00000000, 0x40080000, 0x42000000,
      0x00080100, 0x02000100, 0x40000100, 0x00080000,
      0x00000000, 0x40080000, 0x02080100, 0x40000100
    };

    private static UInt32[] sp6 = new UInt32[64]
    {
      0x20000010, 0x20400000, 0x00004000, 0x20404010,
      0x20400000, 0x00000010, 0x20404010, 0x00400000,
      0x20004000, 0x00404010, 0x00400000, 0x20000010,
      0x00400010, 0x20004000, 0x20000000, 0x00004010,
      0x00000000, 0x00400010, 0x20004010, 0x00004000,
      0x00404000, 0x20004010, 0x00000010, 0x20400010,
      0x20400010, 0x00000000, 0x00404010, 0x20404000,
      0x00004010, 0x00404000, 0x20404000, 0x20000000,
      0x20004000, 0x00000010, 0x20400010, 0x00404000,
      0x20404010, 0x00400000, 0x00004010, 0x20000010,
      0x00400000, 0x20004000, 0x20000000, 0x00004010,
      0x20000010, 0x20404010, 0x00404000, 0x20400000,
      0x00404010, 0x20404000, 0x00000000, 0x20400010,
      0x00000010, 0x00004000, 0x20400000, 0x00404010,
      0x00004000, 0x00400010, 0x20004010, 0x00000000,
      0x20404000, 0x20000000, 0x00400010, 0x20004010
    };

    private static UInt32[] sp7 = new UInt32[64]
    {
      0x00200000, 0x04200002, 0x04000802, 0x00000000,
      0x00000800, 0x04000802, 0x00200802, 0x04200800,
      0x04200802, 0x00200000, 0x00000000, 0x04000002,
      0x00000002, 0x04000000, 0x04200002, 0x00000802,
      0x04000800, 0x00200802, 0x00200002, 0x04000800,
      0x04000002, 0x04200000, 0x04200800, 0x00200002,
      0x04200000, 0x00000800, 0x00000802, 0x04200802,
      0x00200800, 0x00000002, 0x04000000, 0x00200800,
      0x04000000, 0x00200800, 0x00200000, 0x04000802,
      0x04000802, 0x04200002, 0x04200002, 0x00000002,
      0x00200002, 0x04000000, 0x04000800, 0x00200000,
      0x04200800, 0x00000802, 0x00200802, 0x04200800,
      0x00000802, 0x04000002, 0x04200802, 0x04200000,
      0x00200800, 0x00000000, 0x00000002, 0x04200802,
      0x00000000, 0x00200802, 0x04200000, 0x00000800,
      0x04000002, 0x04000800, 0x00000800, 0x00200002
    };

    private static UInt32[] sp8 = new UInt32[64]
    {
      0x10001040, 0x00001000, 0x00040000, 0x10041040,
      0x10000000, 0x10001040, 0x00000040, 0x10000000,
      0x00040040, 0x10040000, 0x10041040, 0x00041000,
      0x10041000, 0x00041040, 0x00001000, 0x00000040,
      0x10040000, 0x10000040, 0x10001000, 0x00001040,
      0x00041000, 0x00040040, 0x10040040, 0x10041000,
      0x00001040, 0x00000000, 0x00000000, 0x10040040,
      0x10000040, 0x10001000, 0x00041040, 0x00040000,
      0x00041040, 0x00040000, 0x10041000, 0x00001000,
      0x00000040, 0x10040040, 0x00001000, 0x00041040,
      0x10001000, 0x00000040, 0x10000040, 0x10040000,
      0x10040040, 0x10000000, 0x00040000, 0x10001040,
      0x00000000, 0x10041040, 0x00040040, 0x10000040,
      0x10040000, 0x10001000, 0x10001040, 0x00000000,
      0x10041040, 0x00041000, 0x00041000, 0x00001040,
      0x00001040, 0x00040040, 0x10000000, 0x10041000
    };

    private static void Scrunch(byte[] outOf, UInt32 offset, UInt32[] into)
    {
      into[0] = (UInt32)outOf[offset] << 24 | (UInt32)outOf[offset + 1] << 16 | (UInt32)outOf[offset + 2] << 8 | (UInt32)outOf[offset + 3];
      into[1] = (UInt32)outOf[offset + 4] << 24 | (UInt32)outOf[offset + 5] << 16 | (UInt32)outOf[offset + 6] << 8 | (UInt32)outOf[offset + 7];
    }

    private static void UnScrun(UInt32[] outOf, byte[] into, UInt32 offset)
    {
      into[offset] = (byte)((outOf[0] >> 24) & 0x000000FF);
      into[offset + 1] = (byte)((outOf[0] >> 16) & 0x000000FF);
      into[offset + 2] = (byte)((outOf[0] >> 8) & 0x000000FF);
      into[offset + 3] = (byte)(outOf[0] & 0x000000FF);
      into[offset + 4] = (byte)((outOf[1] >> 24) & 0x000000FF);
      into[offset + 5] = (byte)((outOf[1] >> 16) & 0x000000FF);
      into[offset + 6] = (byte)((outOf[1] >> 8) & 0x000000FF);
      into[offset + 7] = (byte)(outOf[1] & 0x000000FF);
    }

    private static void DesFunc(UInt32[] block, D3DesKey key)
    {
      UInt32 work;

      UInt32 leftt = block[0];
      UInt32 right = block[1];
      work = ((leftt >> 4) ^ right) & 0x0F0F0F0F;
      right ^= work;
      leftt ^= (work << 4);
      work = ((leftt >> 16) ^ right) & 0x0000FFFF;
      right ^= work;
      leftt ^= (work << 16);
      work = ((right >> 2) ^ leftt) & 0x33333333;
      leftt ^= work;
      right ^= (work << 2);
      work = ((right >> 8) ^ leftt) & 0x00FF00FF;
      leftt ^= work;
      right ^= (work << 8);
      right = ((right << 1) | ((right >> 31) & 1)) & 0xFFFFFFFF;
      work = (leftt ^ right) & 0xAAAAAAAA;
      leftt ^= work;
      right ^= work;
      leftt = ((leftt << 1) | ((leftt >> 31) & 1)) & 0xFFFFFFFF;

      int keyIndex = 0;
      for(int round = 0; round < 8; round++)
      {
        UInt32 fval;

        work = (right << 28) | (right >> 4);
        work ^= key[keyIndex++];
        fval = sp7[work & 0x3F];
        fval |= sp5[(work >> 8) & 0x3F];
        fval |= sp3[(work >> 16) & 0x3F];
        fval |= sp1[(work >> 24) & 0x3F];
        work = right ^ key[keyIndex++];
        fval |= sp8[work & 0x3F];
        fval |= sp6[(work >> 8) & 0x3F];
        fval |= sp4[(work >> 16) & 0x3F];
        fval |= sp2[(work >> 24) & 0x3F];
        leftt ^= fval;
        work = (leftt << 28) | (leftt >> 4);
        work ^= key[keyIndex++];
        fval = sp7[work & 0x3F];
        fval |= sp5[(work >> 8) & 0x3F];
        fval |= sp3[(work >> 16) & 0x3F];
        fval |= sp1[(work >> 24) & 0x3F];
        work = leftt ^ key[keyIndex++];
        fval |= sp8[work & 0x3F];
        fval |= sp6[(work >> 8) & 0x3F];
        fval |= sp4[(work >> 16) & 0x3F];
        fval |= sp2[(work >> 24) & 0x3F];
        right ^= fval;
      }

      right = (right << 31) | (right >> 1);
      work = (leftt ^ right) & 0xAAAAAAAA;
      leftt ^= work;
      right ^= work;
      leftt = (leftt << 31) | (leftt >> 1);
      work = ((leftt >> 8) ^ right) & 0x00FF00FF;
      right ^= work;
      leftt ^= (work << 8);
      work = ((leftt >> 2) ^ right) & 0x33333333;
      right ^= work;
      leftt ^= (work << 2);
      work = ((right >> 16) ^ leftt) & 0x0000FFFF;
      leftt ^= work;
      right ^= (work << 16);
      work = ((right >> 4) ^ leftt) & 0x0F0F0F0F;
      leftt ^= work;
      right ^= (work << 4);
      block[0] = right;
      block[1] = leftt;
    }

    public static void Des(byte[] from, byte[] to, int length, D3DesKey key)
    {
      UInt32[] work = new UInt32[2];
      for(int i = 0; i < length; i += 8)
      {
        Scrunch(from, (UInt32)i, work);
        DesFunc(work, key);
        UnScrun(work, to, (UInt32)i);
      }
    }
  }
}
