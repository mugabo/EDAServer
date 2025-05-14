using System;
using System.Collections.Generic;
using SemiE120.CEM;
using SemiE120.Protobuf;

namespace SemiE120.Converters
{
    /// <summary>
    /// SEMI E120 CEM 모델과 Protocol Buffers 메시지 간의 변환을 처리하는 유틸리티 클래스
    /// </summary>
    public static class CemProtobufConverter
    {
        /// <summary>
        /// CEM Equipment 객체를 EquipmentProto로 변환
        /// </summary>
        public static EquipmentProto ToProtobuf(this Equipment equipment)
        {
            if (equipment == null) return null;

            var proto = new EquipmentProto
            {
                Uid = equipment.Uid,
                Name = equipment.Name,
                Description = equipment.Description,
                ElementType = equipment.ElementType,
                Supplier = equipment.Supplier,
                Make = equipment.Make,
                Model = equipment.Model,
                ModelRevision = equipment.ModelRevision,
                Function = equipment.Function,
                ImmutableId = equipment.ImmutableId,
                ProcessName = equipment.ProcessName,
                ProcessType = ConvertProcessType(equipment.ProcessTypeValue),
                RecipeType = equipment.RecipeType
            };

            // 확장
            foreach (var extension in equipment.Extensions)
            {
                proto.Extensions.Add(ToProtobuf(extension));
            }

            // 논리적 요소
            foreach (var logicalElement in equipment.LogicalElements)
            {
                proto.LogicalElements.Add(ToProtobuf(logicalElement));
            }

            // 소프트웨어 모듈
            foreach (var swModule in equipment.SoftwareModules)
            {
                proto.SoftwareModules.Add(ToProtobuf(swModule));
            }

            // 모듈
            foreach (var module in equipment.Modules)
            {
                proto.Modules.Add(ToProtobuf(module));
            }

            // 서브시스템
            foreach (var subsystem in equipment.Subsystems)
            {
                proto.Subsystems.Add(ToProtobuf(subsystem));
            }

            // IO 장치
            foreach (var ioDevice in equipment.IODevices)
            {
                proto.IoDevices.Add(ToProtobuf(ioDevice));
            }

            // 머티리얼 로케이션
            foreach (var matLocation in equipment.MaterialLocations)
            {
                proto.MaterialLocations.Add(ToProtobuf(matLocation));
            }

            // 장비 목록
            foreach (var childEquipment in equipment.EquipmentList)
            {
                proto.EquipmentList.Add(ToProtobuf(childEquipment));
            }

            return proto;
        }

        /// <summary>
        /// EquipmentProto를 CEM Equipment 객체로 변환
        /// </summary>
        public static Equipment FromProtobuf(this EquipmentProto proto)
        {
            if (proto == null) return null;

            var equipment = new Equipment
            {
                Uid = proto.Uid,
                Name = proto.Name,
                Description = proto.Description,
                ElementType = proto.ElementType,
                Supplier = proto.Supplier,
                Make = proto.Make,
                Model = proto.Model,
                ModelRevision = proto.ModelRevision,
                Function = proto.Function,
                ImmutableId = proto.ImmutableId,
                ProcessName = proto.ProcessName,
                ProcessTypeValue = ConvertProcessType(proto.ProcessType),
                RecipeType = proto.RecipeType
            };

            // 확장
            foreach (var extensionProto in proto.Extensions)
            {
                equipment.Extensions.Add(FromProtobuf(extensionProto));
            }

            // 논리적 요소
            foreach (var logicalElementProto in proto.LogicalElements)
            {
                equipment.LogicalElements.Add(FromProtobuf(logicalElementProto));
            }

            // 소프트웨어 모듈
            foreach (var swModuleProto in proto.SoftwareModules)
            {
                equipment.SoftwareModules.Add(FromProtobuf(swModuleProto));
            }

            // 모듈
            foreach (var moduleProto in proto.Modules)
            {
                equipment.Modules.Add(FromProtobuf(moduleProto));
            }

            // 서브시스템
            foreach (var subsystemProto in proto.Subsystems)
            {
                equipment.Subsystems.Add(FromProtobuf(subsystemProto));
            }

            // IO 장치
            foreach (var ioDeviceProto in proto.IoDevices)
            {
                equipment.IODevices.Add(FromProtobuf(ioDeviceProto));
            }

            // 머티리얼 로케이션
            foreach (var matLocationProto in proto.MaterialLocations)
            {
                equipment.MaterialLocations.Add(FromProtobuf(matLocationProto));
            }

            // 장비 목록
            foreach (var childEquipmentProto in proto.EquipmentList)
            {
                equipment.EquipmentList.Add(FromProtobuf(childEquipmentProto));
            }

            return equipment;
        }

