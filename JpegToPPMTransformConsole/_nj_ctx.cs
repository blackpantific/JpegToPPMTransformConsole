using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JpegToPPMTransformConsole.Program;

namespace JpegToPPMTransformConsole
{
    public class _nj_ctx
    {
        public _nj_result error;
        public byte[] pos;
        public int size;
        public int length;
        public int width, height;
        public int mbwidth, mbheight;
        public int mbsizex, mbsizey;
        public int ncomp;
        public _nj_cmp[] comp = new _nj_cmp[3];
        public int qtused, qtavail;
        public byte[,] qtab = new byte[4,64];
        public _nj_code[,] vlctab = new _nj_code[4, 65536];
        public int buf, bufbits;
        public int[] block = new int[64];
        public int rstinterval;
        public byte[] rgb;

        public int Shift { get; set; }
        public byte GetElement(int shift)
        {
            return pos[Shift + shift];
        }
    }
}
