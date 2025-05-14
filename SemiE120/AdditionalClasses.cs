// AdditionalClasses.cs
namespace SemiE120.CEM
{
    /// <summary>
    /// LogicalElement - 장비 모델 내에서 일정한 비구조 요소의 표현을 허용하기 위한 클래스
    /// </summary>
    public class LogicalElement : Nameable
    {
        /// <summary>
        /// LogicalElement의 유형 및 목적을 식별
        /// </summary>
        public string ElementType { get; set; }
    }

    /// <summary>
    /// 소재 유형 열거형
    /// </summary>
    public enum MaterialType
    {
        Carrier,
        Substrate,
        ProcessDurable,
        Consumable,
        Other
    }

    /// <summary>
    /// MaterialLocation - 특정 장비 구성요소가 소재를 보유하는 능력을 모델링
    /// </summary>
    public class MaterialLocation : Nameable
    {
        /// <summary>
        /// 보유하는 소재의 유형
        /// </summary>
        public MaterialType MaterialTypeValue { get; set; }

        /// <summary>
        /// 소재 유형의 추가 구분을 제공하는 구현자 정의 용어
        /// </summary>
        public string MaterialSubType { get; set; }
    }

    /// <summary>
    /// SoftwareModule - 장비 및 장비 구성요소에서 사용 중인 장비 시스템 소프트웨어의 존재 및 버전 설명
    /// </summary>
    public class SoftwareModule
    {
        /// <summary>
        /// SoftwareModule의 이름
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// SoftwareModule을 만든 회사의 이름
        /// </summary>
        public string Supplier { get; set; }

        /// <summary>
        /// SoftwareModule의 기능 또는 목적에 대한 설명
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// SoftwareModule의 버전 번호
        /// </summary>
        public string Version { get; set; }
    }
}