        /// <summary>
        /// Module을 ModuleProto로 변환
        /// </summary>
        public static ModuleProto ToProtobuf(this Module module)
        {
            if (module == null) return null;

            var proto = new ModuleProto
            {
                Uid = module.Uid,
                Name = module.Name,
                Description = module.Description,
                ElementType = module.ElementType,
                Supplier = module.Supplier,
                Make = module.Make,
                Model = module.Model,
                ModelRevision = module.ModelRevision,
                Function = module.Function,
                ImmutableId = module.ImmutableId,
                ProcessName = module.ProcessName,
                ProcessType = ConvertProcessType(module.ProcessTypeValue),
                RecipeType = module.RecipeType
            };

            // 확장
            foreach (var extension in module.Extensions)
            {
                proto.Extensions.Add(ToProtobuf(extension));
            }

            // 논리적 요소
            foreach (var logicalElement in module.LogicalElements)
            {
                proto.LogicalElements.Add(ToProtobuf(logicalElement));
            }

            // 소프트웨어 모듈
            foreach (var swModule in module.SoftwareModules)
            {
                proto.SoftwareModules.Add(ToProtobuf(swModule));
            }

            // 모듈
            foreach (var childModule in module.Modules)
            {
                proto.Modules.Add(ToProtobuf(childModule));
            }

            // 서브시스템
            foreach (var subsystem in module.Subsystems)
            {
                proto.Subsystems.Add(ToProtobuf(subsystem));
            }

            // IO 장치
            foreach (var ioDevice in module.IODevices)
            {
                proto.IoDevices.Add(ToProtobuf(ioDevice));
            }

            // 머티리얼 로케이션
            foreach (var matLocation in module.MaterialLocations)
            {
                proto.MaterialLocations.Add(ToProtobuf(matLocation));
            }

            return proto;
        }

        /// <summary>
        /// ModuleProto를 Module로 변환
        /// </summary>
        public static Module FromProtobuf(this ModuleProto proto)
        {
            if (proto == null) return null;

            var module = new Module
            {
                Uid = proto.Uid,
                Name = proto.Name,
                Description = proto.Description,
                ElementType = proto.ElementType,
                Supplier = proto.Supplier,
                Make = proto.Make,
                Model = proto.Model,
                ModelRevision = proto.ModelRevision,
                Function = proto.Function,
                ImmutableId = proto.ImmutableId,
                ProcessName = proto.ProcessName,
                ProcessTypeValue = ConvertProcessType(proto.ProcessType),
                RecipeType = proto.RecipeType
            };

            // 확장
            foreach (var extensionProto in proto.Extensions)
            {
                module.Extensions.Add(FromProtobuf(extensionProto));
            }

            // 논리적 요소
            foreach (var logicalElementProto in proto.LogicalElements)
            {
                module.LogicalElements.Add(FromProtobuf(logicalElementProto));
            }

            // 소프트웨어 모듈
            foreach (var swModuleProto in proto.SoftwareModules)
            {
                module.SoftwareModules.Add(FromProtobuf(swModuleProto));
            }

            // 모듈
            foreach (var moduleProto in proto.Modules)
            {
                module.Modules.Add(FromProtobuf(moduleProto));
            }

            // 서브시스템
            foreach (var subsystemProto in proto.Subsystems)
            {
                module.Subsystems.Add(FromProtobuf(subsystemProto));
            }

            // IO 장치
            foreach (var ioDeviceProto in proto.IoDevices)
            {
                module.IODevices.Add(FromProtobuf(ioDeviceProto));
            }

            // 머티리얼 로케이션
            foreach (var matLocationProto in proto.MaterialLocations)
            {
                module.MaterialLocations.Add(FromProtobuf(matLocationProto));
            }

            return module;
        }

