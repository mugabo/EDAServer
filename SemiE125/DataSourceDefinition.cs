// DataSourceDefinition.cs
namespace SemiE125.Core.DataCollection
{
    public class DataSourceDefinition : SemiE120.CEM.Nameable
    {
        /// <summary>
        /// 데이터 소스 타입(OPC UA, 직접 I/O, 파일 등)
        /// </summary>
        public DataSourceType SourceType { get; set; }

        /// <summary>
        /// 데이터 소스 위치 또는 경로
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// 데이터 유형 정보
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 샘플링 주기 (ms)
        /// </summary>
        public int SamplingRate { get; set; }

        /// <summary>
        /// 데이터 수집 활성화 여부
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 데이터 수집 우선순위
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 연결된 장비의 UID
        /// </summary>
        public string EquipmentUid { get; set; }

        /// <summary>
        /// 데이터 소스 유효성 검사
        /// </summary>
        public bool Validate()
        {
            return !string.IsNullOrEmpty(Name) &&
                   !string.IsNullOrEmpty(SourcePath);
        }
    }

    public enum DataSourceType
    {
        OpcUa,
        DirectIO,
        File,
        Database,
        Custom
    }
}