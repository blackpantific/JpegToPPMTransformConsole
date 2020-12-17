using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JpegToPPMTransformConsole
{
    public static class ArrayExtention
    {
        public static byte[] ExtractOneDimensionalArray(this byte[,] array, int position)
        {
            var dim = array.GetLength(1);
            var outputArray = new byte[dim];
            for (int i = 0; i < dim; i++)
            {
                outputArray[i] = array[position, i];
            }
            return outputArray;
        }

        public static _nj_code[] ExtractOneDimensionalArray(this _nj_code[,] array, int position)
        {
            var dim = array.GetLength(1);
            var outputArray = new _nj_code[dim];
            for (int i = 0; i < dim; i++)
            {
                outputArray[i] = array[position, i];
            }
            return outputArray;
        }
    }
}
