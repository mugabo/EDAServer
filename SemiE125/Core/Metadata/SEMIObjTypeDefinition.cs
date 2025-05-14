// Core/Metadata/SEMIObjTypeDefinition.cs
using System.Collections.Generic;

namespace SemiE125.Core.Metadata
{
    /// <summary>
    /// SEMI 표준 객체 타입 정의 클래스
    /// </summary>
    public class SEMIObjTypeDefinition
    {
        public string ObjTypeId { get; set; }
        public string ObjTypeName { get; set; }
        public string StandardReference { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }
}