        /// <summary>
        /// Subsystem을 SubsystemProto로 변환
        /// </summary>
        public static SubsystemProto ToProtobuf(this Subsystem subsystem)
        {
            if (subsystem == null) return null;

            var proto = new SubsystemProto
            {
                Uid = subsystem.Uid,
                Name = subsystem.Name,
                Description = subsystem.Description,
                ElementType = subsystem.ElementType,
                Supplier = subsystem.Supplier,
                Make = subsystem.Make,
                Model = subsystem.Model,
                ModelRevision = subsystem.ModelRevision,
                Function = subsystem.Function,
                ImmutableId = subsystem.ImmutableId
            };

            // 확장
            foreach (var extension in subsystem.Extensions)
            {
                proto.Extensions.Add(ToProtobuf(extension));
            }

            // 논리적 요소
            foreach (var logicalElement in subsystem.LogicalElements)
            {
                proto.LogicalElements.Add(ToProtobuf(logicalElement));
            }

            // 소프트웨어 모듈
            foreach (var swModule in subsystem.SoftwareModules)
            {
                proto.SoftwareModules.Add(ToProtobuf(swModule));
            }

            // 서브시스템
            foreach (var childSubsystem in subsystem.Subsystems)
            {
                proto.Subsystems.Add(ToProtobuf(childSubsystem));
            }

            // IO 장치
            foreach (var ioDevice in subsystem.IODevices)
            {
                proto.IoDevices.Add(ToProtobuf(ioDevice));
            }

            // 머티리얼 로케이션
            foreach (var matLocation in subsystem.MaterialLocations)
            {
                proto.MaterialLocations.Add(ToProtobuf(matLocation));
            }

            return proto;
        }

        /// <summary>
        /// SubsystemProto를 Subsystem으로 변환
        /// </summary>
        public static Subsystem FromProtobuf(this SubsystemProto proto)
        {
            if (proto == null) return null;

            var subsystem = new Subsystem
            {
                Uid = proto.Uid,
                Name = proto.Name,
                Description = proto.Description,
                ElementType = proto.ElementType,
                Supplier = proto.Supplier,
                Make = proto.Make,
                Model = proto.Model,
                ModelRevision = proto.ModelRevision,
                Function = proto.Function,
                ImmutableId = proto.ImmutableId
            };

            // 확장
            foreach (var extensionProto in proto.Extensions)
            {
                subsystem.Extensions.Add(FromProtobuf(extensionProto));
            }

            // 논리적 요소
            foreach (var logicalElementProto in proto.LogicalElements)
            {
                subsystem.LogicalElements.Add(FromProtobuf(logicalElementProto));
            }

            // 소프트웨어 모듈
            foreach (var swModuleProto in proto.SoftwareModules)
            {
                subsystem.SoftwareModules.Add(FromProtobuf(swModuleProto));
            }

            // 서브시스템
            foreach (var subsystemProto in proto.Subsystems)
            {
                subsystem.Subsystems.Add(FromProtobuf(subsystemProto));
            }

            // IO 장치
            foreach (var ioDeviceProto in proto.IoDevices)
            {
                subsystem.IODevices.Add(FromProtobuf(ioDeviceProto));
            }

            // 머티리얼 로케이션
            foreach (var matLocationProto in proto.MaterialLocations)
            {
                subsystem.MaterialLocations.Add(FromProtobuf(matLocationProto));
            }

            return subsystem;
        }

