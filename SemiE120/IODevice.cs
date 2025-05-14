using System;
using System.Collections.Generic;

namespace SemiE120.CEM
{
    /// <summary>
    /// IODevice - 센서, 액츄에이터 또는 지능형 액츄에이터/센서 장치를 모델링하는 클래스
    /// SEMI E120 표준에 따른 구현
    /// </summary>
    public class IODevice : EquipmentElement
    {
        private object _value;
        private DateTime _timestamp;
        private StatusCode _statusCode;

        /// <summary>
        /// IO 장치의 현재 값
        /// </summary>
        public object Value
        {
            get { return _value; }
            set
            {
                _value = value;
                _timestamp = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 값이 마지막으로 업데이트된 타임스탬프
        /// </summary>
        public DateTime Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        /// <summary>
        /// 값의 상태 코드 (Good, Bad, Uncertain 등)
        /// </summary>
        public StatusCode StatusCode
        {
            get { return _statusCode; }
            set { _statusCode = value; }
        }

        /// <summary>
        /// 장치의 엔지니어링 단위 (예: "mV", "°C", "bar" 등)
        /// </summary>
        public string EngineeringUnits { get; set; }

        /// <summary>
        /// 값의 범위를 정의하는 속성
        /// </summary>
        public Range Range { get; set; }

        /// <summary>
        /// IO 장치가 읽기 전용인지 여부
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public IODevice()
        {
            _timestamp = DateTime.UtcNow;
            _statusCode = StatusCode.Good;
            IsReadOnly = true; // 기본적으로 읽기 전용
        }

        /// <summary>
        /// 초기 값을 설정하는 생성자
        /// </summary>
        public IODevice(object initialValue) : this()
        {
            Value = initialValue;
        }

        /// <summary>
        /// 값을 지정된 타입으로 변환해서 반환
        /// </summary>
        public T GetValueAs<T>()
        {
            if (_value == null)
                return default;

            try
            {
                return (T)Convert.ChangeType(_value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// 값이 유효한지 확인
        /// </summary>
        public bool IsValueValid()
        {
            return _statusCode == StatusCode.Good;
        }
    }

    /// <summary>
    /// 값의 상태를 나타내는 열거형
    /// </summary>
    public enum StatusCode : uint
    {
        Good = 0,
        Uncertain = 0x40000000,
        Bad = 0x80000000
    }

    /// <summary>
    /// 값의 범위를 정의하는 클래스
    /// </summary>
    public class Range
    {
        /// <summary>
        /// 최대값
        /// </summary>
        public double High { get; set; }

        /// <summary>
        /// 최소값
        /// </summary>
        public double Low { get; set; }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public Range()
        {
        }

        /// <summary>
        /// 최대값과 최소값을 설정하는 생성자
        /// </summary>
        public Range(double high, double low)
        {
            High = high;
            Low = low;
        }

        /// <summary>
        /// 범위 내에 값이 있는지 확인
        /// </summary>
        public bool IsInRange(double value)
        {
            return value >= Low && value <= High;
        }
    }
}