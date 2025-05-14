using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemiE125.Core.DataCollection
{
    /// <summary>
    /// 데이터 샘플링 전략 인터페이스
    /// </summary>
    public interface ISamplingStrategy
    {
        /// <summary>
        /// 원시 데이터에 샘플링 전략 적용
        /// </summary>
        /// <param name="rawData">원시 데이터</param>
        /// <returns>샘플링 적용된 데이터</returns>
        byte[] ApplySampling(byte[] rawData);
    }
}