        /// <summary>
        /// IODevice를 IODeviceProto로 변환
        /// </summary>
        public static IODeviceProto ToProtobuf(this IODevice ioDevice)
        {
            if (ioDevice == null) return null;

            var proto = new IODeviceProto
            {
                Uid = ioDevice.Uid,
                Name = ioDevice.Name,
                Description = ioDevice.Description,
                ElementType = ioDevice.ElementType,
                Supplier = ioDevice.Supplier,
                Make = ioDevice.Make,
                Model = ioDevice.Model,
                ModelRevision = ioDevice.ModelRevision,
                Function = ioDevice.Function,
                ImmutableId = ioDevice.ImmutableId
            };

            // 확장
            foreach (var extension in ioDevice.Extensions)
            {
                proto.Extensions.Add(ToProtobuf(extension));
            }

            // 논리적 요소
            foreach (var logicalElement in ioDevice.LogicalElements)
            {
                proto.LogicalElements.Add(ToProtobuf(logicalElement));
            }

            // 소프트웨어 모듈
            foreach (var swModule in ioDevice.SoftwareModules)
            {
                proto.SoftwareModules.Add(ToProtobuf(swModule));
            }

            // 값 설정
            if (ioDevice.Value != null)
            {
                SetIODeviceValue(proto, ioDevice.Value);
            }

            return proto;
        }

        /// <summary>
        /// IODeviceProto를 IODevice로 변환
        /// </summary>
        public static IODevice FromProtobuf(this IODeviceProto proto)
        {
            if (proto == null) return null;

            var ioDevice = new IODevice
            {
                Uid = proto.Uid,
                Name = proto.Name,
                Description = proto.Description,
                ElementType = proto.ElementType,
                Supplier = proto.Supplier,
                Make = proto.Make,
                Model = proto.Model,
                ModelRevision = proto.ModelRevision,
                Function = proto.Function,
                ImmutableId = proto.ImmutableId
            };

            // 확장
            foreach (var extensionProto in proto.Extensions)
            {
                ioDevice.Extensions.Add(FromProtobuf(extensionProto));
            }

            // 논리적 요소
            foreach (var logicalElementProto in proto.LogicalElements)
            {
                ioDevice.LogicalElements.Add(FromProtobuf(logicalElementProto));
            }

            // 소프트웨어 모듈
            foreach (var swModuleProto in proto.SoftwareModules)
            {
                ioDevice.SoftwareModules.Add(FromProtobuf(swModuleProto));
            }

            // 값 가져오기
            ioDevice.Value = GetIODeviceValue(proto);

            return ioDevice;
        }

        /// <summary>
        /// MaterialLocation을 MaterialLocationProto로 변환
        /// </summary>
        public static MaterialLocationProto ToProtobuf(this MaterialLocation materialLocation)
        {
            if (materialLocation == null) return null;

            var proto = new MaterialLocationProto
            {
                Uid = materialLocation.Uid,
                Name = materialLocation.Name,
                Description = materialLocation.Description,
                MaterialType = ConvertMaterialType(materialLocation.MaterialTypeValue),
                MaterialSubType = materialLocation.MaterialSubType
            };

            // 확장
            foreach (var extension in materialLocation.Extensions)
            {
                proto.Extensions.Add(ToProtobuf(extension));
            }

            return proto;
        }

        /// <summary>
        /// MaterialLocationProto를 MaterialLocation으로 변환
        /// </summary>
        public static MaterialLocation FromProtobuf(this MaterialLocationProto proto)
        {
            if (proto == null) return null;

            var materialLocation = new MaterialLocation
            {
                Uid = proto.Uid,
                Name = proto.Name,
                Description = proto.Description,
                MaterialTypeValue = ConvertMaterialType(proto.MaterialType),
                MaterialSubType = proto.MaterialSubType
            };

            // 확장
            foreach (var extensionProto in proto.Extensions)
            {
                materialLocation.Extensions.Add(FromProtobuf(extensionProto));
            }

            return materialLocation;
        }

