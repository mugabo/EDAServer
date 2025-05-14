// Equipment.cs
using System.Collections.Generic;

namespace SemiE120.CEM
{
    /// <summary>
    /// Equipment - 장비 전체를 모델링
    /// </summary>
    public class Equipment : ExecutionElement
    {
        /// <summary>
        /// Equipment에 속한 Module 객체들
        /// </summary>
        public List<Module> Modules { get; set; } = new List<Module>();

        /// <summary>
        /// Equipment에 속한 Subsystem 객체들
        /// </summary>
        public List<Subsystem> Subsystems { get; set; } = new List<Subsystem>();

        /// <summary>
        /// Equipment에 속한 IODevice 객체들
        /// </summary>
        public List<IODevice> IODevices { get; set; } = new List<IODevice>();

        /// <summary>
        /// Equipment에 속한 MaterialLocation 객체들
        /// </summary>
        public List<MaterialLocation> MaterialLocations { get; set; } = new List<MaterialLocation>();

        /// <summary>
        /// Equipment에 속한 다른 Equipment 객체들
        /// </summary>
        public List<Equipment> EquipmentList { get; set; } = new List<Equipment>();
    }

    /// <summary>
    /// Module - 프로세스 챔버와 같은 장비의 주요 서브시스템을 모델링
    /// </summary>
    public class Module : ExecutionElement
    {
        /// <summary>
        /// Module에 속한 다른 Module 객체들
        /// </summary>
        public List<Module> Modules { get; set; } = new List<Module>();

        /// <summary>
        /// Module에 속한 Subsystem 객체들
        /// </summary>
        public List<Subsystem> Subsystems { get; set; } = new List<Subsystem>();

        /// <summary>
        /// Module에 속한 IODevice 객체들
        /// </summary>
        public List<IODevice> IODevices { get; set; } = new List<IODevice>();

        /// <summary>
        /// Module에 속한 MaterialLocation 객체들
        /// </summary>
        public List<MaterialLocation> MaterialLocations { get; set; } = new List<MaterialLocation>();
    }

    /// <summary>
    /// Subsystem - 장비의 서브시스템 및 하위 어셈블리 구성 요소를 모델링
    /// </summary>
    public class Subsystem : EquipmentElement
    {
        /// <summary>
        /// Subsystem에 속한 다른 Subsystem 객체들
        /// </summary>
        public List<Subsystem> Subsystems { get; set; } = new List<Subsystem>();

        /// <summary>
        /// Subsystem에 속한 IODevice 객체들
        /// </summary>
        public List<IODevice> IODevices { get; set; } = new List<IODevice>();

        /// <summary>
        /// Subsystem에 속한 MaterialLocation 객체들
        /// </summary>
        public List<MaterialLocation> MaterialLocations { get; set; } = new List<MaterialLocation>();
    }
}