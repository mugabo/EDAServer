// Core/Metadata/EquipmentNodeDescription.cs
using System.Collections.Generic;

namespace SemiE125.Core.Metadata
{
    /// <summary>
    /// 장비 노드 상세 설명 클래스
    /// </summary>
    public class EquipmentNodeDescription
    {
        public string NodeId { get; set; }
        public string NodeName { get; set; }
        public string NodeType { get; set; }
        public string Description { get; set; }
        public List<string> ParameterIds { get; set; } = new List<string>();
        public List<string> ExceptionIds { get; set; } = new List<string>();
        public List<string> ObjTypeIds { get; set; } = new List<string>();
        public List<string> StateMachineIds { get; set; } = new List<string>();
        public List<string> SimpleEventIds { get; set; } = new List<string>();
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }
}