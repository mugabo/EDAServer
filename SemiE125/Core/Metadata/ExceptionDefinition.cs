// Core/Metadata/ExceptionDefinition.cs
using System.Collections.Generic;

namespace SemiE125.Core.Metadata
{
    /// <summary>
    /// 예외 정의 클래스
    /// </summary>
    public class ExceptionDefinition
    {
        public string ExceptionId { get; set; }
        public string ExceptionName { get; set; }
        public string ExceptionCode { get; set; }
        public string Severity { get; set; }  // INFO, WARNING, ERROR, CRITICAL
        public string Category { get; set; }  // SYSTEM, VACUUM, POWER, PROCESS, ROBOT, SAFETY
        public string Description { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }
}