using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JpegToPPMTransformConsole
{
    public struct _nj_cmp
    {
        public int cid;
        public int ssx, ssy;
        public int width, height;
        public int stride;
        public int qtsel;
        public int actabsel, dctabsel;
        public int dcpred;
        public byte[] pixels;
    }
}