        /// <summary>
        /// LogicalElement를 LogicalElementProto로 변환
        /// </summary>
        public static LogicalElementProto ToProtobuf(this LogicalElement logicalElement)
        {
            if (logicalElement == null) return null;

            var proto = new LogicalElementProto
            {
                Uid = logicalElement.Uid,
                Name = logicalElement.Name,
                Description = logicalElement.Description,
                ElementType = logicalElement.ElementType
            };

            // 확장
            foreach (var extension in logicalElement.Extensions)
            {
                proto.Extensions.Add(ToProtobuf(extension));
            }

            return proto;
        }

        /// <summary>
        /// LogicalElementProto를 LogicalElement로 변환
        /// </summary>
        public static LogicalElement FromProtobuf(this LogicalElementProto proto)
        {
            if (proto == null) return null;

            var logicalElement = new LogicalElement
            {
                Uid = proto.Uid,
                Name = proto.Name,
                Description = proto.Description,
                ElementType = proto.ElementType
            };

            // 확장
            foreach (var extensionProto in proto.Extensions)
            {
                logicalElement.Extensions.Add(FromProtobuf(extensionProto));
            }

            return logicalElement;
        }

        /// <summary>
        /// SoftwareModule을 SoftwareModuleProto로 변환
        /// </summary>
        public static SoftwareModuleProto ToProtobuf(this SoftwareModule softwareModule)
        {
            if (softwareModule == null) return null;

            return new SoftwareModuleProto
            {
                Name = softwareModule.Name,
                Supplier = softwareModule.Supplier,
                Description = softwareModule.Description,
                Version = softwareModule.Version
            };
        }

        /// <summary>
        /// SoftwareModuleProto를 SoftwareModule로 변환
        /// </summary>
        public static SoftwareModule FromProtobuf(this SoftwareModuleProto proto)
        {
            if (proto == null) return null;

            return new SoftwareModule
            {
                Name = proto.Name,
                Supplier = proto.Supplier,
                Description = proto.Description,
                Version = proto.Version
            };
        }

        /// <summary>
        /// Extension을 ExtensionProto로 변환
        /// </summary>
        public static ExtensionProto ToProtobuf(this Extension extension)
        {
            if (extension == null) return null;

            return new ExtensionProto
            {
                ExtensionType = extension.GetType().Name,
                ExtensionData = extension.ToString() // 실제 구현에서는 적절한 직렬화 방법 사용
            };
        }

        /// <summary>
        /// ExtensionProto를 Extension으로 변환
        /// </summary>
        public static Extension FromProtobuf(this ExtensionProto proto)
        {
            if (proto == null) return null;

            // 실제 구현에서는 Extension 유형에 맞게 역직렬화 수행
            // 이 예제에서는 기본 Extension 클래스 반환
            return new Extension();
        }

        #region 헬퍼 메서드

        /// <summary>
        /// IODevice 값 설정
        /// </summary>
        private static void SetIODeviceValue(IODeviceProto proto, object value)
        {
            switch (value)
            {
                case bool boolValue:
                    proto.BoolValue = boolValue;
                    break;
                case double doubleValue:
                    proto.DoubleValue = doubleValue;
                    break;
                case float floatValue:
                    proto.FloatValue = floatValue;
                    break;
                case int intValue:
                    proto.Int32Value = intValue;
                    break;
                case long longValue:
                    proto.Int64Value = longValue;
                    break;
                case string stringValue:
                    proto.StringValue = stringValue;
                    break;
                default:
                    // 지원되지 않는 유형은 문자열로 변환
                    proto.StringValue = value?.ToString();
                    break;
            }
        }

