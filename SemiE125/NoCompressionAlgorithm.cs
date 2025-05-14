using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemiE125.Core.DataCollection
{
    public class NoCompressionAlgorithm : ICompressionAlgorithm
    {
        public byte[] Compress(byte[] data)
        {
            // 압축 없이 그대로 반환
            return data;
        }

        public byte[] Decompress(byte[] data)
        {
            // 압축 해제 없이 그대로 반환
            return data;
        }
    }
}
