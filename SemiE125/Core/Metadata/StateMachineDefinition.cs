// Core/Metadata/StateMachineDefinition.cs
using System.Collections.Generic;

namespace SemiE125.Core.Metadata
{
    /// <summary>
    /// 상태 머신 정의 클래스
    /// </summary>
    public class StateMachineDefinition
    {
        public string StateMachineId { get; set; }
        public string StateMachineName { get; set; }
        public string Description { get; set; }
        public List<StateDefinition> States { get; set; } = new List<StateDefinition>();
        public List<TransitionDefinition> Transitions { get; set; } = new List<TransitionDefinition>();
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 상태 정의 클래스
    /// </summary>
    public class StateDefinition
    {
        public string StateId { get; set; }
        public string StateName { get; set; }
        public string Description { get; set; }
        public bool IsInitialState { get; set; }
    }

    /// <summary>
    /// 전이 정의 클래스
    /// </summary>
    public class TransitionDefinition
    {
        public string TransitionId { get; set; }
        public string FromStateId { get; set; }
        public string ToStateId { get; set; }
        public string EventTrigger { get; set; }
        public string Description { get; set; }
    }
}