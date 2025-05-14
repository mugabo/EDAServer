// Core/Metadata/EquipmentStructure.cs
using System.Collections.Generic;

namespace SemiE125.Core.Metadata
{
    /// <summary>
    /// 장비 구조 클래스
    /// </summary>
    public class EquipmentStructure
    {
        public string EquipmentUid { get; set; }
        public string EquipmentName { get; set; }
        public string EquipmentType { get; set; }
        public string Description { get; set; }
        public List<EquipmentNode> Modules { get; set; } = new List<EquipmentNode>();
        public List<EquipmentNode> Subsystems { get; set; } = new List<EquipmentNode>();
        public List<EquipmentNode> IODevices { get; set; } = new List<EquipmentNode>();
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 장비 노드 클래스
    /// </summary>
    public class EquipmentNode
    {
        public string NodeId { get; set; }
        public string NodeName { get; set; }
        public string NodeType { get; set; }
        public string Description { get; set; }
        public List<EquipmentNode> Children { get; set; } = new List<EquipmentNode>();
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }
}