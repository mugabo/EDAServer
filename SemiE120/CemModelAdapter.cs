using Newtonsoft.Json;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System;

namespace SemiE120.CEM
{
    public class CemModelAdapter
    {
        private Dictionary<string, string> _cemToOpcTagMap;
        private Dictionary<string, Type> _tagDataTypes;
        private Equipment _cemEquipment;
        private bool _isMonitoring = false;

        // 설정 파일 경로를 위한 필드 추가
        private readonly string _modelConfigPath;

        public CemModelAdapter(string modelConfigPath = null)
        {
            _cemToOpcTagMap = new Dictionary<string, string>();
            _tagDataTypes = new Dictionary<string, Type>();

            // 설정 파일 경로 설정
            _modelConfigPath = modelConfigPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "equipment_model.json");

            // CEM 모델 구조 생성
            InitializeCemModel();

            // 태그 매핑 설정
            InitializeTagMapping();
        }

        private void InitializeCemModel()
        {
            try
            {
                // 설정 파일이 존재하는지 확인
                if (!File.Exists(_modelConfigPath))
                {
                    Console.WriteLine($"설정 파일을 찾을 수 없습니다: {_modelConfigPath}");
                    // 파일이 없을 경우 기본 모델 생성
                    CreateDefaultCemModel();
                    // 선택적으로 기본 모델을 파일로 저장할 수 있음
                    SaveModelToFile(_cemEquipment);
                    return;
                }

                // 설정 파일 읽기
                string jsonContent = File.ReadAllText(_modelConfigPath);

                // JSON 파싱 및 장비 객체 생성
                var jsonSettings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    NullValueHandling = NullValueHandling.Ignore
                };

                // JSON에서 루트 객체 가져오기
                var modelData = JsonConvert.DeserializeObject<dynamic>(jsonContent, jsonSettings);

                // 장비 객체 생성
                _cemEquipment = DeserializeEquipment(modelData.equipment);

                Console.WriteLine($"장비 모델을 설정 파일에서 로드했습니다: {_modelConfigPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"설정 파일 로드 중 오류 발생: {ex.Message}");
                // 오류 발생 시 기본 모델 생성
                CreateDefaultCemModel();
            }
        }

        // 저장된 JSON에서 Equipment 객체 생성
        private Equipment DeserializeEquipment(dynamic equipmentData)
        {
            var equipment = new Equipment
            {
                Uid = equipmentData.uid,
                Name = equipmentData.name,
                Description = equipmentData.description,
                ElementType = equipmentData.elementType,
                Supplier = equipmentData.supplier,
                ProcessName = equipmentData.processName,
                ProcessTypeValue = ParseProcessType(equipmentData.processType?.ToString()),
                RecipeType = equipmentData.recipeType
            };

            // 모듈 추가
            if (equipmentData.modules != null)
            {
                foreach (var moduleData in equipmentData.modules)
                {
                    var module = DeserializeModule(moduleData);
                    equipment.Modules.Add(module);
                }
            }

            // 서브시스템 추가
            if (equipmentData.subsystems != null)
            {
                foreach (var subsystemData in equipmentData.subsystems)
                {
                    var subsystem = DeserializeSubsystem(subsystemData);
                    equipment.Subsystems.Add(subsystem);
                }
            }

            return equipment;
        }

        // Module 객체 생성
        private Module DeserializeModule(dynamic moduleData)
        {
            var module = new Module
            {
                Uid = moduleData.uid,
                Name = moduleData.name,
                Description = moduleData.description,
                ElementType = moduleData.elementType,
                ProcessName = moduleData.processName,
                ProcessTypeValue = ParseProcessType(moduleData.processType?.ToString())
            };

            // IO 장치 추가
            if (moduleData.ioDevices != null)
            {
                foreach (var ioDeviceData in moduleData.ioDevices)
                {
                    var ioDevice = DeserializeIODevice(ioDeviceData);
                    module.IODevices.Add(ioDevice);
                }
            }

            // 머티리얼 로케이션 추가
            if (moduleData.materialLocations != null)
            {
                foreach (var locationData in moduleData.materialLocations)
                {
                    var location = DeserializeMaterialLocation(locationData);
                    module.MaterialLocations.Add(location);
                }
            }

            return module;
        }

        // Subsystem 객체 생성
        private Subsystem DeserializeSubsystem(dynamic subsystemData)
        {
            var subsystem = new Subsystem
            {
                Uid = subsystemData.uid,
                Name = subsystemData.name,
                Description = subsystemData.description,
                ElementType = subsystemData.elementType
            };

            // IO 장치 추가
            if (subsystemData.ioDevices != null)
            {
                foreach (var ioDeviceData in subsystemData.ioDevices)
                {
                    var ioDevice = DeserializeIODevice(ioDeviceData);
                    subsystem.IODevices.Add(ioDevice);
                }
            }

            return subsystem;
        }

