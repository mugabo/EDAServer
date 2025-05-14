using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemiE125.Core.DataCollection
{
    public class DefaultSamplingStrategy : ISamplingStrategy
    {
        public byte[] ApplySampling(byte[] rawData)
        {
            // 기본 구현에서는 모든 데이터를 유지
            return rawData;
        }
    }
}
