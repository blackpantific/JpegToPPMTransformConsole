using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JpegToPPMTransformConsole
{
    public class Program
    {
        public enum _nj_result
        {
            NJ_OK = 0, // no error, decoding successful
            NJ_NO_JPEG, // not a JPEG file
            NJ_UNSUPPORTED, // unsupported format
            NJ_OUT_OF_MEM, // out of memory
            NJ_INTERNAL_ERR, // internal error
            NJ_SYNTAX_ERROR, // syntax error
            __NJ_FINISHED // used internally, will never be reported
        }

        public static _nj_ctx nj { get; set; }




        static void Main(string[] args)
        {
            int size;
            byte[] buffer;
            FileStream fileStream;

            
            using (fileStream = File.OpenRead(args[0]))
            {
                var fileSize = fileStream.Length;
                buffer = new byte[fileSize];

                fileStream.Read(buffer, 0, Convert.ToInt32(fileSize));

            }

            nj = new _nj_ctx();        //njInit();
            if (njDecode(buffer, buffer.Length) != 0)
            {
                Console.WriteLine("Error decoding the input file.\n");
                return;
            }
        }

        public static _nj_result njDecode(byte[] jpeg, int size)
        {
            njDone();
            nj.pos = jpeg;
            nj.size = size & 0x7FFFFFFF;
            if (nj.size < 2)
            {
                return _nj_result.NJ_NO_JPEG;
            }
            if (((nj.GetElement(0) ^ 0xFF) | (nj.GetElement(1) ^ 0xD8)) != 0)
            {
                return _nj_result.NJ_NO_JPEG;
            }
            njSkip(2);
            while (nj.error == 0)
            {
                if ((nj.size < 2) || (nj.GetElement(0) != 0xFF))
                {
                    //return NJ_SYNTAX_ERROR;
                }
                njSkip(2);
                switch (nj.GetElement(-1))
                {
                    case 0xC0:
                        njDecodeSOF();
                        break;
                    case 0xC4:
                        njDecodeDHT();
                        break;
                    case 0xDB:
                        njDecodeDQT();
                        break;
                    case 0xDD:
                        //njDecodeDRI();
                        break;
                    case 0xDA:
                        njDecodeScan();
                        break;
                    case 0xFE:
                        njSkipMarker();
                        break;
                    default:
                        {
                            if ((nj.GetElement(-1) & 0xF0) == 0xE0)
                            {
                                njSkipMarker();
                            }
                            else
                            {
                                return _nj_result.NJ_UNSUPPORTED;
                            }
                        }
                        break;
                        
                }
            }
            //if (nj.error != __NJ_FINISHED)
            //{
            //    return nj.error;
            //}
            //nj.error = NJ_OK;
            //njConvert();
            return nj.error;


        }
        public static void njDone()
        {
            int i;
            for (i = 0; i < 3; ++i)
            {
                if (nj.comp[i].pixels != null)
                {
                    nj.comp[i].pixels = null;//???????
                    //njFreeMem((void*)nj.comp[i].pixels);
                }
            }
            if (nj.rgb != null)
            {
                nj.rgb = null;
                //njFreeMem((object)nj.rgb);
            }
            nj = new _nj_ctx();
        }

        public static void njDecodeSOF()
        {
            int i, ssxmax = 0, ssymax = 0;
            _nj_cmp[] c;
            njDecodeLength();
            if(!njCheckError())
                return;
            if (nj.length < 9)
            {
                if (!njThrow(_nj_result.NJ_SYNTAX_ERROR))
                    return;
            }
            if (nj.GetElement(0) != 8)
            {
                if (!njThrow(_nj_result.NJ_UNSUPPORTED))
                    return;
            }

            var ca = nj.GetElement(0);
            nj.height = njDecode16(1);
            nj.width = njDecode16(3);

            if (nj.width == 0 || nj.height == 0)
            {
                njThrow(_nj_result.NJ_SYNTAX_ERROR);
            }
            nj.ncomp = nj.GetElement(5);
            njSkip(6);
            switch (nj.ncomp)
            {
                case 1:
                case 3:
                    break;
                default:
                    njThrow(_nj_result.NJ_UNSUPPORTED);
                    break;
            }
            if (nj.length < (nj.ncomp * 3))
            {
                njThrow(_nj_result.NJ_SYNTAX_ERROR);
            }
            int j = 0;
            for (i = 0, c = nj.comp, j = 0; i < nj.ncomp; ++i, ++j)//c = nj.comp; i < nj.ncomp; ++i, ++c
            {
                c[j].cid = nj.GetElement(0);
                if ((c[j].ssx = nj.GetElement(1) >> 4) == 0)
                {
                    njThrow(_nj_result.NJ_SYNTAX_ERROR);
                }
                if ((c[j].ssx & (c[j].ssx - 1)) != 0)
                {
                    njThrow(_nj_result.NJ_UNSUPPORTED); // non-power of two
                }
                if ((c[j].ssy = nj.GetElement(1) & 15) == 0)
                {
                    njThrow(_nj_result.NJ_SYNTAX_ERROR);
                }
                if ((c[j].ssy & (c[j].ssy - 1)) != 0)
                {
                    njThrow(_nj_result.NJ_UNSUPPORTED); // non-power of two
                }
                if (((c[j].qtsel = nj.GetElement(2)) & 0xFC) != 0)
                {
                    njThrow(_nj_result.NJ_SYNTAX_ERROR);
                }
                njSkip(3);
                nj.qtused |= 1 << c[j].qtsel;
                if (c[j].ssx > ssxmax)
                {
                    ssxmax = c[j].ssx;
                }
                if (c[j].ssy > ssymax)
                {
                    ssymax = c[j].ssy;
                }
            }
            if (nj.ncomp == 1)
            {
                c = nj.comp;
                c[j].ssx = c[j].ssy = ssxmax = ssymax = 1;
            }

            nj.mbsizex = ssxmax << 3;
            nj.mbsizey = ssymax << 3;
            nj.mbwidth = (nj.width + nj.mbsizex - 1) / nj.mbsizex;
            nj.mbheight = (nj.height + nj.mbsizey - 1) / nj.mbsizey;
            j = 0;
            for (i = 0, c = nj.comp; i < nj.ncomp; ++i, ++j)
            {
                c[j].width = (nj.width * c[j].ssx + ssxmax - 1) / ssxmax;
                c[j].height = (nj.height * c[j].ssy + ssymax - 1) / ssymax;
                c[j].stride = nj.mbwidth * c[j].ssx << 3;
                if (((c[j].width < 3) && (c[j].ssx != ssxmax)) || ((c[j].height < 3) && (c[j].ssy != ssymax)))
                {
                    njThrow(_nj_result.NJ_UNSUPPORTED);
                }
                if ((c[j].pixels = new byte[(c[j].stride * nj.mbheight * c[j].ssy << 3)]).Length == 0)
                {
                    njThrow(_nj_result.NJ_OUT_OF_MEM);
                }
            }
            if (nj.ncomp == 3)
            {
                nj.rgb = new byte[(nj.width * nj.height * nj.ncomp)];
                if (nj.rgb.Length == 0)
                {
                    njThrow(_nj_result.NJ_OUT_OF_MEM);
                }
            }
            njSkip(nj.length);


        }

        public static void njDecodeLength()
        {
            if (nj.size < 2)
            {
                if (!njThrow(_nj_result.NJ_SYNTAX_ERROR))//предположение, что return должен быть как возврат из этой функции
                    return;
            }
            nj.length = njDecode16(0);//0, потому что смещение nj.Shift, то есть 0
            if (nj.length > nj.size)
            {
                if (!njThrow(_nj_result.NJ_SYNTAX_ERROR))//предположение, что return должен быть как возврат из этой функции
                    return;
            }
            njSkip(2);
        }


        public static bool njThrow(_nj_result e)
        {
            nj.error = e;
            return false;
            
        }//проверить, должно возвращать 0/1
        public static ushort njDecode16(int shift)
        {
            var c = (nj.GetElement(0) << 8) | nj.GetElement(1);
            return (ushort)((nj.GetElement(0 + shift) << 8) | nj.GetElement(1 + shift));
        }
        public static bool njCheckError()//проверить, должно возвращать 0/1
        {
            if (nj.error != 0)
                return false;
            return true;
        }

        public static void njSkip(int count)
        {
            nj.Shift += count;
            nj.size -= count;
            nj.length -= count;
            if (nj.size < 0) 
                nj.error = _nj_result.NJ_SYNTAX_ERROR;
        }

        public static void njSkipMarker()
        {
            njDecodeLength();
            njSkip(nj.length);
        }

        public static void njDecodeDQT()
        {
            int i;
            byte[] t;
            njDecodeLength();
            njCheckError();
            while (nj.length >= 65)
            {
                i = nj.GetElement(0);
                if ((i & 0xFC) != 0)
                {
                    njThrow(_nj_result.NJ_SYNTAX_ERROR);
                }
                nj.qtavail |= 1 << i;
                //t = nj.qtab[i,0];
                t = nj.qtab.ExtractOneDimensionalArray(i);
                for (i = 0; i < 64; ++i)
                {
                    t[i] = nj.GetElement(i + 1);
                }
                njSkip(65);
            }
            if (nj.length != 0)
            {
                njThrow(_nj_result.NJ_SYNTAX_ERROR);
            }
        }

        public static void njDecodeDHT()
        {
            int codelen, currcnt, remain, spread, i, j;
            _nj_code[] vlc;
            byte[] counts = new byte[16];
            njDecodeLength();
            njCheckError();
            while (nj.length >= 17)
            {
                i = nj.GetElement(0);
                if ((i & 0xEC) != 0)
                {
                    njThrow(_nj_result.NJ_SYNTAX_ERROR);
                }
                if ((i & 0x02) != 0)
                {
                    njThrow(_nj_result.NJ_UNSUPPORTED);
                }
                i = (i | (i >> 3)) & 3; // combined DC/AC + tableid value
                for (codelen = 1; codelen <= 16; ++codelen)
                {
                    counts[codelen - 1] = nj.GetElement(codelen);
                }
                njSkip(17);
                vlc = nj.vlctab.ExtractOneDimensionalArray(i);
                int iterator = 0;
                remain = spread = 65536;
                for (codelen = 1; codelen <= 16; ++codelen)
                {
                    spread >>= 1;
                    currcnt = counts[codelen - 1];
                    if (currcnt == 0)
                    {
                        continue;
                    }
                    if (nj.length < currcnt)
                    {
                        njThrow(_nj_result.NJ_SYNTAX_ERROR);
                    }
                    remain -= currcnt << (16 - codelen);
                    if (remain < 0)
                    {
                        njThrow(_nj_result.NJ_SYNTAX_ERROR);
                    }
                    for (i = 0; i < currcnt; ++i)
                    {
                        byte code = nj.GetElement(i);
                        for (j = spread; j != 0; --j)
                        {
                            vlc[iterator].bits = (byte)codelen;
                            vlc[iterator].code = code;
                            ++iterator;
                        }
                    }
                    njSkip(currcnt);
                }
                while ((remain--) != 0)
                {
                    vlc[iterator].bits = 0;
                    ++iterator;
                }
            }
            if (nj.length != 0)
            {
                njThrow(_nj_result.NJ_SYNTAX_ERROR);
            }
        }

        public static void njDecodeScan()
        {
            int i, mbx, mby, sbx, sby;
            int rstcount = nj.rstinterval;
            int nextrst = 0;
            _nj_cmp[] c;
            njDecodeLength();
            njCheckError();
            if (nj.length < (4 + 2 * nj.ncomp))
            {
                njThrow(_nj_result.NJ_SYNTAX_ERROR);
            }
            if (nj.pos[0] != nj.ncomp)
            {
                njThrow(_nj_result.NJ_UNSUPPORTED);
            }
            njSkip(1);
            int j = 0;
            for (i = 0, c = nj.comp; i < nj.ncomp; ++i, ++j)
            {
                if (nj.pos[0] != c[j].cid)
                {
                    njThrow(_nj_result.NJ_SYNTAX_ERROR);
                }
                if ((nj.pos[1] & 0xEE) != 0)
                {
                    njThrow(_nj_result.NJ_SYNTAX_ERROR);
                }
                c[j].dctabsel = nj.pos[1] >> 4;
                c[j].actabsel = (nj.pos[1] & 1) | 2;
                njSkip(2);
            }
            if (nj.GetElement(0) != 0 || (nj.GetElement(1) != 63) || nj.GetElement(2) != 0)
            {
                njThrow(_nj_result.NJ_UNSUPPORTED);
            }
            njSkip(nj.length);
            j = 0;
            for (mbx = mby = 0; ;)
            {
                for (i = 0, c = nj.comp; i < nj.ncomp; ++i, ++j)
                {
                    for (sby = 0; sby < c[j].ssy; ++sby)
                    {
                        for (sbx = 0; sbx < c[j].ssx; ++sbx)
                        {
                            njDecodeBlock(ref c[j], ref c[j].pixels, (((mby * c[j].ssy + sby) * c[j].stride + mbx * c[j].ssx + sbx) << 3));
                            njCheckError();
                        }
                    }
                }
                if (++mbx >= nj.mbwidth)
                {
                    mbx = 0;
                    if (++mby >= nj.mbheight)
                    {
                        break;
                    }
                }
                if (nj.rstinterval != 0 && (--rstcount) == 0)
                {
                    njByteAlign();
                    i = njGetBits(16);
                    if (((i & 0xFFF8) != 0xFFD0) || ((i & 7) != nextrst))
                    {
                        njThrow(_nj_result.NJ_SYNTAX_ERROR);
                    }
                    nextrst = (nextrst + 1) & 7;
                    rstcount = nj.rstinterval;
                    for (i = 0; i < 3; ++i)
                    {
                        nj.comp[i].dcpred = 0;
                    }
                }
            }
            nj.error = _nj_result.__NJ_FINISHED;
        }

        public static void njDecodeBlock(ref _nj_cmp c, ref byte[] @out, int shift)
        {
            byte code = 0;
            int value;
            int coef = 0;
            //njFillMem(nj.block, 0, sizeof(nj.block)); не надо поскольку массив в C# уже заполнен нулями при создании
            c.dcpred += njGetVLC(nj.vlctab[c.dctabsel][0], null);
            nj.block[0] = (c.dcpred) * nj.qtab[c.qtsel][0];
            do
            {
                value = njGetVLC(nj.vlctab[c.actabsel][0], code);
                if (code == 0)
                {
                    break; // EOB
                }
                if ((code & 0x0F) == 0 && (code != 0xF0))
                {
                    njThrow(_nj_result.NJ_SYNTAX_ERROR);
                }
                coef += (code >> 4) + 1;
                if (coef > 63)
                {
                    njThrow(_nj_result.NJ_SYNTAX_ERROR);
                }
                nj.block[(int)njZZ[coef]] = value * nj.qtab[c.qtsel][coef];
            } while (coef < 63);

            for (coef = 0; coef < 64; coef += 8)
            {
                njRowIDCT(nj.block[coef]);
            }
            for (coef = 0; coef < 8; ++coef)
            {
                njColIDCT(nj.block[coef], @out[coef], c.stride);
            }

        }


    }
}
