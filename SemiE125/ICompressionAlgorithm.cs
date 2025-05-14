using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemiE125.Core.DataCollection
{
    /// <summary>
    /// 데이터 압축 알고리즘 인터페이스
    /// </summary>
    public interface ICompressionAlgorithm
    {
        /// <summary>
        /// 데이터 압축
        /// </summary>
        /// <param name="data">원본 데이터</param>
        /// <returns>압축된 데이터</returns>
        byte[] Compress(byte[] data);

        /// <summary>
        /// 데이터 압축 해제
        /// </summary>
        /// <param name="data">압축된 데이터</param>
        /// <returns>원본 데이터</returns>
        byte[] Decompress(byte[] data);
    }
}