        /// <summary>
        /// IODevice 값 가져오기
        /// </summary>
        private static object GetIODeviceValue(IODeviceProto proto)
        {
            switch (proto.ValueCase)
            {
                case IODeviceProto.ValueOneofCase.BoolValue:
                    return proto.BoolValue;
                case IODeviceProto.ValueOneofCase.DoubleValue:
                    return proto.DoubleValue;
                case IODeviceProto.ValueOneofCase.FloatValue:
                    return proto.FloatValue;
                case IODeviceProto.ValueOneofCase.Int32Value:
                    return proto.Int32Value;
                case IODeviceProto.ValueOneofCase.Int64Value:
                    return proto.Int64Value;
                case IODeviceProto.ValueOneofCase.StringValue:
                    return proto.StringValue;
                default:
                    return null;
            }
        }

        /// <summary>
        /// ProcessType 열거형 변환 (CEM -> Protobuf)
        /// </summary>
        private static ProcessTypeEnum ConvertProcessType(ProcessType processType)
        {
            switch (processType)
            {
                case ProcessType.Measurement:
                    return ProcessTypeEnum.ProcessTypeMeasurement;
                case ProcessType.Process:
                    return ProcessTypeEnum.ProcessTypeProcess;
                case ProcessType.Storage:
                    return ProcessTypeEnum.ProcessTypeStorage;
                case ProcessType.Transport:
                    return ProcessTypeEnum.ProcessTypeTransport;
                default:
                    return ProcessTypeEnum.ProcessTypeUnknown;
            }
        }

        /// <summary>
        /// ProcessType 열거형 변환 (Protobuf -> CEM)
        /// </summary>
        private static ProcessType ConvertProcessType(ProcessTypeEnum processType)
        {
            switch (processType)
            {
                case ProcessTypeEnum.ProcessTypeMeasurement:
                    return ProcessType.Measurement;
                case ProcessTypeEnum.ProcessTypeProcess:
                    return ProcessType.Process;
                case ProcessTypeEnum.ProcessTypeStorage:
                    return ProcessType.Storage;
                case ProcessTypeEnum.ProcessTypeTransport:
                    return ProcessType.Transport;
                default:
                    return ProcessType.Process; // 기본값
            }
        }

        /// <summary>
        /// MaterialType 열거형 변환 (CEM -> Protobuf)
        /// </summary>
        private static MaterialTypeEnum ConvertMaterialType(MaterialType materialType)
        {
            switch (materialType)
            {
                case MaterialType.Carrier:
                    return MaterialTypeEnum.MaterialTypeCarrier;
                case MaterialType.Substrate:
                    return MaterialTypeEnum.MaterialTypeSubstrate;
                case MaterialType.ProcessDurable:
                    return MaterialTypeEnum.MaterialTypeProcessDurable;
                case MaterialType.Consumable:
                    return MaterialTypeEnum.MaterialTypeConsumable;
                case MaterialType.Other:
                    return MaterialTypeEnum.MaterialTypeOther;
                default:
                    return MaterialTypeEnum.MaterialTypeUnknown;
            }
        }

        /// <summary>
        /// MaterialType 열거형 변환 (Protobuf -> CEM)
        /// </summary>
        private static MaterialType ConvertMaterialType(MaterialTypeEnum materialType)
        {
            switch (materialType)
            {
                case MaterialTypeEnum.MaterialTypeCarrier:
                    return MaterialType.Carrier;
                case MaterialTypeEnum.MaterialTypeSubstrate:
                    return MaterialType.Substrate;
                case MaterialTypeEnum.MaterialTypeProcessDurable:
                    return MaterialType.ProcessDurable;
                case MaterialTypeEnum.MaterialTypeConsumable:
                    return MaterialType.Consumable;
                case MaterialTypeEnum.MaterialTypeOther:
                    return MaterialType.Other;
                default:
                    return MaterialType.Other; // 기본값
            }
        }

        #endregion
    }
}