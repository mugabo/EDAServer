// EquipmentMetadataManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SemiE120.CEM;
using SemiE125.Core.DataCollection;

namespace SemiE125.Core.Metadata
{
    /// <summary>
    /// SEMI E125 장비 메타데이터 관리 클래스
    /// </summary>
    public class EquipmentMetadataManager
    {
        private readonly ILogger<EquipmentMetadataManager> _logger;
        private readonly IDataSourceRepository _dataSourceRepository;
        private readonly Dictionary<string, EquipmentMetadata> _equipmentMetadataCache;

        public EquipmentMetadataManager(
            ILogger<EquipmentMetadataManager> logger,
            IDataSourceRepository dataSourceRepository)
        {
            _logger = logger;
            _dataSourceRepository = dataSourceRepository;
            _equipmentMetadataCache = new Dictionary<string, EquipmentMetadata>();
        }

        /// <summary>
        /// 장비 메타데이터 생성 또는 업데이트
        /// </summary>
        public async Task<EquipmentMetadata> CreateOrUpdateMetadataAsync(Equipment equipment)
        {
            try
            {
                _logger.LogInformation($"장비 '{equipment.Name}' 메타데이터 생성/업데이트 시작");

                // 기존 메타데이터 확인
                if (_equipmentMetadataCache.TryGetValue(equipment.Uid, out var existingMetadata))
                {
                    _logger.LogDebug($"장비 '{equipment.Name}'의 기존 메타데이터 업데이트");
                    UpdateMetadata(existingMetadata, equipment);
                    return existingMetadata;
                }

                // 새 메타데이터 생성
                var metadata = new EquipmentMetadata
                {
                    EquipmentUid = equipment.Uid,
                    EquipmentName = equipment.Name,
                    MetadataVersion = "1.0",
                    CreatedTimestamp = DateTime.UtcNow,
                    LastModifiedTimestamp = DateTime.UtcNow
                };

                // 장비 세부 정보 설정
                metadata.Attributes.Add("Supplier", equipment.Supplier);
                metadata.Attributes.Add("Model", equipment.Model);
                metadata.Attributes.Add("ElementType", equipment.ElementType);
                metadata.Attributes.Add("ProcessType", equipment.ProcessTypeValue.ToString());

                // 데이터 소스 정보 연결
                await LinkDataSourcesAsync(metadata, equipment);

                // 캐시에 저장
                _equipmentMetadataCache[equipment.Uid] = metadata;

                _logger.LogInformation($"장비 '{equipment.Name}' 메타데이터 생성 완료");
                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"장비 '{equipment.Name}' 메타데이터 생성/업데이트 실패");
                throw;
            }
        }

        /// <summary>
        /// 기존 메타데이터 업데이트
        /// </summary>
        private void UpdateMetadata(EquipmentMetadata metadata, Equipment equipment)
        {
            metadata.EquipmentName = equipment.Name;
            metadata.LastModifiedTimestamp = DateTime.UtcNow;

            // 속성 업데이트
            metadata.Attributes["Supplier"] = equipment.Supplier;
            metadata.Attributes["Model"] = equipment.Model;
            metadata.Attributes["ElementType"] = equipment.ElementType;
            metadata.Attributes["ProcessType"] = equipment.ProcessTypeValue.ToString();

            // 버전 증가
            var versionParts = metadata.MetadataVersion.Split('.');
            if (int.TryParse(versionParts[1], out int minorVersion))
            {
                metadata.MetadataVersion = $"{versionParts[0]}.{minorVersion + 1}";
            }
        }

