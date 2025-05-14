// Nameable.cs
using System;
using System.Collections.Generic;

namespace SemiE120.CEM
{
    /// <summary>
    /// Nameable - 루트 클래스로, 장비 계층 구조의 각 컴포넌트에 대한 고유 식별 제공
    /// </summary>
    public abstract class Nameable
    {
        /// <summary>
        /// Nameable을 고유하게 식별하는 식별자
        /// </summary>
        public string Uid { get; set; }

        /// <summary>
        /// Nameable의 사람이 읽을 수 있는 이름
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Nameable에 대한 설명
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Nameable과 연결된 Extension 객체들
        /// </summary>
        public List<Extension> Extensions { get; set; } = new List<Extension>();

        /// <summary>
        /// Nameable과 연결된 LogicalElement 객체들
        /// </summary>
        public List<LogicalElement> LogicalElements { get; set; } = new List<LogicalElement>();
    }

    /// <summary>
    /// EquipmentElement - 장비 구조의 각 하드웨어 컴포넌트에 대한 기본 정보
    /// </summary>
    public abstract class EquipmentElement : Nameable
    {
        /// <summary>
        /// EquipmentElement의 유형
        /// </summary>
        public string ElementType { get; set; }

        /// <summary>
        /// EquipmentElement의 제조업체 이름
        /// </summary>
        public string Supplier { get; set; }

        /// <summary>
        /// EquipmentElement의 하드웨어 제조사
        /// </summary>
        public string Make { get; set; }

        /// <summary>
        /// EquipmentElement의 하드웨어 모델
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// EquipmentElement의 하드웨어 모델 리비전
        /// </summary>
        public string ModelRevision { get; set; }

        /// <summary>
        /// 장비 내 이 EquipmentElement의 역할
        /// </summary>
        public string Function { get; set; }

        /// <summary>
        /// EquipmentElement의 변경되지 않는 식별자(예: 일련번호)
        /// </summary>
        public string ImmutableId { get; set; }

        /// <summary>
        /// EquipmentElement와 연결된 SoftwareModule 객체들
        /// </summary>
        public List<SoftwareModule> SoftwareModules { get; set; } = new List<SoftwareModule>();
    }

    /// <summary>
    /// ExecutionElement - 소재를 처리, 측정 또는 테스트할 수 있는 장비 구조의 부분 모델링
    /// </summary>
    public abstract class ExecutionElement : EquipmentElement
    {
        /// <summary>
        /// 이 ExecutionElement에서 발생하는 처리 설명
        /// </summary>
        public string ProcessName { get; set; }

        /// <summary>
        /// ExecutionElement의 주요 처리 기능 분류
        /// </summary>
        public ProcessType ProcessTypeValue { get; set; }

        /// <summary>
        /// 동일한 recipeType 문자열을 가진 ExecutionElement 인스턴스는 
        /// 동일한 레시피(처리 지침) 세트를 실행할 수 있습니다.
        /// </summary>
        public string RecipeType { get; set; }
    }

    /// <summary>
    /// 처리 유형 열거형
    /// </summary>
    public enum ProcessType
    {
        Measurement,
        Process,
        Storage,
        Transport
    }
}