        // IODevice 객체 생성
        private IODevice DeserializeIODevice(dynamic ioDeviceData)
        {
            return new IODevice
            {
                Uid = ioDeviceData.uid,
                Name = ioDeviceData.name,
                Description = ioDeviceData.description,
                ElementType = ioDeviceData.elementType
            };
        }

        // MaterialLocation 객체 생성
        private MaterialLocation DeserializeMaterialLocation(dynamic locationData)
        {
            return new MaterialLocation
            {
                Uid = locationData.uid,
                Name = locationData.name,
                Description = locationData.description,
                MaterialTypeValue = ParseMaterialType(locationData.materialType?.ToString()),
                MaterialSubType = locationData.materialSubType
            };
        }

        // 문자열을 ProcessType 열거형으로 변환
        private ProcessType ParseProcessType(string processTypeStr)
        {
            if (string.IsNullOrEmpty(processTypeStr))
                return ProcessType.Process;

            if (Enum.TryParse<ProcessType>(processTypeStr, true, out var result))
                return result;

            return ProcessType.Process;
        }

        // 문자열을 MaterialType 열거형으로 변환
        private MaterialType ParseMaterialType(string materialTypeStr)
        {
            if (string.IsNullOrEmpty(materialTypeStr))
                return MaterialType.Other;

            if (Enum.TryParse<MaterialType>(materialTypeStr, true, out var result))
                return result;

            return MaterialType.Other;
        }

        // 기본 모델 생성 (기존 하드코딩된 모델)
        private void CreateDefaultCemModel()
        {
            // 기존 하드코딩된 내용과 동일
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

            // 기존 코드 계속...
        }

        // 모델을 JSON 파일로 저장
        private void SaveModelToFile(Equipment equipment)
        {
            try
            {
                // 모델을 동적 객체로 변환
                var equipmentObject = new
                {
                    equipment = ConvertEquipmentToJsonObject(equipment)
                };

                // JSON으로 직렬화 및 저장
                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };

                string jsonContent = JsonConvert.SerializeObject(equipmentObject, jsonSettings);
                File.WriteAllText(_modelConfigPath, jsonContent);

                Console.WriteLine($"기본 장비 모델이 파일로 저장되었습니다: {_modelConfigPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"모델 저장 중 오류 발생: {ex.Message}");
            }
        }

        // Equipment 객체를 JSON 직렬화 가능 객체로 변환
        private object ConvertEquipmentToJsonObject(Equipment equipment)
        {
            return new
            {
                uid = equipment.Uid,
                name = equipment.Name,
                description = equipment.Description,
                elementType = equipment.ElementType,
                supplier = equipment.Supplier,
                processName = equipment.ProcessName,
                processType = equipment.ProcessTypeValue.ToString(),
                recipeType = equipment.RecipeType,
                modules = equipment.Modules.Select(m => ConvertModuleToJsonObject(m)).ToArray(),
                subsystems = equipment.Subsystems.Select(s => ConvertSubsystemToJsonObject(s)).ToArray()
            };
        }

        // Module 객체를 JSON 직렬화 가능 객체로 변환
        private object ConvertModuleToJsonObject(Module module)
        {
            return new
            {
                uid = module.Uid,
                name = module.Name,
                description = module.Description,
                elementType = module.ElementType,
                processName = module.ProcessName,
                processType = module.ProcessTypeValue.ToString(),
                ioDevices = module.IODevices.Select(d => ConvertIODeviceToJsonObject(d)).ToArray(),
                materialLocations = module.MaterialLocations.Select(l => ConvertMaterialLocationToJsonObject(l)).ToArray()
            };
        }

        // Subsystem 객체를 JSON 직렬화 가능 객체로 변환
        private object ConvertSubsystemToJsonObject(Subsystem subsystem)
        {
            return new
            {
                uid = subsystem.Uid,
                name = subsystem.Name,
                description = subsystem.Description,
                elementType = subsystem.ElementType,
                ioDevices = subsystem.IODevices.Select(d => ConvertIODeviceToJsonObject(d)).ToArray()
            };
        }

        // IODevice 객체를 JSON 직렬화 가능 객체로 변환
        private object ConvertIODeviceToJsonObject(IODevice ioDevice)
        {
            return new
            {
                uid = ioDevice.Uid,
                name = ioDevice.Name,
                description = ioDevice.Description,
                elementType = ioDevice.ElementType
            };
        }

        // MaterialLocation 객체를 JSON 직렬화 가능 객체로 변환
        private object ConvertMaterialLocationToJsonObject(MaterialLocation location)
        {
            return new
            {
                uid = location.Uid,
                name = location.Name,
                description = location.Description,
                materialType = location.MaterialTypeValue.ToString(),
                materialSubType = location.MaterialSubType
            };
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
            throw new KeyNotFoundException($"CEM 경로 '{cemPath}'에 대한 매핑이 없습니다.");
        }

        public async Task<bool> SetValueAsync(string cemPath, object value)
        {
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