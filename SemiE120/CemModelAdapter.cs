using Opc.Ua;
using SemiE120.CEM;
using SemiE120.OpcUaIntegration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SemiE120.CEM
{
    public class CemModelAdapter
    {
        private OpcUaClient _opcClient;
        private Dictionary<string, string> _cemToOpcTagMap; // CEM 경로를 OPC UA 노드 ID에 매핑
        private Dictionary<string, Type> _tagDataTypes; // CEM 경로별 데이터 타입
        private Equipment _cemEquipment; // SEMI E120 Equipment 모델
        private bool _isMonitoring = false;

        public CemModelAdapter(string serverUrl)
        {
            _opcClient = new OpcUaClient();
            ConnectAsync(serverUrl).Wait();

            _cemToOpcTagMap = new Dictionary<string, string>();
            _tagDataTypes = new Dictionary<string, Type>();

            // CEM 모델 구조 생성
            InitializeCemModel();

            // 태그 매핑 설정
            InitializeTagMapping();
        }

        private async Task ConnectAsync(string serverUrl)
        {
            bool connected = await _opcClient.ConnectAsync(serverUrl);
            if (!connected)
            {
                throw new Exception($"OPC UA 서버 {serverUrl}에 연결할 수 없습니다.");
            }
        }

        private void InitializeCemModel()
        {
            // SEMI E120 모델 구조 생성
            _cemEquipment = new Equipment
            {
                Uid = "Equipment-123",
                Name = "EtchTool01",
                Description = "Plasma Etching Tool",
                ElementType = "Process",
                Supplier = "Acme Semiconductor Equipment",
                ProcessName = "Etch",
                ProcessTypeValue = ProcessType.Process,
                RecipeType = "PlasmaEtch"
            };

            // Module 추가
            var chamber = new Module
            {
                Uid = "Chamber1-456",
                Name = "Chamber1",
                Description = "Main Process Chamber",
                ElementType = "Process Area",
                ProcessName = "MainEtch",
                ProcessTypeValue = ProcessType.Process
            };
            _cemEquipment.Modules.Add(chamber);

            // IO 장치 추가
            var pressureSensor = new IODevice
            {
                Uid = "PressureSensor-101",
                Name = "PressureSensor",
                Description = "Chamber Pressure Sensor",
                ElementType = "Sensor"
            };
            chamber.IODevices.Add(pressureSensor);

            // 머티리얼 로케이션 추가
            var waferSlot = new MaterialLocation
            {
                Uid = "WaferSlot-202",
                Name = "WaferSlot",
                Description = "Wafer Processing Position",
                MaterialTypeValue = MaterialType.Substrate,
                MaterialSubType = "300mm Wafer"
            };
            chamber.MaterialLocations.Add(waferSlot);

            // Subsystem 추가
            var gasSystem = new Subsystem
            {
                Uid = "GasSystem-789",
                Name = "GasSystem",
                Description = "Gas Distribution System",
                ElementType = "Gas Distributor"
            };
            _cemEquipment.Subsystems.Add(gasSystem);

            // 상태 서브시스템 추가
            var statsSystem = new Subsystem
            {
                Uid = "Statistics-999",
                Name = "Statistics",
                Description = "Equipment Statistics",
                ElementType = "Control"
            };

            var connectionStatus = new IODevice
            {
                Uid = "ConnectionStatus-001",
                Name = "ConnectionStatus",
                Description = "Connection Status",
                ElementType = "Sensor"
            };
            statsSystem.IODevices.Add(connectionStatus);

            _cemEquipment.Subsystems.Add(statsSystem);
        }

        private void InitializeTagMapping()
        {
            // CEM 경로를 OPC UA 노드 ID로 매핑
            _cemToOpcTagMap.Add("Equipment1.Modules.Chamber1.IODevices.PressureSensor.Value", "TM.Robot.Pick");
            _tagDataTypes.Add("Equipment1.Modules.Chamber1.IODevices.PressureSensor.Value", typeof(double));

            _cemToOpcTagMap.Add("Equipment1.Modules.Chamber1.MaterialLocations.WaferSlot.Status", "TM.Robot.Place");
            _tagDataTypes.Add("Equipment1.Modules.Chamber1.MaterialLocations.WaferSlot.Status", typeof(bool));

            _cemToOpcTagMap.Add("Equipment1.Subsystems.Statistics.IODevices.ConnectionStatus.Value", "TM.Robot._Statistics._commStatus");
            _tagDataTypes.Add("Equipment1.Subsystems.Statistics.IODevices.ConnectionStatus.Value", typeof(short));
        }

        public Equipment GetEquipmentModel()
        {
            return _cemEquipment;
        }

        public Dictionary<string, string> GetMappings()
        {
            return new Dictionary<string, string>(_cemToOpcTagMap);
        }

        public Type GetValueDataType(string cemPath)
        {
            if (_tagDataTypes.TryGetValue(cemPath, out Type dataType))
            {
                return dataType;
            }

            throw new KeyNotFoundException($"CEM 경로 '{cemPath}'에 대한 데이터 타입 정보가 없습니다.");
        }

        public async Task<object> GetValueAsync(string cemPath)
        {
            // CEM 경로를 OPC UA 노드 ID로 변환
            if (_cemToOpcTagMap.TryGetValue(cemPath, out string opcNodeId))
            {
                // OPC UA 서버에서 값 읽기
                var value = _opcClient.ReadNode(new NodeId(opcNodeId, 2));  // 네임스페이스 인덱스 2 사용
                return value?.Value;
            }

            throw new KeyNotFoundException($"CEM 경로 '{cemPath}'에 대한 매핑이 없습니다.");
        }

        public async Task<bool> SetValueAsync(string cemPath, object value)
        {
            // CEM 경로를 OPC UA 노드 ID로 변환 후 쓰기
            if (_cemToOpcTagMap.TryGetValue(cemPath, out string opcNodeId))
            {
                return _opcClient.WriteNode(new NodeId(opcNodeId, 2), value) == StatusCodes.Good;
            }

            throw new KeyNotFoundException($"CEM 경로 '{cemPath}'에 대한 매핑이 없습니다.");
        }

        public void StartMonitoring(Action<string, object> callback)
        {
            _isMonitoring = true;

            Task.Run(async () => {
                while (_isMonitoring)
                {
                    foreach (var mapping in _cemToOpcTagMap)
                    {
                        try
                        {
                            var value = await GetValueAsync(mapping.Key);
                            callback(mapping.Key, value);
                        }
                        catch (Exception ex)
                        {
                            callback(mapping.Key, $"오류: {ex.Message}");
                        }
                    }

                    await Task.Delay(1000);  // 1초마다 업데이트
                }
            });
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
        }
    }
}
