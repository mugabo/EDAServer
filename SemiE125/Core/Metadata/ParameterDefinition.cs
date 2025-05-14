// Core/Metadata/ParameterDefinition.cs
using System.Collections.Generic;

namespace SemiE125.Core.Metadata
{
    /// <summary>
    /// 매개변수 정의 클래스
    /// </summary>
    public class ParameterDefinition
    {
        public string ParameterId { get; set; }
        public string ParameterName { get; set; }
        public string DataType { get; set; }  // int, double, string, boolean, enum
        public string DefaultValue { get; set; }
        public string MinValue { get; set; }
        public string MaxValue { get; set; }
        public string UnitId { get; set; }
        public string Category { get; set; }  // PROCESS, GAS, CHAMBER, ROBOT, ALARM
        public string NodeId { get; set; }    // 연관된 장비 노드 ID
        public string Description { get; set; }
        public bool ReadOnly { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }
}