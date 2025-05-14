// Core/Metadata/TypeDefinition.cs
using System.Collections.Generic;

namespace SemiE125.Core.Metadata
{
    /// <summary>
    /// 타입 정의 클래스
    /// </summary>
    public class TypeDefinition
    {
        public string TypeId { get; set; }
        public string TypeName { get; set; }
        public string BaseType { get; set; }
        public string Description { get; set; }
        public List<TypeProperty> Properties { get; set; } = new List<TypeProperty>();
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 타입 속성 클래스
    /// </summary>
    public class TypeProperty
    {
        public string PropertyName { get; set; }
        public string PropertyType { get; set; }
        public bool IsRequired { get; set; }
        public string Description { get; set; }
        public string DefaultValue { get; set; }
        public string UnitId { get; set; }
    }
}