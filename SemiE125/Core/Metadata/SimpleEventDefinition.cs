// Core/Metadata/SimpleEventDefinition.cs
using System.Collections.Generic;

namespace SemiE125.Core.Metadata
{
    /// <summary>
    /// 간단한 이벤트 정의 클래스
    /// </summary>
    public class SimpleEventDefinition
    {
        public string EventId { get; set; }
        public string EventName { get; set; }
        public string EventType { get; set; }  // INFO, WARNING, ERROR, PROCESS, SYSTEM, etc.
        public string Description { get; set; }
        public string NodeId { get; set; }     // 이벤트 발생 소스 노드
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }
}