        /// <summary>
        /// 데이터 소스 연결
        /// </summary>
        private async Task LinkDataSourcesAsync(EquipmentMetadata metadata, Equipment equipment)
        {
            try
            {
                // 장비의 데이터 소스 가져오기
                var dataSources = await _dataSourceRepository.GetAllAsync();
                var equipmentDataSources = dataSources.Where(ds => ds.EquipmentUid == equipment.Uid).ToList();

                // IO 장치에 대한 데이터 소스 연결
                foreach (var ioDevice in GetAllIODevices(equipment))
                {
                    var dataSource = equipmentDataSources.FirstOrDefault(ds => ds.SourcePath.EndsWith(ioDevice.Name));
                    if (dataSource != null)
                    {
                        var metadataItem = new MetadataItem
                        {
                            ItemId = ioDevice.Uid,
                            ItemName = ioDevice.Name,
                            ItemType = "IODevice",
                            Description = ioDevice.Description,
                            DataSourceUid = dataSource.Uid
                        };

                        // 데이터 형식 정보 추가
                        if (ioDevice.Value != null)
                        {
                            metadataItem.Attributes.Add("DataType", ioDevice.Value.GetType().Name);
                            metadataItem.Attributes.Add("EngineeringUnits", ioDevice.EngineeringUnits ?? "");
                        }

                        metadata.Items.Add(metadataItem);
                    }
                }

                _logger.LogInformation($"장비 '{equipment.Name}'에 {metadata.Items.Count}개의 메타데이터 항목 연결됨");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"데이터 소스 연결 중 오류 발생");
            }
        }

        /// <summary>
        /// 장비의 모든 IO 장치 목록 가져오기
        /// </summary>
        private List<IODevice> GetAllIODevices(Equipment equipment)
        {
            var devices = new List<IODevice>(equipment.IODevices);

            // 모듈 내의 IO 장치
            foreach (var module in equipment.Modules)
            {
                devices.AddRange(module.IODevices);

                // 중첩된 모듈 내의 IO 장치
                foreach (var nestedModule in module.Modules)
                {
                    devices.AddRange(nestedModule.IODevices);
                }
            }

            // 서브시스템 내의 IO 장치
            foreach (var subsystem in equipment.Subsystems)
            {
                devices.AddRange(subsystem.IODevices);

                // 중첩된 서브시스템 내의 IO 장치
                foreach (var nestedSubsystem in subsystem.Subsystems)
                {
                    devices.AddRange(nestedSubsystem.IODevices);
                }
            }

            return devices;
        }

        /// <summary>
        /// 장비 메타데이터 조회
        /// </summary>
        public EquipmentMetadata GetMetadata(string equipmentUid)
        {
            if (_equipmentMetadataCache.TryGetValue(equipmentUid, out var metadata))
            {
                return metadata;
            }

            return null;
        }

        /// <summary>
        /// 장비의 단위 메타데이터 조회
        /// </summary>
        public IList<UnitDefinition> GetUnits(string equipmentUid)
        {
            try
            {
                _logger.LogInformation($"장비 '{equipmentUid}' 단위 메타데이터 조회 시작");

                // 장비에 따라 단위 목록을 다르게 제공할 수 있음
                var equipment = GetMetadata(equipmentUid);
                if (equipment == null)
                {
                    _logger.LogWarning($"장비 '{equipmentUid}'를 찾을 수 없습니다.");
                    return new List<UnitDefinition>();
                }

                // 스퍼터링 장비에 특화된 단위 목록
                var units = new List<UnitDefinition>();

                // 진공 측정 관련 단위
                units.Add(new UnitDefinition
                {
                    UnitId = "pressure_pa",
                    UnitName = "파스칼",
                    UnitSymbol = "Pa",
                    Description = "압력 단위 - 파스칼"
                });

                units.Add(new UnitDefinition
                {
                    UnitId = "pressure_mbar",
                    UnitName = "밀리바",
                    UnitSymbol = "mbar",
                    Description = "압력 단위 - 밀리바"
                });

                units.Add(new UnitDefinition
                {
                    UnitId = "pressure_torr",
                    UnitName = "토르",
                    UnitSymbol = "Torr",
                    Description = "압력 단위 - 토르(진공 측정에 흔히 사용)"
                });

                // 온도 관련 단위
                units.Add(new UnitDefinition
                {
                    UnitId = "temp_c",
                    UnitName = "섭씨",
                    UnitSymbol = "°C",
                    Description = "온도 단위 - 섭씨"
                });

                // 가스 유량 관련 단위 (MFC)
                units.Add(new UnitDefinition
                {
                    UnitId = "flow_sccm",
                    UnitName = "SCCM",
                    UnitSymbol = "sccm",
                    Description = "가스 유량 단위 - 표준 세제곱 센티미터/분(Standard Cubic Centimeters per Minute)"
                });

                units.Add(new UnitDefinition
                {
                    UnitId = "flow_slm",
                    UnitName = "SLM",
                    UnitSymbol = "slm",
                    Description = "가스 유량 단위 - 표준 리터/분(Standard Liters per Minute)"
                });

                // 전력 관련 단위 (스퍼터링 전원 공급 장치)
                units.Add(new UnitDefinition
                {
                    UnitId = "power_w",
                    UnitName = "와트",
                    UnitSymbol = "W",
                    Description = "전력 단위 - 와트"
                });

                units.Add(new UnitDefinition
                {
                    UnitId = "power_kw",
                    UnitName = "킬로와트",
                    UnitSymbol = "kW",
                    Description = "전력 단위 - 킬로와트"
                });

                // 전압 관련 단위
                units.Add(new UnitDefinition
                {
                    UnitId = "voltage_v",
                    UnitName = "볼트",
                    UnitSymbol = "V",
                    Description = "전압 단위 - 볼트"
                });

                // 전류 관련 단위
                units.Add(new UnitDefinition
                {
                    UnitId = "current_a",
                    UnitName = "암페어",
                    UnitSymbol = "A",
                    Description = "전류 단위 - 암페어"
                });

                // 시간 관련 단위
                units.Add(new UnitDefinition
                {
                    UnitId = "time_s",
                    UnitName = "초",
                    UnitSymbol = "s",
                    Description = "시간 단위 - 초"
                });

                units.Add(new UnitDefinition
                {
                    UnitId = "time_min",
                    UnitName = "분",
                    UnitSymbol = "min",
                    Description = "시간 단위 - 분"
                });

                // RF 관련 단위 (프리클린 챔버)
                units.Add(new UnitDefinition
                {
                    UnitId = "frequency_hz",
                    UnitName = "헤르츠",
                    UnitSymbol = "Hz",
                    Description = "주파수 단위 - 헤르츠"
                });

                units.Add(new UnitDefinition
                {
                    UnitId = "frequency_mhz",
                    UnitName = "메가헤르츠",
                    UnitSymbol = "MHz",
                    Description = "주파수 단위 - 메가헤르츠(RF 전원 주파수)"
                });

                _logger.LogInformation($"장비 '{equipmentUid}' 단위 메타데이터 조회 완료: {units.Count}개 단위 정의");
                return units;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"단위 메타데이터 조회 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 장비의 타입 정의 메타데이터 조회
        /// </summary>
        public IList<TypeDefinition> GetTypeDefinitions(string equipmentUid)
        {
            try
            {
                _logger.LogInformation($"장비 '{equipmentUid}' 타입 정의 메타데이터 조회 시작");

                // 장비에 따라 타입 정의 목록을 다르게 제공할 수 있음
                var equipment = GetMetadata(equipmentUid);
                if (equipment == null)
                {
                    _logger.LogWarning($"장비 '{equipmentUid}'를 찾을 수 없습니다.");
                    return new List<TypeDefinition>();
                }

                // 스퍼터링 장비에 특화된 타입 정의 목록
                var typeDefinitions = new List<TypeDefinition>();

                // Wafer 타입 정의
                var waferType = new TypeDefinition
                {
                    TypeId = "type_wafer_300mm",
                    TypeName = "Wafer300mm",
                    BaseType = "Substrate",
                    Description = "300mm 반도체 웨이퍼"
                };

                waferType.Properties.Add(new TypeProperty
                {
                    PropertyName = "Diameter",
                    PropertyType = "double",
                    IsRequired = true,
                    Description = "웨이퍼 직경",
                    DefaultValue = "300.0",
                    UnitId = "length_mm"
                });

                // 나머지 속성들도 모두 MetadataTypeProperty로 변경...

                typeDefinitions.Add(waferType);

                // Recipe, Target, Chamber 타입 정의도 유사하게 수정...

                return typeDefinitions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"타입 정의 메타데이터 조회 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 장비의 상태 머신 메타데이터 조회
        /// </summary>
        public IList<StateMachineDefinition> GetStateMachines(string equipmentUid)
        {
            try
            {
                _logger.LogInformation($"장비 '{equipmentUid}' 상태 머신 메타데이터 조회 시작");

                // 장비에 따라 상태 머신 목록을 다르게 제공할 수 있음
                var equipment = GetMetadata(equipmentUid);
                if (equipment == null)
                {
                    _logger.LogWarning($"장비 '{equipmentUid}'를 찾을 수 없습니다.");
                    return new List<StateMachineDefinition>();
                }

                // 스퍼터링 장비에 특화된 상태 머신 목록
                var stateMachines = new List<StateMachineDefinition>();

                // 1. 장비 상태 머신 (Equipment State Machine) - SEMI E10 기반
                var equipmentStateMachine = new StateMachineDefinition
                {
                    StateMachineId = "sm_equipment_e10",
                    StateMachineName = "EquipmentStateMachine",
                    Description = "SEMI E10 기반 장비 상태 머신"
                };

                // 상태 정의
                equipmentStateMachine.States.Add(new StateDefinition
                {
                    StateId = "non_scheduled",
                    StateName = "NON_SCHEDULED",
                    Description = "장비가 예약되지 않음 (비가동 시간)",
                    IsInitialState = false
                });

                equipmentStateMachine.States.Add(new StateDefinition
                {
                    StateId = "engineering",
                    StateName = "ENGINEERING",
                    Description = "엔지니어링 작업 중",
                    IsInitialState = false
                });

                equipmentStateMachine.States.Add(new StateDefinition
                {
                    StateId = "scheduled_down",
                    StateName = "SCHEDULED_DOWN",
                    Description = "계획된 다운타임",
                    IsInitialState = false
                });

                equipmentStateMachine.States.Add(new StateDefinition
                {
                    StateId = "unscheduled_down",
                    StateName = "UNSCHEDULED_DOWN",
                    Description = "계획되지 않은 다운타임",
                    IsInitialState = false
                });

                equipmentStateMachine.States.Add(new StateDefinition
                {
                    StateId = "standby",
                    StateName = "STANDBY",
                    Description = "대기 중 (가동 가능 상태)",
                    IsInitialState = true
                });

                equipmentStateMachine.States.Add(new StateDefinition
                {
                    StateId = "productive",
                    StateName = "PRODUCTIVE",
                    Description = "생산 중",
                    IsInitialState = false
                });

                // 전이 정의
                equipmentStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_standby_to_productive",
                    FromStateId = "standby",
                    ToStateId = "productive",
                    EventTrigger = "START_PROCESS",
                    Description = "생산 시작"
                });

                equipmentStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_productive_to_standby",
                    FromStateId = "productive",
                    ToStateId = "standby",
                    EventTrigger = "PROCESS_COMPLETE",
                    Description = "생산 완료"
                });

                equipmentStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_standby_to_scheduled_down",
                    FromStateId = "standby",
                    ToStateId = "scheduled_down",
                    EventTrigger = "SCHEDULE_MAINTENANCE",
                    Description = "계획된 유지보수 시작"
                });

                equipmentStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_scheduled_down_to_standby",
                    FromStateId = "scheduled_down",
                    ToStateId = "standby",
                    EventTrigger = "MAINTENANCE_COMPLETE",
                    Description = "유지보수 완료"
                });

                equipmentStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_any_to_unscheduled_down",
                    FromStateId = "*",
                    ToStateId = "unscheduled_down",
                    EventTrigger = "EQUIPMENT_FAILURE",
                    Description = "장비 고장"
                });

                equipmentStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_unscheduled_down_to_standby",
                    FromStateId = "unscheduled_down",
                    ToStateId = "standby",
                    EventTrigger = "REPAIR_COMPLETE",
                    Description = "수리 완료"
                });

                stateMachines.Add(equipmentStateMachine);

                // 2. 프로세스 상태 머신 (PVD Chamber Process State Machine)
                var processStateMachine = new StateMachineDefinition
                {
                    StateMachineId = "sm_pvd_process",
                    StateMachineName = "PVDProcessStateMachine",
                    Description = "PVD 공정 챔버 상태 머신"
                };

                // 상태 정의
                processStateMachine.States.Add(new StateDefinition
                {
                    StateId = "idle",
                    StateName = "IDLE",
                    Description = "챔버 대기 중",
                    IsInitialState = true
                });

                processStateMachine.States.Add(new StateDefinition
                {
                    StateId = "wafer_loading",
                    StateName = "WAFER_LOADING",
                    Description = "웨이퍼 로딩 중",
                    IsInitialState = false
                });

                processStateMachine.States.Add(new StateDefinition
                {
                    StateId = "pumping",
                    StateName = "PUMPING",
                    Description = "진공 펌핑 중",
                    IsInitialState = false
                });

                processStateMachine.States.Add(new StateDefinition
                {
                    StateId = "heating",
                    StateName = "HEATING",
                    Description = "기판 가열 중",
                    IsInitialState = false
                });

                processStateMachine.States.Add(new StateDefinition
                {
                    StateId = "pre_sputtering",
                    StateName = "PRE_SPUTTERING",
                    Description = "타겟 프리스퍼터링 중",
                    IsInitialState = false
                });

                processStateMachine.States.Add(new StateDefinition
                {
                    StateId = "sputtering",
                    StateName = "SPUTTERING",
                    Description = "스퍼터링 공정 중",
                    IsInitialState = false
                });

                processStateMachine.States.Add(new StateDefinition
                {
                    StateId = "cooling",
                    StateName = "COOLING",
                    Description = "기판 냉각 중",
                    IsInitialState = false
                });

                processStateMachine.States.Add(new StateDefinition
                {
                    StateId = "venting",
                    StateName = "VENTING",
                    Description = "챔버 벤팅 중",
                    IsInitialState = false
                });

                processStateMachine.States.Add(new StateDefinition
                {
                    StateId = "wafer_unloading",
                    StateName = "WAFER_UNLOADING",
                    Description = "웨이퍼 언로딩 중",
                    IsInitialState = false
                });

                processStateMachine.States.Add(new StateDefinition
                {
                    StateId = "error",
                    StateName = "ERROR",
                    Description = "공정 오류 상태",
                    IsInitialState = false
                });

                // 전이 정의
                processStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_idle_to_loading",
                    FromStateId = "idle",
                    ToStateId = "wafer_loading",
                    EventTrigger = "START_LOADING",
                    Description = "웨이퍼 로딩 시작"
                });

                processStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_loading_to_pumping",
                    FromStateId = "wafer_loading",
                    ToStateId = "pumping",
                    EventTrigger = "LOADING_COMPLETE",
                    Description = "로딩 완료, 펌핑 시작"
                });

                processStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_pumping_to_heating",
                    FromStateId = "pumping",
                    ToStateId = "heating",
                    EventTrigger = "BASE_PRESSURE_REACHED",
                    Description = "기본 압력 도달, 가열 시작"
                });

                processStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_heating_to_pre_sputtering",
                    FromStateId = "heating",
                    ToStateId = "pre_sputtering",
                    EventTrigger = "TEMPERATURE_REACHED",
                    Description = "목표 온도 도달, 프리스퍼터링 시작"
                });

                processStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_pre_sputtering_to_sputtering",
                    FromStateId = "pre_sputtering",
                    ToStateId = "sputtering",
                    EventTrigger = "PRE_SPUTTERING_COMPLETE",
                    Description = "프리스퍼터링 완료, 메인 스퍼터링 시작"
                });

                processStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_sputtering_to_cooling",
                    FromStateId = "sputtering",
                    ToStateId = "cooling",
                    EventTrigger = "SPUTTERING_COMPLETE",
                    Description = "스퍼터링 완료, 냉각 시작"
                });

                processStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_cooling_to_venting",
                    FromStateId = "cooling",
                    ToStateId = "venting",
                    EventTrigger = "COOLING_COMPLETE",
                    Description = "냉각 완료, 벤팅 시작"
                });

                processStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_venting_to_unloading",
                    FromStateId = "venting",
                    ToStateId = "wafer_unloading",
                    EventTrigger = "VENTING_COMPLETE",
                    Description = "벤팅 완료, 언로딩 시작"
                });

                processStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_unloading_to_idle",
                    FromStateId = "wafer_unloading",
                    ToStateId = "idle",
                    EventTrigger = "UNLOADING_COMPLETE",
                    Description = "언로딩 완료, 대기 상태로 복귀"
                });

                processStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_any_to_error",
                    FromStateId = "*",
                    ToStateId = "error",
                    EventTrigger = "PROCESS_ERROR",
                    Description = "공정 오류 발생"
                });

                processStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_error_to_idle",
                    FromStateId = "error",
                    ToStateId = "idle",
                    EventTrigger = "ERROR_RESET",
                    Description = "오류 리셋, 대기 상태로 복귀"
                });

                stateMachines.Add(processStateMachine);

                // 3. 로봇 상태 머신 (Vacuum Robot State Machine)
                var robotStateMachine = new StateMachineDefinition
                {
                    StateMachineId = "sm_vacuum_robot",
                    StateMachineName = "VacuumRobotStateMachine",
                    Description = "진공 로봇 상태 머신"
                };

                // 상태 정의
                robotStateMachine.States.Add(new StateDefinition
                {
                    StateId = "home",
                    StateName = "HOME",
                    Description = "로봇 홈 위치",
                    IsInitialState = true
                });

                robotStateMachine.States.Add(new StateDefinition
                {
                    StateId = "moving",
                    StateName = "MOVING",
                    Description = "이동 중",
                    IsInitialState = false
                });

                robotStateMachine.States.Add(new StateDefinition
                {
                    StateId = "picking",
                    StateName = "PICKING",
                    Description = "웨이퍼 픽업 중",
                    IsInitialState = false
                });

                robotStateMachine.States.Add(new StateDefinition
                {
                    StateId = "placing",
                    StateName = "PLACING",
                    Description = "웨이퍼 플레이스 중",
                    IsInitialState = false
                });

                robotStateMachine.States.Add(new StateDefinition
                {
                    StateId = "holding",
                    StateName = "HOLDING",
                    Description = "웨이퍼 홀딩 중",
                    IsInitialState = false
                });

                robotStateMachine.States.Add(new StateDefinition
                {
                    StateId = "error",
                    StateName = "ERROR",
                    Description = "로봇 오류 상태",
                    IsInitialState = false
                });

                // 전이 정의
                robotStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_home_to_moving",
                    FromStateId = "home",
                    ToStateId = "moving",
                    EventTrigger = "MOVE_TO_POSITION",
                    Description = "위치로 이동"
                });

                robotStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_moving_to_picking",
                    FromStateId = "moving",
                    ToStateId = "picking",
                    EventTrigger = "START_PICK",
                    Description = "픽업 시작"
                });

                robotStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_picking_to_holding",
                    FromStateId = "picking",
                    ToStateId = "holding",
                    EventTrigger = "PICK_COMPLETE",
                    Description = "픽업 완료, 웨이퍼 홀딩"
                });

                robotStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_holding_to_moving",
                    FromStateId = "holding",
                    ToStateId = "moving",
                    EventTrigger = "MOVE_WITH_WAFER",
                    Description = "웨이퍼를 들고 이동"
                });

                robotStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_moving_to_placing",
                    FromStateId = "moving",
                    ToStateId = "placing",
                    EventTrigger = "START_PLACE",
                    Description = "플레이스 시작"
                });

                robotStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_placing_to_moving",
                    FromStateId = "placing",
                    ToStateId = "moving",
                    EventTrigger = "PLACE_COMPLETE",
                    Description = "플레이스 완료, 빈 핸드로 이동"
                });

                robotStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_moving_to_home",
                    FromStateId = "moving",
                    ToStateId = "home",
                    EventTrigger = "RETURN_HOME",
                    Description = "홈 위치로 복귀"
                });

                robotStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_any_to_error",
                    FromStateId = "*",
                    ToStateId = "error",
                    EventTrigger = "ROBOT_ERROR",
                    Description = "로봇 오류 발생"
                });

                robotStateMachine.Transitions.Add(new TransitionDefinition
                {
                    TransitionId = "t_error_to_home",
                    FromStateId = "error",
                    ToStateId = "home",
                    EventTrigger = "ERROR_RESET",
                    Description = "오류 리셋, 홈 위치로 복귀"
                });

                stateMachines.Add(robotStateMachine);

                _logger.LogInformation($"장비 '{equipmentUid}' 상태 머신 메타데이터 조회 완료: {stateMachines.Count}개 상태 머신 정의");
                return stateMachines;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"상태 머신 메타데이터 조회 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 장비의 SEMI 표준 객체 타입 메타데이터 조회
        /// </summary>
        public IList<SEMIObjTypeDefinition> GetSEMIObjTypes(string equipmentUid)
        {
            try
            {
                _logger.LogInformation($"장비 '{equipmentUid}' SEMI 표준 객체 타입 메타데이터 조회 시작");

                // 장비에 따라 객체 타입 목록을 다르게 제공할 수 있음
                var equipment = GetMetadata(equipmentUid);
                if (equipment == null)
                {
                    _logger.LogWarning($"장비 '{equipmentUid}'를 찾을 수 없습니다.");
                    return new List<SEMIObjTypeDefinition>();
                }

                // 장비(PVD/스퍼터링)에 특화된 SEMI 표준 객체 타입 목록
                var objTypes = new List<SEMIObjTypeDefinition>();

                // 1. SEMI E10 - 장비 상태 관련 객체 타입
                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E10_EquipmentState",
                    ObjTypeName = "EquipmentState",
                    StandardReference = "SEMI E10",
                    Description = "장비 상태 객체 (NON_SCHEDULED, ENGINEERING, SCHEDULED_DOWN, UNSCHEDULED_DOWN, STANDBY, PRODUCTIVE)"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E10_NonScheduledTime",
                    ObjTypeName = "NonScheduledTime",
                    StandardReference = "SEMI E10",
                    Description = "비예약 시간 객체 (UNWORKED, FACILITIES_RELATED, SUPPLIER_EXCURSION)"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E10_EngineeringState",
                    ObjTypeName = "EngineeringState",
                    StandardReference = "SEMI E10",
                    Description = "엔지니어링 상태 객체 (PROCESS_EXPERIMENT, EQUIPMENT_EXPERIMENT, CHANGE_SETUP)"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E10_ScheduledDowntimeState",
                    ObjTypeName = "ScheduledDowntimeState",
                    StandardReference = "SEMI E10",
                    Description = "계획된 다운타임 상태 객체 (MAINTENANCE, SETUP_OR_CONFIGURATION, PREVENTIVE_MAINTENANCE)"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E10_UnscheduledDowntimeState",
                    ObjTypeName = "UnscheduledDowntimeState",
                    StandardReference = "SEMI E10",
                    Description = "계획되지 않은 다운타임 상태 객체 (REPAIR, FAILURE, WAITING_FOR_PARTS)"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E10_StandbyState",
                    ObjTypeName = "StandbyState",
                    StandardReference = "SEMI E10",
                    Description = "대기 상태 객체 (NO_PRODUCT, NO_OPERATOR, NO_SUPPORT_TOOL, ASSOCIATED_CLUSTER_EQUIPMENT_DOWN)"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E10_ProductiveState",
                    ObjTypeName = "ProductiveState",
                    StandardReference = "SEMI E10",
                    Description = "생산 상태 객체 (REGULAR_PRODUCTION, REWORK, ENGINEERING_PRODUCTION)"
                });

                // 2. SEMI E30 - GEM 관련 객체 타입
                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E30_SpoolingObj",
                    ObjTypeName = "SpoolingObj",
                    StandardReference = "SEMI E30",
                    Description = "GEM 메시지 스풀링 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E30_Clock",
                    ObjTypeName = "Clock",
                    StandardReference = "SEMI E30",
                    Description = "장비 클럭 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E30_AlarmObj",
                    ObjTypeName = "AlarmObj",
                    StandardReference = "SEMI E30",
                    Description = "알람 관리 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E30_EventObj",
                    ObjTypeName = "EventObj",
                    StandardReference = "SEMI E30",
                    Description = "이벤트 관리 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E30_DataVariable",
                    ObjTypeName = "DataVariable",
                    StandardReference = "SEMI E30",
                    Description = "데이터 변수 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E30_EquipmentConstant",
                    ObjTypeName = "EquipmentConstant",
                    StandardReference = "SEMI E30",
                    Description = "장비 상수 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E30_StatusVariable",
                    ObjTypeName = "StatusVariable",
                    StandardReference = "SEMI E30",
                    Description = "상태 변수 객체"
                });

                // 3. SEMI E87 - 캐리어 관리 관련 객체 타입
                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E87_CarrierObj",
                    ObjTypeName = "CarrierObj",
                    StandardReference = "SEMI E87",
                    Description = "캐리어(FOUP) 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E87_LoadPortObj",
                    ObjTypeName = "LoadPortObj",
                    StandardReference = "SEMI E87",
                    Description = "로드 포트 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E87_AccessModeObj",
                    ObjTypeName = "AccessModeObj",
                    StandardReference = "SEMI E87",
                    Description = "캐리어 접근 모드 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E87_CassetteMapObj",
                    ObjTypeName = "CassetteMapObj",
                    StandardReference = "SEMI E87",
                    Description = "카세트 슬롯 맵 객체"
                });

                // 4. SEMI E90 - 기판 추적 관련 객체 타입
                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E90_SubstrateObj",
                    ObjTypeName = "SubstrateObj",
                    StandardReference = "SEMI E90",
                    Description = "기판(웨이퍼) 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E90_SubstrateLocationObj",
                    ObjTypeName = "SubstrateLocationObj",
                    StandardReference = "SEMI E90",
                    Description = "기판 위치 객체"
                });

                // 5. SEMI E94 - 제어 작업 관련 객체 타입
                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E94_ControlJobObj",
                    ObjTypeName = "ControlJobObj",
                    StandardReference = "SEMI E94",
                    Description = "제어 작업 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E94_ProcessJobObj",
                    ObjTypeName = "ProcessJobObj",
                    StandardReference = "SEMI E94",
                    Description = "공정 작업 객체"
                });

                // 6. SEMI E116 - 웨이퍼 공정 데이터 관련 객체 타입
                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E116_WaferProcessObj",
                    ObjTypeName = "WaferProcessObj",
                    StandardReference = "SEMI E116",
                    Description = "웨이퍼 공정 데이터 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E116_ProcessParameterObj",
                    ObjTypeName = "ProcessParameterObj",
                    StandardReference = "SEMI E116",
                    Description = "공정 매개변수 객체"
                });

                // 7. SEMI E157 - 모듈 공정 작업 관련 객체 타입
                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E157_ModuleProcessJobObj",
                    ObjTypeName = "ModuleProcessJobObj",
                    StandardReference = "SEMI E157",
                    Description = "모듈 공정 작업 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "E157_SubstrateRoutingObj",
                    ObjTypeName = "SubstrateRoutingObj",
                    StandardReference = "SEMI E157",
                    Description = "기판 라우팅 객체"
                });

                // 8. 스퍼터링 장비 특화 객체 타입
                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "SPUTTER_TargetObj",
                    ObjTypeName = "TargetObj",
                    StandardReference = "Vendor Specific",
                    Description = "스퍼터링 타겟 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "SPUTTER_DepositionRecipeObj",
                    ObjTypeName = "DepositionRecipeObj",
                    StandardReference = "Vendor Specific",
                    Description = "증착 레시피 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "SPUTTER_ChamberObj",
                    ObjTypeName = "ChamberObj",
                    StandardReference = "Vendor Specific",
                    Description = "챔버 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "SPUTTER_GasSystemObj",
                    ObjTypeName = "GasSystemObj",
                    StandardReference = "Vendor Specific",
                    Description = "가스 시스템 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "SPUTTER_VacuumSystemObj",
                    ObjTypeName = "VacuumSystemObj",
                    StandardReference = "Vendor Specific",
                    Description = "진공 시스템 객체"
                });

                objTypes.Add(new SEMIObjTypeDefinition
                {
                    ObjTypeId = "SPUTTER_PowerSupplyObj",
                    ObjTypeName = "PowerSupplyObj",
                    StandardReference = "Vendor Specific",
                    Description = "전원 공급 장치 객체"
                });

                _logger.LogInformation($"장비 '{equipmentUid}' SEMI 표준 객체 타입 메타데이터 조회 완료: {objTypes.Count}개 객체 타입 정의");
                return objTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SEMI 표준 객체 타입 메타데이터 조회 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 장비의 예외 메타데이터 조회
        /// </summary>
        public IList<ExceptionDefinition> GetExceptions(string equipmentUid)
        {
            try
            {
                _logger.LogInformation($"장비 '{equipmentUid}' 예외 메타데이터 조회 시작");

                // 장비에 따라 예외 목록을 다르게 제공할 수 있음
                var equipment = GetMetadata(equipmentUid);
                if (equipment == null)
                {
                    _logger.LogWarning($"장비 '{equipmentUid}'를 찾을 수 없습니다.");
                    return new List<ExceptionDefinition>();
                }

                // 스퍼터링 장비에 특화된 예외 목록
                var exceptions = new List<ExceptionDefinition>();

                // 1. 시스템 오류
                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "SYS001",
                    ExceptionName = "SystemInitializationError",
                    ExceptionCode = "E001",
                    Severity = "ERROR",
                    Category = "SYSTEM",
                    Description = "시스템 초기화 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "SYS002",
                    ExceptionName = "CommunicationError",
                    ExceptionCode = "E002",
                    Severity = "ERROR",
                    Category = "SYSTEM",
                    Description = "내부 통신 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "SYS003",
                    ExceptionName = "ConfigurationError",
                    ExceptionCode = "E003",
                    Severity = "ERROR",
                    Category = "SYSTEM",
                    Description = "시스템 설정 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "SYS004",
                    ExceptionName = "SoftwareError",
                    ExceptionCode = "E004",
                    Severity = "ERROR",
                    Category = "SYSTEM",
                    Description = "소프트웨어 오류"
                });

                // 2. 진공 시스템 오류
                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "VAC001",
                    ExceptionName = "PumpFailure",
                    ExceptionCode = "E101",
                    Severity = "CRITICAL",
                    Category = "VACUUM",
                    Description = "진공 펌프 고장"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "VAC002",
                    ExceptionName = "BasePressureTimeout",
                    ExceptionCode = "E102",
                    Severity = "ERROR",
                    Category = "VACUUM",
                    Description = "기본 압력 도달 타임아웃"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "VAC003",
                    ExceptionName = "PressureOutOfRange",
                    ExceptionCode = "E103",
                    Severity = "WARNING",
                    Category = "VACUUM",
                    Description = "챔버 압력이 범위를 벗어남"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "VAC004",
                    ExceptionName = "LeakDetected",
                    ExceptionCode = "E104",
                    Severity = "ERROR",
                    Category = "VACUUM",
                    Description = "진공 누설 감지됨"
                });

                // 3. 전원 공급 장치 오류
                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "PWR001",
                    ExceptionName = "DCPowerError",
                    ExceptionCode = "E201",
                    Severity = "ERROR",
                    Category = "POWER",
                    Description = "DC 전원 공급 장치 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "PWR002",
                    ExceptionName = "RFPowerError",
                    ExceptionCode = "E202",
                    Severity = "ERROR",
                    Category = "POWER",
                    Description = "RF 전원 공급 장치 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "PWR003",
                    ExceptionName = "OverCurrent",
                    ExceptionCode = "E203",
                    Severity = "CRITICAL",
                    Category = "POWER",
                    Description = "과전류 감지됨"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "PWR004",
                    ExceptionName = "PowerOutOfRange",
                    ExceptionCode = "E204",
                    Severity = "WARNING",
                    Category = "POWER",
                    Description = "전력 값이 범위를 벗어남"
                });

                // 4. 프로세스 오류
                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "PRC001",
                    ExceptionName = "RecipeParseError",
                    ExceptionCode = "E301",
                    Severity = "ERROR",
                    Category = "PROCESS",
                    Description = "레시피 파싱 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "PRC002",
                    ExceptionName = "ProcessTimeout",
                    ExceptionCode = "E302",
                    Severity = "ERROR",
                    Category = "PROCESS",
                    Description = "공정 타임아웃"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "PRC003",
                    ExceptionName = "UniformityError",
                    ExceptionCode = "E303",
                    Severity = "WARNING",
                    Category = "PROCESS",
                    Description = "증착 균일성 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "PRC004",
                    ExceptionName = "DepositionRateError",
                    ExceptionCode = "E304",
                    Severity = "WARNING",
                    Category = "PROCESS",
                    Description = "증착 속도 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "PRC005",
                    ExceptionName = "TemperatureOutOfRange",
                    ExceptionCode = "E305",
                    Severity = "WARNING",
                    Category = "PROCESS",
                    Description = "온도가 허용 범위를 벗어남"
                });

                // 5. 로봇 및 웨이퍼 처리 오류
                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "RBT001",
                    ExceptionName = "RobotMovementError",
                    ExceptionCode = "E401",
                    Severity = "ERROR",
                    Category = "ROBOT",
                    Description = "로봇 이동 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "RBT002",
                    ExceptionName = "WaferMappingError",
                    ExceptionCode = "E402",
                    Severity = "ERROR",
                    Category = "ROBOT",
                    Description = "웨이퍼 매핑 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "RBT003",
                    ExceptionName = "WaferAlignmentError",
                    ExceptionCode = "E403",
                    Severity = "ERROR",
                    Category = "ROBOT",
                    Description = "웨이퍼 정렬 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "RBT004",
                    ExceptionName = "WaferBreakage",
                    ExceptionCode = "E404",
                    Severity = "CRITICAL",
                    Category = "ROBOT",
                    Description = "웨이퍼 파손 감지됨"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "RBT005",
                    ExceptionName = "LoadPortError",
                    ExceptionCode = "E405",
                    Severity = "ERROR",
                    Category = "ROBOT",
                    Description = "로드 포트 오류"
                });

                // 6. 가스 시스템 오류
                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "GAS001",
                    ExceptionName = "MFCError",
                    ExceptionCode = "E501",
                    Severity = "ERROR",
                    Category = "GAS",
                    Description = "질량 유량 컨트롤러 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "GAS002",
                    ExceptionName = "GasFlowOutOfRange",
                    ExceptionCode = "E502",
                    Severity = "WARNING",
                    Category = "GAS",
                    Description = "가스 유량이 범위를 벗어남"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "GAS003",
                    ExceptionName = "GasPressureError",
                    ExceptionCode = "E503",
                    Severity = "ERROR",
                    Category = "GAS",
                    Description = "가스 공급 압력 오류"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "GAS004",
                    ExceptionName = "GasLeakWarning",
                    ExceptionCode = "E504",
                    Severity = "CRITICAL",
                    Category = "GAS",
                    Description = "가스 누출 경고"
                });

                // 7. 안전 관련 오류
                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "SFT001",
                    ExceptionName = "EmergencyStop",
                    ExceptionCode = "E601",
                    Severity = "CRITICAL",
                    Category = "SAFETY",
                    Description = "비상 정지 버튼 활성화됨"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "SFT002",
                    ExceptionName = "DoorOpenWhileProcessing",
                    ExceptionCode = "E602",
                    Severity = "CRITICAL",
                    Category = "SAFETY",
                    Description = "공정 중 도어 열림 감지됨"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "SFT003",
                    ExceptionName = "OverTemperature",
                    ExceptionCode = "E603",
                    Severity = "CRITICAL",
                    Category = "SAFETY",
                    Description = "과열 감지됨"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "SFT004",
                    ExceptionName = "HardwareInterlockTriggered",
                    ExceptionCode = "E604",
                    Severity = "CRITICAL",
                    Category = "SAFETY",
                    Description = "하드웨어 인터락 트리거됨"
                });

                // 8. 알림 및 경고
                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "NTF001",
                    ExceptionName = "TargetLifeWarning",
                    ExceptionCode = "W101",
                    Severity = "WARNING",
                    Category = "MAINTENANCE",
                    Description = "타겟 수명 경고 - 곧 교체 필요"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "NTF002",
                    ExceptionName = "MaintenanceDue",
                    ExceptionCode = "W102",
                    Severity = "INFO",
                    Category = "MAINTENANCE",
                    Description = "정기 유지보수 시기"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "NTF003",
                    ExceptionName = "ProcessCompleted",
                    ExceptionCode = "I101",
                    Severity = "INFO",
                    Category = "PROCESS",
                    Description = "공정 완료됨"
                });

                exceptions.Add(new ExceptionDefinition
                {
                    ExceptionId = "NTF004",
                    ExceptionName = "RecipeChanged",
                    ExceptionCode = "I102",
                    Severity = "INFO",
                    Category = "PROCESS",
                    Description = "레시피 변경됨"
                });

                _logger.LogInformation($"장비 '{equipmentUid}' 예외 메타데이터 조회 완료: {exceptions.Count}개 예외 정의");
                return exceptions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"예외 메타데이터 조회 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 장비의 매개변수 메타데이터 조회
        /// </summary>
        public IList<ParameterDefinition> GetParameters(string equipmentUid, string nodeId = null)
        {
            try
            {
                _logger.LogInformation($"장비 '{equipmentUid}' 매개변수 메타데이터 조회 시작");

                // 장비에 따라 매개변수 목록을 다르게 제공할 수 있음
                var equipment = GetMetadata(equipmentUid);
                if (equipment == null)
                {
                    _logger.LogWarning($"장비 '{equipmentUid}'를 찾을 수 없습니다.");
                    return new List<ParameterDefinition>();
                }

                // 모든 매개변수를 담을 리스트
                var parameters = new List<ParameterDefinition>();

                // 1. 공정 매개변수 - 스퍼터링 챔버(PMC6, PMC7)
                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "P001",
                    ParameterName = "BasePressure",
                    DataType = "double",
                    DefaultValue = "5.0E-8",
                    MinValue = "1.0E-9",
                    MaxValue = "1.0E-6",
                    UnitId = "pressure_torr",
                    Category = "PROCESS",
                    NodeId = "PMC6",
                    Description = "스퍼터링 전 요구되는 기본 압력",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "P002",
                    ParameterName = "ProcessPressure",
                    DataType = "double",
                    DefaultValue = "5.0E-3",
                    MinValue = "1.0E-4",
                    MaxValue = "1.0E-2",
                    UnitId = "pressure_torr",
                    Category = "PROCESS",
                    NodeId = "PMC6",
                    Description = "스퍼터링 중 유지되어야 하는 공정 압력",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "P003",
                    ParameterName = "DCPower",
                    DataType = "double",
                    DefaultValue = "2000",
                    MinValue = "500",
                    MaxValue = "5000",
                    UnitId = "power_w",
                    Category = "PROCESS",
                    NodeId = "PMC6",
                    Description = "스퍼터링 타겟에 적용되는 DC 전력",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "P004",
                    ParameterName = "SubstrateTemperature",
                    DataType = "double",
                    DefaultValue = "250",
                    MinValue = "20",
                    MaxValue = "500",
                    UnitId = "temp_c",
                    Category = "PROCESS",
                    NodeId = "PMC6",
                    Description = "기판 온도",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "P005",
                    ParameterName = "ProcessTime",
                    DataType = "double",
                    DefaultValue = "60",
                    MinValue = "1",
                    MaxValue = "3600",
                    UnitId = "time_s",
                    Category = "PROCESS",
                    NodeId = "PMC6",
                    Description = "스퍼터링 공정 시간",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "P006",
                    ParameterName = "PreSputtingTime",
                    DataType = "double",
                    DefaultValue = "30",
                    MinValue = "0",
                    MaxValue = "300",
                    UnitId = "time_s",
                    Category = "PROCESS",
                    NodeId = "PMC6",
                    Description = "타겟 표면 세정을 위한 프리스퍼터링 시간",
                    ReadOnly = false
                });

                // 2. 가스 매개변수
                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "G001",
                    ParameterName = "ArFlowRate",
                    DataType = "double",
                    DefaultValue = "50",
                    MinValue = "10",
                    MaxValue = "200",
                    UnitId = "flow_sccm",
                    Category = "GAS",
                    NodeId = "PMC6",
                    Description = "아르곤 가스 유량",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "G002",
                    ParameterName = "N2FlowRate",
                    DataType = "double",
                    DefaultValue = "0",
                    MinValue = "0",
                    MaxValue = "100",
                    UnitId = "flow_sccm",
                    Category = "GAS",
                    NodeId = "PMC6",
                    Description = "질소 가스 유량",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "G003",
                    ParameterName = "O2FlowRate",
                    DataType = "double",
                    DefaultValue = "0",
                    MinValue = "0",
                    MaxValue = "50",
                    UnitId = "flow_sccm",
                    Category = "GAS",
                    NodeId = "PMC6",
                    Description = "산소 가스 유량",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "G004",
                    ParameterName = "ArPressure",
                    DataType = "double",
                    DefaultValue = "30",
                    MinValue = "20",
                    MaxValue = "50",
                    UnitId = "pressure_psi",
                    Category = "GAS",
                    NodeId = "GasSystem",
                    Description = "아르곤 가스 공급 압력",
                    ReadOnly = true
                });

                // 3. 챔버 설정
                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "C001",
                    ParameterName = "TargetSubstrateDistance",
                    DataType = "double",
                    DefaultValue = "100",
                    MinValue = "50",
                    MaxValue = "150",
                    UnitId = "length_mm",
                    Category = "CHAMBER",
                    NodeId = "PMC6",
                    Description = "타겟과 기판 사이의 거리",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "C002",
                    ParameterName = "ChuckerType",
                    DataType = "enum",
                    DefaultValue = "ELECTROSTATIC",
                    MinValue = "",
                    MaxValue = "",
                    UnitId = "",
                    Category = "CHAMBER",
                    NodeId = "PMC6",
                    Description = "웨이퍼 척 유형 (ELECTROSTATIC, MECHANICAL, VACUUM)",
                    ReadOnly = true,
                    Attributes = new Dictionary<string, string>
            {
                { "EnumValues", "ELECTROSTATIC,MECHANICAL,VACUUM" }
            }
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "C003",
                    ParameterName = "ChuckerVoltage",
                    DataType = "double",
                    DefaultValue = "400",
                    MinValue = "0",
                    MaxValue = "1000",
                    UnitId = "voltage_v",
                    Category = "CHAMBER",
                    NodeId = "PMC6",
                    Description = "정전기 척 전압",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "C004",
                    ParameterName = "HeBacksidePressure",
                    DataType = "double",
                    DefaultValue = "5",
                    MinValue = "0",
                    MaxValue = "20",
                    UnitId = "pressure_torr",
                    Category = "CHAMBER",
                    NodeId = "PMC6",
                    Description = "웨이퍼 뒷면 헬륨 압력",
                    ReadOnly = false
                });

                // 4. 로봇 설정
                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "R001",
                    ParameterName = "RobotSpeed",
                    DataType = "double",
                    DefaultValue = "50",
                    MinValue = "10",
                    MaxValue = "100",
                    UnitId = "percentage",
                    Category = "ROBOT",
                    NodeId = "TRM-Robot-01",
                    Description = "로봇 이동 속도 (최대 속도의 %)",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "R002",
                    ParameterName = "WaferHandlingMode",
                    DataType = "enum",
                    DefaultValue = "NORMAL",
                    MinValue = "",
                    MaxValue = "",
                    UnitId = "",
                    Category = "ROBOT",
                    NodeId = "TRM-Robot-01",
                    Description = "웨이퍼 처리 모드 (NORMAL, GENTLE, FAST)",
                    ReadOnly = false,
                    Attributes = new Dictionary<string, string>
            {
                { "EnumValues", "NORMAL,GENTLE,FAST" }
            }
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "R003",
                    ParameterName = "RobotHomePosition",
                    DataType = "string",
                    DefaultValue = "HOME1",
                    MinValue = "",
                    MaxValue = "",
                    UnitId = "",
                    Category = "ROBOT",
                    NodeId = "TRM-Robot-01",
                    Description = "로봇 홈 위치",
                    ReadOnly = true
                });

                // 5. 알람 임계값
                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "A001",
                    ParameterName = "PressureAlarmThreshold",
                    DataType = "double",
                    DefaultValue = "1.0E-5",
                    MinValue = "1.0E-7",
                    MaxValue = "1.0E-3",
                    UnitId = "pressure_torr",
                    Category = "ALARM",
                    NodeId = "PMC6",
                    Description = "압력 경고 임계값",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "A002",
                    ParameterName = "TemperatureAlarmThreshold",
                    DataType = "double",
                    DefaultValue = "400",
                    MinValue = "100",
                    MaxValue = "600",
                    UnitId = "temp_c",
                    Category = "ALARM",
                    NodeId = "PMC6",
                    Description = "온도 경고 임계값",
                    ReadOnly = false
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "A003",
                    ParameterName = "PowerAlarmThreshold",
                    DataType = "double",
                    DefaultValue = "4500",
                    MinValue = "1000",
                    MaxValue = "6000",
                    UnitId = "power_w",
                    Category = "ALARM",
                    NodeId = "PMC6",
                    Description = "전력 경고 임계값",
                    ReadOnly = false
                });

                // 6. 타겟 관리 설정
                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "T001",
                    ParameterName = "TargetMaterial",
                    DataType = "enum",
                    DefaultValue = "Ti",
                    MinValue = "",
                    MaxValue = "",
                    UnitId = "",
                    Category = "TARGET",
                    NodeId = "PMC6-Target-001",
                    Description = "타겟 재료 (Ti, Cu, Al, Ta, W, etc.)",
                    ReadOnly = true,
                    Attributes = new Dictionary<string, string>
            {
                { "EnumValues", "Ti,Cu,Al,Ta,W" }
            }
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "T002",
                    ParameterName = "TargetPurity",
                    DataType = "double",
                    DefaultValue = "99.99",
                    MinValue = "99.9",
                    MaxValue = "99.9999",
                    UnitId = "percentage",
                    Category = "TARGET",
                    NodeId = "PMC6-Target-001",
                    Description = "타겟 순도 (%)",
                    ReadOnly = true
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "T003",
                    ParameterName = "TargetLife",
                    DataType = "double",
                    DefaultValue = "100",
                    MinValue = "0",
                    MaxValue = "100",
                    UnitId = "percentage",
                    Category = "TARGET",
                    NodeId = "PMC6-Target-001",
                    Description = "타겟 수명 (%)",
                    ReadOnly = true
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "T004",
                    ParameterName = "TargetLifeThreshold",
                    DataType = "double",
                    DefaultValue = "10",
                    MinValue = "5",
                    MaxValue = "50",
                    UnitId = "percentage",
                    Category = "TARGET",
                    NodeId = "PMC6-Target-001",
                    Description = "타겟 수명 경고 임계값 (%)",
                    ReadOnly = false
                });

                // 7. 시스템 설정
                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "S001",
                    ParameterName = "SystemMode",
                    DataType = "enum",
                    DefaultValue = "PRODUCTION",
                    MinValue = "",
                    MaxValue = "",
                    UnitId = "",
                    Category = "SYSTEM",
                    NodeId = "WLP",
                    Description = "시스템 작동 모드 (PRODUCTION, ENGINEERING, MAINTENANCE)",
                    ReadOnly = false,
                    Attributes = new Dictionary<string, string>
            {
                { "EnumValues", "PRODUCTION,ENGINEERING,MAINTENANCE" }
            }
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "S002",
                    ParameterName = "LogLevel",
                    DataType = "enum",
                    DefaultValue = "INFO",
                    MinValue = "",
                    MaxValue = "",
                    UnitId = "",
                    Category = "SYSTEM",
                    NodeId = "WLP",
                    Description = "시스템 로그 레벨 (DEBUG, INFO, WARNING, ERROR)",
                    ReadOnly = false,
                    Attributes = new Dictionary<string, string>
            {
                { "EnumValues", "DEBUG,INFO,WARNING,ERROR" }
            }
                });

                parameters.Add(new ParameterDefinition
                {
                    ParameterId = "S003",
                    ParameterName = "AutoRecoveryEnabled",
                    DataType = "boolean",
                    DefaultValue = "true",
                    MinValue = "",
                    MaxValue = "",
                    UnitId = "",
                    Category = "SYSTEM",
                    NodeId = "WLP",
                    Description = "오류 자동 복구 활성화 여부",
                    ReadOnly = false
                });

                // 특정 노드 ID에 대한 필터링이 요청된 경우
                if (!string.IsNullOrEmpty(nodeId))
                {
                    parameters = parameters.Where(p => p.NodeId == nodeId).ToList();
                }

                _logger.LogInformation($"장비 '{equipmentUid}' 매개변수 메타데이터 조회 완료: {parameters.Count}개 매개변수 정의");
                return parameters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"매개변수 메타데이터 조회 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 장비의 간단한 이벤트 메타데이터 조회
        /// </summary>
        public IList<SimpleEventDefinition> GetSimpleEvents(string equipmentUid, string nodeId = null)
        {
            try
            {
                _logger.LogInformation($"장비 '{equipmentUid}' 간단한 이벤트 메타데이터 조회 시작");

                // 장비에 따라 이벤트 목록을 다르게 제공할 수 있음
                var equipment = GetMetadata(equipmentUid);
                if (equipment == null)
                {
                    _logger.LogWarning($"장비 '{equipmentUid}'를 찾을 수 없습니다.");
                    return new List<SimpleEventDefinition>();
                }

                // 스퍼터링 장비에 특화된 간단한 이벤트 목록
                var events = new List<SimpleEventDefinition>();

                // 1. 프로세스 이벤트
                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT001",
                    EventName = "ProcessStarted",
                    EventType = "PROCESS",
                    Description = "공정이 시작됨",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT002",
                    EventName = "ProcessCompleted",
                    EventType = "PROCESS",
                    Description = "공정이 정상적으로 완료됨",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT003",
                    EventName = "ProcessAborted",
                    EventType = "PROCESS",
                    Description = "공정이 중단됨",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT004",
                    EventName = "BasePressureReached",
                    EventType = "PROCESS",
                    Description = "기본 압력에 도달함",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT005",
                    EventName = "ProcessTemperatureReached",
                    EventType = "PROCESS",
                    Description = "공정 온도에 도달함",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT006",
                    EventName = "DepositionStarted",
                    EventType = "PROCESS",
                    Description = "증착 시작됨",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT007",
                    EventName = "DepositionCompleted",
                    EventType = "PROCESS",
                    Description = "증착 완료됨",
                    NodeId = "PMC6"
                });

                // 2. 웨이퍼 처리 이벤트
                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT101",
                    EventName = "WaferLoaded",
                    EventType = "WAFER",
                    Description = "웨이퍼가 로드됨",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT102",
                    EventName = "WaferUnloaded",
                    EventType = "WAFER",
                    Description = "웨이퍼가 언로드됨",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT103",
                    EventName = "WaferMapped",
                    EventType = "WAFER",
                    Description = "웨이퍼 매핑 완료",
                    NodeId = "EFEM"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT104",
                    EventName = "WaferAligned",
                    EventType = "WAFER",
                    Description = "웨이퍼 정렬 완료",
                    NodeId = "EFEM-Aligner-01"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT105",
                    EventName = "WaferProcessed",
                    EventType = "WAFER",
                    Description = "웨이퍼 가공 완료",
                    NodeId = "PMC6"
                });

                // 3. 로봇 이벤트
                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT201",
                    EventName = "RobotMovementStarted",
                    EventType = "ROBOT",
                    Description = "로봇 이동 시작",
                    NodeId = "TRM-Robot-01"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT202",
                    EventName = "RobotMovementCompleted",
                    EventType = "ROBOT",
                    Description = "로봇 이동 완료",
                    NodeId = "TRM-Robot-01"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT203",
                    EventName = "RobotPickStarted",
                    EventType = "ROBOT",
                    Description = "로봇 집기 시작",
                    NodeId = "TRM-Robot-01"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT204",
                    EventName = "RobotPickCompleted",
                    EventType = "ROBOT",
                    Description = "로봇 집기 완료",
                    NodeId = "TRM-Robot-01"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT205",
                    EventName = "RobotPlaceStarted",
                    EventType = "ROBOT",
                    Description = "로봇 놓기 시작",
                    NodeId = "TRM-Robot-01"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT206",
                    EventName = "RobotPlaceCompleted",
                    EventType = "ROBOT",
                    Description = "로봇 놓기 완료",
                    NodeId = "TRM-Robot-01"
                });

                // 4. 시스템 이벤트
                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT301",
                    EventName = "SystemStartup",
                    EventType = "SYSTEM",
                    Description = "시스템 시작",
                    NodeId = "WLP"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT302",
                    EventName = "SystemShutdown",
                    EventType = "SYSTEM",
                    Description = "시스템 종료",
                    NodeId = "WLP"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT303",
                    EventName = "RecipeChanged",
                    EventType = "SYSTEM",
                    Description = "레시피 변경됨",
                    NodeId = "WLP"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT304",
                    EventName = "ConfigurationChanged",
                    EventType = "SYSTEM",
                    Description = "설정 변경됨",
                    NodeId = "WLP"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT305",
                    EventName = "MaintenanceRequired",
                    EventType = "SYSTEM",
                    Description = "유지보수 필요",
                    NodeId = "WLP"
                });

                // 5. 알람 이벤트
                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT401",
                    EventName = "PressureOutOfRange",
                    EventType = "ALARM",
                    Description = "압력이 범위를 벗어남",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT402",
                    EventName = "TemperatureOutOfRange",
                    EventType = "ALARM",
                    Description = "온도가 범위를 벗어남",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT403",
                    EventName = "PowerOutOfRange",
                    EventType = "ALARM",
                    Description = "전력이 범위를 벗어남",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT404",
                    EventName = "GasFlowOutOfRange",
                    EventType = "ALARM",
                    Description = "가스 유량이 범위를 벗어남",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT405",
                    EventName = "VacuumLeak",
                    EventType = "ALARM",
                    Description = "진공 누설 감지됨",
                    NodeId = "PMC6"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT406",
                    EventName = "TargetLifeLow",
                    EventType = "ALARM",
                    Description = "타겟 수명이 낮음",
                    NodeId = "PMC6-Target-001"
                });

                // 6. 도어 및 로드 포트 이벤트
                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT501",
                    EventName = "DoorOpened",
                    EventType = "DOOR",
                    Description = "도어 열림",
                    NodeId = "EFEM"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT502",
                    EventName = "DoorClosed",
                    EventType = "DOOR",
                    Description = "도어 닫힘",
                    NodeId = "EFEM"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT503",
                    EventName = "LoadPortLoaded",
                    EventType = "LOADPORT",
                    Description = "로드 포트에 FOUP 로드됨",
                    NodeId = "EFEM-LP-001"
                });

                events.Add(new SimpleEventDefinition
                {
                    EventId = "EVT504",
                    EventName = "LoadPortUnloaded",
                    EventType = "LOADPORT",
                    Description = "로드 포트에서 FOUP 언로드됨",
                    NodeId = "EFEM-LP-001"
                });

                // 특정 노드 ID에 대한 필터링이 요청된 경우
                if (!string.IsNullOrEmpty(nodeId))
                {
                    events = events.Where(e => e.NodeId == nodeId).ToList();
                }

                _logger.LogInformation($"장비 '{equipmentUid}' 간단한 이벤트 메타데이터 조회 완료: {events.Count}개 이벤트 정의");
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"간단한 이벤트 메타데이터 조회 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 장비의 전체 구조 메타데이터 조회
        /// </summary>
        public EquipmentStructure GetEquipmentStructure(string equipmentUid)
        {
            try
            {
                _logger.LogInformation($"장비 '{equipmentUid}' 전체 구조 메타데이터 조회 시작");

                // 장비에 따라 구조를 다르게 제공할 수 있음
                var equipment = GetMetadata(equipmentUid);
                if (equipment == null)
                {
                    _logger.LogWarning($"장비 '{equipmentUid}'를 찾을 수 없습니다.");
                    return null;
                }

                // equipment_model.json 기반 구조 생성
                var structure = new EquipmentStructure
                {
                    EquipmentUid = equipmentUid,
                    EquipmentName = equipment.EquipmentName,
                    EquipmentType = "Process", // 장비 유형 (Process, Metrology, Transport 등)
                    Description = "300mm Semiconductor Wafer-Level Packaging System" // equipment.Attributes에서 가져올 수도 있음
                };

                // 장비 속성 설정
                if (equipment.Attributes.TryGetValue("Supplier", out var supplier))
                    structure.Attributes.Add("Supplier", supplier);

                if (equipment.Attributes.TryGetValue("Model", out var model))
                    structure.Attributes.Add("Model", model);

                if (equipment.Attributes.TryGetValue("ElementType", out var elementType))
                    structure.Attributes.Add("ElementType", elementType);

                if (equipment.Attributes.TryGetValue("ProcessType", out var processType))
                    structure.Attributes.Add("ProcessType", processType);

                // 모듈, 서브시스템, IO 장치 노드 구성
                // 이 부분은 실제 equipment_model.json 데이터를 기반으로 구성

                // 1. 모듈 추가
                // Load/Unload Module 1 (LUM1)
                var lum1 = new EquipmentNode
                {
                    NodeId = "LUM1",
                    NodeName = "LUM1",
                    NodeType = "Transport",
                    Description = "Load/Unload Module 1"
                };
                lum1.Attributes.Add("ProcessName", "Load/Unload");
                lum1.Attributes.Add("ProcessType", "Transport");

                // LUM1에 IO 장치 추가
                var lum1Pump = new EquipmentNode
                {
                    NodeId = "LUM1-TMP-01",
                    NodeName = "TurboMolecularPump",
                    NodeType = "Pump",
                    Description = "LUM1 Turbo Molecular Pump"
                };
                lum1.Children.Add(lum1Pump);

                // LUM1에 머티리얼 위치 추가
                var lum1WaferSlot = new EquipmentNode
                {
                    NodeId = "LUM1-ML-001",
                    NodeName = "WaferSlot",
                    NodeType = "MaterialLocation",
                    Description = "Wafer Loading Position"
                };
                lum1WaferSlot.Attributes.Add("MaterialType", "Substrate");
                lum1WaferSlot.Attributes.Add("MaterialSubType", "300mm Wafer");
                lum1.Children.Add(lum1WaferSlot);

                structure.Modules.Add(lum1);

                // Load/Unload Module 2 (LUM2)
                var lum2 = new EquipmentNode
                {
                    NodeId = "LUM2",
                    NodeName = "LUM2",
                    NodeType = "Transport",
                    Description = "Load/Unload Module 2"
                };
                lum2.Attributes.Add("ProcessName", "Load/Unload");
                lum2.Attributes.Add("ProcessType", "Transport");

                // LUM2에 IO 장치 추가
                var lum2Pump = new EquipmentNode
                {
                    NodeId = "LUM2-TMP-01",
                    NodeName = "TurboMolecularPump",
                    NodeType = "Pump",
                    Description = "LUM2 Turbo Molecular Pump"
                };
                lum2.Children.Add(lum2Pump);

                // LUM2에 머티리얼 위치 추가
                var lum2WaferSlot = new EquipmentNode
                {
                    NodeId = "LUM2-ML-001",
                    NodeName = "WaferSlot",
                    NodeType = "MaterialLocation",
                    Description = "Wafer Loading Position"
                };
                lum2WaferSlot.Attributes.Add("MaterialType", "Substrate");
                lum2WaferSlot.Attributes.Add("MaterialSubType", "300mm Wafer");
                lum2.Children.Add(lum2WaferSlot);

                structure.Modules.Add(lum2);

                // Degas Chamber (PMC3)
                var pmc3 = new EquipmentNode
                {
                    NodeId = "PMC3",
                    NodeName = "Degas",
                    NodeType = "Process Area",
                    Description = "Degas Chamber with 8-stage Cassette"
                };
                pmc3.Attributes.Add("ProcessName", "Degas");
                pmc3.Attributes.Add("ProcessType", "Process");

                // PMC3에 IO 장치 추가
                var pmc3PressureSensor = new EquipmentNode
                {
                    NodeId = "PMC3-PressureSensor-01",
                    NodeName = "PressureSensor",
                    NodeType = "Sensor",
                    Description = "Chamber Pressure Sensor"
                };
                pmc3.Children.Add(pmc3PressureSensor);

                var pmc3TempController = new EquipmentNode
                {
                    NodeId = "PMC3-TempController-01",
                    NodeName = "TemperatureController",
                    NodeType = "Controller",
                    Description = "Temperature Controller for 8-stage Cassette"
                };
                pmc3.Children.Add(pmc3TempController);

                // PMC3에 머티리얼 위치 추가
                var pmc3WaferSlot = new EquipmentNode
                {
                    NodeId = "PMC3-WaferSlot-001",
                    NodeName = "WaferSlot",
                    NodeType = "MaterialLocation",
                    Description = "Wafer Processing Position (8-stage Cassette)"
                };
                pmc3WaferSlot.Attributes.Add("MaterialType", "Substrate");
                pmc3WaferSlot.Attributes.Add("MaterialSubType", "300mm Wafer");
                pmc3.Children.Add(pmc3WaferSlot);

                structure.Modules.Add(pmc3);

                // Pre-clean Chamber (PMC4)
                var pmc4 = new EquipmentNode
                {
                    NodeId = "PMC4",
                    NodeName = "Pre-clean",
                    NodeType = "Process Area",
                    Description = "Pre-clean Chamber"
                };
                pmc4.Attributes.Add("ProcessName", "Pre-clean");
                pmc4.Attributes.Add("ProcessType", "Process");

                // PMC4에 IO 장치 추가
                var pmc4PressureSensor = new EquipmentNode
                {
                    NodeId = "PMC4-PressureSensor-01",
                    NodeName = "PressureSensor",
                    NodeType = "Sensor",
                    Description = "Chamber Pressure Sensor"
                };
                pmc4.Children.Add(pmc4PressureSensor);

                var pmc4RFGenerator = new EquipmentNode
                {
                    NodeId = "PMC4-RFGenerator-01",
                    NodeName = "RFGenerator",
                    NodeType = "Generator",
                    Description = "RF Generator for Plasma Generation"
                };
                pmc4.Children.Add(pmc4RFGenerator);

                // MFC 추가
                var pmc4MFCs = new[] {
            new { Id = "PMC4-MFC-01", Name = "MFC_H2", Gas = "H2" },
            new { Id = "PMC4-MFC-02", Name = "MFC_Ar", Gas = "Ar" },
            new { Id = "PMC4-MFC-03", Name = "MFC_N2", Gas = "N2" },
            new { Id = "PMC4-MFC-04", Name = "MFC_He", Gas = "He" }
        };

                foreach (var mfc in pmc4MFCs)
                {
                    var mfcNode = new EquipmentNode
                    {
                        NodeId = mfc.Id,
                        NodeName = mfc.Name,
                        NodeType = "Controller",
                        Description = $"Mass Flow Controller for {mfc.Gas}"
                    };
                    pmc4.Children.Add(mfcNode);
                }

                // PMC4에 머티리얼 위치 추가
                var pmc4WaferSlot = new EquipmentNode
                {
                    NodeId = "PMC4-WaferSlot-001",
                    NodeName = "WaferSlot",
                    NodeType = "MaterialLocation",
                    Description = "Wafer Processing Position"
                };
                pmc4WaferSlot.Attributes.Add("MaterialType", "Substrate");
                pmc4WaferSlot.Attributes.Add("MaterialSubType", "300mm Wafer");
                pmc4.Children.Add(pmc4WaferSlot);

                structure.Modules.Add(pmc4);

                // PVD Ti Chamber (PMC6)
                var pmc6 = new EquipmentNode
                {
                    NodeId = "PMC6",
                    NodeName = "PVD_Ti",
                    NodeType = "Process Area",
                    Description = "Physical Vapor Deposition Chamber for Titanium"
                };
                pmc6.Attributes.Add("ProcessName", "Sputtering");
                pmc6.Attributes.Add("ProcessType", "Process");

                // PMC6에 IO 장치 추가
                var pmc6PressureSensor = new EquipmentNode
                {
                    NodeId = "PMC6-PressureSensor-01",
                    NodeName = "PressureSensor",
                    NodeType = "Sensor",
                    Description = "Chamber Pressure Sensor"
                };
                pmc6.Children.Add(pmc6PressureSensor);

                var pmc6DCPower = new EquipmentNode
                {
                    NodeId = "PMC6-DCPower-01",
                    NodeName = "DCPowerSupply",
                    NodeType = "Power",
                    Description = "DC Power Supply for Sputtering"
                };
                pmc6.Children.Add(pmc6DCPower);

                var pmc6MFC = new EquipmentNode
                {
                    NodeId = "PMC6-MFC-01",
                    NodeName = "MFC_Ar",
                    NodeType = "Controller",
                    Description = "Mass Flow Controller for Ar"
                };
                pmc6.Children.Add(pmc6MFC);

                var pmc6HeaterController = new EquipmentNode
                {
                    NodeId = "PMC6-HeaterController-01",
                    NodeName = "HeaterController",
                    NodeType = "Controller",
                    Description = "Heater Controller for Pedestal"
                };
                pmc6.Children.Add(pmc6HeaterController);

                // PMC6에 머티리얼 위치 추가
                var pmc6WaferSlot = new EquipmentNode
                {
                    NodeId = "PMC6-WaferSlot-001",
                    NodeName = "WaferSlot",
                    NodeType = "MaterialLocation",
                    Description = "Wafer Processing Position"
                };
                pmc6WaferSlot.Attributes.Add("MaterialType", "Substrate");
                pmc6WaferSlot.Attributes.Add("MaterialSubType", "300mm Wafer");
                pmc6.Children.Add(pmc6WaferSlot);

                var pmc6Target = new EquipmentNode
                {
                    NodeId = "PMC6-Target-001",
                    NodeName = "TiTarget",
                    NodeType = "MaterialLocation",
                    Description = "Titanium Target"
                };
                pmc6Target.Attributes.Add("MaterialType", "ProcessDurable");
                pmc6Target.Attributes.Add("MaterialSubType", "Sputtering Target");
                pmc6.Children.Add(pmc6Target);

                structure.Modules.Add(pmc6);

                // PVD Cu Chamber (PMC7)
                var pmc7 = new EquipmentNode
                {
                    NodeId = "PMC7",
                    NodeName = "PVD_Cu",
                    NodeType = "Process Area",
                    Description = "Physical Vapor Deposition Chamber for Copper"
                };
                pmc7.Attributes.Add("ProcessName", "Sputtering");
                pmc7.Attributes.Add("ProcessType", "Process");

                // PMC7에 IO 장치 추가 (PMC6와 유사)
                var pmc7PressureSensor = new EquipmentNode
                {
                    NodeId = "PMC7-PressureSensor-01",
                    NodeName = "PressureSensor",
                    NodeType = "Sensor",
                    Description = "Chamber Pressure Sensor"
                };
                pmc7.Children.Add(pmc7PressureSensor);

                var pmc7DCPower = new EquipmentNode
                {
                    NodeId = "PMC7-DCPower-01",
                    NodeName = "DCPowerSupply",
                    NodeType = "Power",
                    Description = "DC Power Supply for Sputtering"
                };
                pmc7.Children.Add(pmc7DCPower);

                var pmc7MFC = new EquipmentNode
                {
                    NodeId = "PMC7-MFC-01",
                    NodeName = "MFC_Ar",
                    NodeType = "Controller",
                    Description = "Mass Flow Controller for Ar"
                };
                pmc7.Children.Add(pmc7MFC);

                var pmc7HeaterController = new EquipmentNode
                {
                    NodeId = "PMC7-HeaterController-01",
                    NodeName = "HeaterController",
                    NodeType = "Controller",
                    Description = "Heater Controller for Pedestal"
                };
                pmc7.Children.Add(pmc7HeaterController);

                // PMC7에 머티리얼 위치 추가
                var pmc7WaferSlot = new EquipmentNode
                {
                    NodeId = "PMC7-WaferSlot-001",
                    NodeName = "WaferSlot",
                    NodeType = "MaterialLocation",
                    Description = "Wafer Processing Position"
                };
                pmc7WaferSlot.Attributes.Add("MaterialType", "Substrate");
                pmc7WaferSlot.Attributes.Add("MaterialSubType", "300mm Wafer");
                pmc7.Children.Add(pmc7WaferSlot);

                var pmc7Target = new EquipmentNode
                {
                    NodeId = "PMC7-Target-001",
                    NodeName = "CuTarget",
                    NodeType = "MaterialLocation",
                    Description = "Copper Target"
                };
                pmc7Target.Attributes.Add("MaterialType", "ProcessDurable");
                pmc7Target.Attributes.Add("MaterialSubType", "Sputtering Target");
                pmc7.Children.Add(pmc7Target);

                structure.Modules.Add(pmc7);

                // 2. 서브시스템 추가
                // Equipment Front End Module (EFEM)
                var efem = new EquipmentNode
                {
                    NodeId = "EFEM",
                    NodeName = "EFEM",
                    NodeType = "Transport",
                    Description = "Equipment Front End Module"
                };

                // EFEM에 IO 장치 추가
                var efemRobot = new EquipmentNode
                {
                    NodeId = "EFEM-Robot-01",
                    NodeName = "ATMRobot",
                    NodeType = "Robot",
                    Description = "Atmospheric Robot for Wafer Handling"
                };
                efem.Children.Add(efemRobot);

                var efemAligner = new EquipmentNode
                {
                    NodeId = "EFEM-Aligner-01",
                    NodeName = "PreAligner",
                    NodeType = "Aligner",
                    Description = "Wafer Pre-Aligner"
                };
                efem.Children.Add(efemAligner);

                // EFEM에 로드포트 추가
                var efemLoadPort1 = new EquipmentNode
                {
                    NodeId = "EFEM-LP-001",
                    NodeName = "LoadPort1",
                    NodeType = "MaterialLocation",
                    Description = "Wafer FOUP LoadPort 1"
                };
                efemLoadPort1.Attributes.Add("MaterialType", "Carrier");
                efemLoadPort1.Attributes.Add("MaterialSubType", "FOUP");
                efem.Children.Add(efemLoadPort1);

                var efemLoadPort3 = new EquipmentNode
                {
                    NodeId = "EFEM-LP-003",
                    NodeName = "LoadPort3",
                    NodeType = "MaterialLocation",
                    Description = "Wafer FOUP LoadPort 3"
                };
                efemLoadPort3.Attributes.Add("MaterialType", "Carrier");
                efemLoadPort3.Attributes.Add("MaterialSubType", "FOUP");
                efem.Children.Add(efemLoadPort3);

                structure.Subsystems.Add(efem);

                // Transfer Module (TRM)
                var trm = new EquipmentNode
                {
                    NodeId = "TRM",
                    NodeName = "TRM",
                    NodeType = "Transport",
                    Description = "Transfer Module"
                };

                // TRM에 IO 장치 추가
                var trmRobot = new EquipmentNode
                {
                    NodeId = "TRM-Robot-01",
                    NodeName = "VacuumRobot",
                    NodeType = "Robot",
                    Description = "Vacuum Robot for Wafer Handling"
                };
                trm.Children.Add(trmRobot);

                var trmPressureSensor = new EquipmentNode
                {
                    NodeId = "TRM-PressureSensor-01",
                    NodeName = "PressureSensor",
                    NodeType = "Sensor",
                    Description = "TRM Pressure Sensor"
                };
                trm.Children.Add(trmPressureSensor);

                var trmCryoPump = new EquipmentNode
                {
                    NodeId = "TRM-CryoPump-01",
                    NodeName = "CryoPump",
                    NodeType = "Pump",
                    Description = "Cryo Pump for TRM"
                };
                trm.Children.Add(trmCryoPump);

                var trmMFC = new EquipmentNode
                {
                    NodeId = "TRM-MFC-01",
                    NodeName = "MFC_Ar",
                    NodeType = "Controller",
                    Description = "Mass Flow Controller for Ar"
                };
                trm.Children.Add(trmMFC);

                structure.Subsystems.Add(trm);

                // Gas System
                var gasSystem = new EquipmentNode
                {
                    NodeId = "GasSystem",
                    NodeName = "GasSystem",
                    NodeType = "Gas Distributor",
                    Description = "Gas Distribution System"
                };
                structure.Subsystems.Add(gasSystem);

                // Statistics System
                var statistics = new EquipmentNode
                {
                    NodeId = "Statistics",
                    NodeName = "Statistics",
                    NodeType = "Control",
                    Description = "Equipment Statistics"
                };

                var connectionStatus = new EquipmentNode
                {
                    NodeId = "ConnectionStatus-001",
                    NodeName = "ConnectionStatus",
                    NodeType = "Sensor",
                    Description = "Connection Status"
                };
                statistics.Children.Add(connectionStatus);

                structure.Subsystems.Add(statistics);

                _logger.LogInformation($"장비 '{equipmentUid}' 전체 구조 메타데이터 조회 완료");
                return structure;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"장비 구조 메타데이터 조회 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 장비의 특정 노드 상세 설명 메타데이터 조회
        /// </summary>
        public (List<EquipmentNodeDescription>, List<string>) GetEquipmentNodeDescriptions(string equipmentUid, List<string> nodeIds)
        {
            try
            {
                _logger.LogInformation($"장비 '{equipmentUid}' 노드 상세 설명 메타데이터 조회 시작: {nodeIds.Count}개 노드");

                var nodeDescriptions = new List<EquipmentNodeDescription>();
                var unrecognizedNodeIds = new List<string>();

                // 장비 구조 가져오기
                var structure = GetEquipmentStructure(equipmentUid);
                if (structure == null)
                {
                    _logger.LogWarning($"장비 '{equipmentUid}'를 찾을 수 없습니다.");
                    return (nodeDescriptions, nodeIds); // 모든 노드 ID가 인식되지 않음
                }

                // 모든 노드 수집
                var allNodes = CollectAllNodes(structure);

                // 각 요청된 노드 ID에 대해 처리
                foreach (var nodeId in nodeIds)
                {
                    var node = allNodes.FirstOrDefault(n => n.NodeId == nodeId);

                    if (node == null)
                    {
                        // 노드를 찾을 수 없음
                        unrecognizedNodeIds.Add(nodeId);
                        continue;
                    }

                    // 노드 설명 생성
                    var nodeDescription = new EquipmentNodeDescription
                    {
                        NodeId = node.NodeId,
                        NodeName = node.NodeName,
                        NodeType = node.NodeType,
                        Description = node.Description
                    };

                    // 속성 복사
                    foreach (var attr in node.Attributes)
                    {
                        nodeDescription.Attributes.Add(attr.Key, attr.Value);
                    }

                    // 노드 유형에 따라 관련 정보 추가
                    switch (node.NodeType)
                    {
                        case "Process Area":
                        case "Chamber":
                            // 공정 챔버에 대한 추가 정보
                            nodeDescription.StateMachineIds.Add("sm_pvd_process"); // 공정 상태 머신
                            nodeDescription.ObjTypeIds.Add("SPUTTER_ChamberObj"); // 챔버 객체 타입

                            // 매개변수 ID 추가
                            nodeDescription.ParameterIds.AddRange(new[]
                            {
                        "P001", "P002", "P003", "P004", "P005", "P006", // 공정 매개변수
                        "G001", "G002", "G003", // 가스 매개변수
                        "C001", "C002", "C003", "C004", // 챔버 설정
                        "A001", "A002", "A003" // 알람 임계값
                    });

                            // 예외 ID 추가
                            nodeDescription.ExceptionIds.AddRange(new[]
                            {
                        "PRC001", "PRC002", "PRC003", "PRC004", "PRC005", // 프로세스 오류
                        "VAC001", "VAC002", "VAC003", "VAC004", // 진공 오류
                        "PWR001", "PWR002", "PWR003", "PWR004" // 전원 오류
                    });

                            // 이벤트 ID 추가
                            nodeDescription.SimpleEventIds.AddRange(new[]
                            {
                        "EVT001", "EVT002", "EVT003", "EVT004", "EVT005", "EVT006", "EVT007", // 공정 이벤트
                        "EVT101", "EVT102", "EVT105", // 웨이퍼 이벤트
                        "EVT401", "EVT402", "EVT403", "EVT404", "EVT405" // 알람 이벤트
                    });
                            break;

                        case "Robot":
                            // 로봇에 대한 추가 정보
                            nodeDescription.StateMachineIds.Add("sm_vacuum_robot"); // 로봇 상태 머신

                            // 매개변수 ID 추가
                            nodeDescription.ParameterIds.AddRange(new[] { "R001", "R002", "R003" });

                            // 예외 ID 추가
                            nodeDescription.ExceptionIds.AddRange(new[] { "RBT001", "RBT002", "RBT003", "RBT004" });

                            // 이벤트 ID 추가
                            nodeDescription.SimpleEventIds.AddRange(new[]
                            {
                        "EVT201", "EVT202", "EVT203", "EVT204", "EVT205", "EVT206" // 로봇 이벤트
                    });
                            break;

                        case "Transport":
                            // 이송 모듈에 대한 추가 정보
                            nodeDescription.ObjTypeIds.Add("E87_LoadPortObj"); // SEMI E87 로드 포트 객체

                            // 이벤트 ID 추가
                            if (node.NodeId == "EFEM")
                            {
                                nodeDescription.SimpleEventIds.AddRange(new[]
                                {
                            "EVT103", "EVT501", "EVT502", "EVT503", "EVT504" // EFEM 관련 이벤트
                        });
                            }
                            else if (node.NodeId == "TRM")
                            {
                                nodeDescription.SimpleEventIds.AddRange(new[]
                                {
                            "EVT201", "EVT202" // TRM 관련 이벤트
                        });
                            }
                            break;

                        case "Sensor":
                            // 센서에 대한 추가 정보
                            if (node.NodeName.Contains("Pressure"))
                            {
                                nodeDescription.ObjTypeIds.Add("E30_DataVariable"); // SEMI E30 데이터 변수
                                nodeDescription.SimpleEventIds.Add("EVT401"); // 압력 관련 이벤트
                            }
                            else if (node.NodeName.Contains("Temperature"))
                            {
                                nodeDescription.ObjTypeIds.Add("E30_DataVariable"); // SEMI E30 데이터 변수
                                nodeDescription.SimpleEventIds.Add("EVT402"); // 온도 관련 이벤트
                            }
                            break;

                        case "Controller":
                            // 컨트롤러에 대한 추가 정보
                            if (node.NodeName.Contains("MFC"))
                            {
                                nodeDescription.ObjTypeIds.Add("E30_DataVariable"); // SEMI E30 데이터 변수
                                nodeDescription.SimpleEventIds.Add("EVT404"); // 가스 유량 관련 이벤트
                                nodeDescription.ParameterIds.Add("G001"); // 가스 유량 매개변수
                                nodeDescription.ExceptionIds.Add("GAS001"); // MFC 오류
                            }
                            else if (node.NodeName.Contains("Heater"))
                            {
                                nodeDescription.ObjTypeIds.Add("E30_DataVariable"); // SEMI E30 데이터 변수
                                nodeDescription.SimpleEventIds.Add("EVT402"); // 온도 관련 이벤트
                                nodeDescription.ParameterIds.Add("P004"); // 온도 매개변수
                            }
                            break;

                        case "Power":
                            // 전원 공급 장치에 대한 추가 정보
                            nodeDescription.ObjTypeIds.Add("SPUTTER_PowerSupplyObj"); // 전원 객체 타입
                            nodeDescription.SimpleEventIds.Add("EVT403"); // 전력 관련 이벤트
                            nodeDescription.ParameterIds.Add("P003"); // 전력 매개변수
                            nodeDescription.ExceptionIds.AddRange(new[] { "PWR001", "PWR002", "PWR003" }); // 전원 오류
                            break;
                    }

                    nodeDescriptions.Add(nodeDescription);
                }

                _logger.LogInformation($"장비 '{equipmentUid}' 노드 상세 설명 메타데이터 조회 완료: " +
                                       $"{nodeDescriptions.Count}개 노드 정보 반환, {unrecognizedNodeIds.Count}개 인식되지 않음");
                return (nodeDescriptions, unrecognizedNodeIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"노드 상세 설명 메타데이터 조회 중 오류: {ex.Message}");
                throw;
            }
        }

        // 장비 구조에서 모든 노드 수집 (재귀적으로)
        private List<EquipmentNode> CollectAllNodes(EquipmentStructure structure)
        {
            var nodes = new List<EquipmentNode>();

            // 장비 자체를 루트 노드로 추가
            var rootNode = new EquipmentNode
            {
                NodeId = structure.EquipmentUid,
                NodeName = structure.EquipmentName,
                NodeType = structure.EquipmentType,
                Description = structure.Description
            };

            foreach (var attr in structure.Attributes)
            {
                rootNode.Attributes.Add(attr.Key, attr.Value);
            }

            nodes.Add(rootNode);

            // 모듈 추가
            foreach (var module in structure.Modules)
            {
                nodes.Add(module);
                nodes.AddRange(CollectChildNodes(module));
            }

            // 서브시스템 추가
            foreach (var subsystem in structure.Subsystems)
            {
                nodes.Add(subsystem);
                nodes.AddRange(CollectChildNodes(subsystem));
            }

            // IO 장치 추가
            foreach (var ioDevice in structure.IODevices)
            {
                nodes.Add(ioDevice);
                nodes.AddRange(CollectChildNodes(ioDevice));
            }

            return nodes;
        }

        // 노드의 모든 자식 노드 수집 (재귀적으로)
        private List<EquipmentNode> CollectChildNodes(EquipmentNode node)
        {
            var nodes = new List<EquipmentNode>();

            foreach (var child in node.Children)
            {
                nodes.Add(child);
                nodes.AddRange(CollectChildNodes(child));
            }

            return nodes;
        }

        /// <summary>
        /// 특정 메타데이터 항목 조회
        /// </summary>
        public MetadataItem GetMetadataItem(string equipmentUid, string itemId)
        {
            var metadata = GetMetadata(equipmentUid);
            if (metadata != null)
            {
                return metadata.Items.FirstOrDefault(item => item.ItemId == itemId);
            }

            return null;
        }

        /// <summary>
        /// 장비 메타데이터 삭제
        /// </summary>
        public bool RemoveMetadata(string equipmentUid)
        {
            return _equipmentMetadataCache.Remove(equipmentUid);
        }
    }

    /// <summary>
    /// 장비 메타데이터 클래스
    /// </summary>
    public class EquipmentMetadata
    {
        public string EquipmentUid { get; set; }
        public string EquipmentName { get; set; }
        public string MetadataVersion { get; set; }
        public DateTime CreatedTimestamp { get; set; }
        public DateTime LastModifiedTimestamp { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public List<MetadataItem> Items { get; set; } = new List<MetadataItem>();
    }

    /// <summary>
    /// 메타데이터 항목 클래스
    /// </summary>
    public class MetadataItem
    {
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; }
        public string Description { get; set; }
        public string DataSourceUid { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }
}