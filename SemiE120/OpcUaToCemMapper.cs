// OpcUaToCemMapper.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Opc.Ua;
using SemiE120.CEM;

namespace SemiE120.OpcUaIntegration
{
    public class OpcUaToCemMapper
    {
        private readonly OpcUaClient _client;
        private readonly Dictionary<string, NodeId> _cemUidToNodeIdMap;
        private readonly Dictionary<NodeId, string> _nodeIdToCemUidMap;

        public OpcUaToCemMapper(OpcUaClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _cemUidToNodeIdMap = new Dictionary<string, NodeId>();
            _nodeIdToCemUidMap = new Dictionary<NodeId, string>();
        }

        public async Task<Equipment> CreateEquipmentModelFromOpcUaAsync(string endpointUrl, NodeId rootNodeId)
        {
            if (!_client.IsConnected)
            {
                var connected = await _client.ConnectAsync(endpointUrl);
                if (!connected)
                {
                    throw new InvalidOperationException("OPC UA 서버에 연결할 수 없습니다.");
                }
            }

            // 장비 객체 생성
            var equipment = new Equipment();
            MapBaseProperties(rootNodeId, equipment);

            // 모듈, 서브시스템, 장치 등 매핑
            await MapModulesAsync(rootNodeId, equipment);
            await MapSubsystemsAsync(rootNodeId, equipment);
            await MapIODevicesAsync(rootNodeId, equipment);
            await MapMaterialLocationsAsync(rootNodeId, equipment);
            await MapSoftwareModulesAsync(rootNodeId, equipment);

            return equipment;
        }

        private void MapBaseProperties(NodeId nodeId, Nameable nameableObject)
        {
            try
            {
                // 기본 속성 매핑: Uid, Name, Description
                var nameResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.Name", nodeId.NamespaceIndex));
                if (nameResult != null && nameResult.Value != null)
                {
                    nameableObject.Name = nameResult.Value.ToString();
                }

                var uidResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.Uid", nodeId.NamespaceIndex));
                if (uidResult != null && uidResult.Value != null)
                {
                    nameableObject.Uid = uidResult.Value.ToString();
                    // 매핑 저장
                    _cemUidToNodeIdMap[nameableObject.Uid] = nodeId;
                    _nodeIdToCemUidMap[nodeId] = nameableObject.Uid;
                }

                var descResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.Description", nodeId.NamespaceIndex));
                if (descResult != null && descResult.Value != null)
                {
                    nameableObject.Description = descResult.Value.ToString();
                }

                // EquipmentElement 속성 매핑 (해당하는 경우)
                if (nameableObject is EquipmentElement equipmentElement)
                {
                    MapEquipmentElementProperties(nodeId, equipmentElement);
                }

                // ExecutionElement 속성 매핑 (해당하는 경우)
                if (nameableObject is ExecutionElement executionElement)
                {
                    MapExecutionElementProperties(nodeId, executionElement);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"기본 속성 매핑 오류: {ex.Message}");
            }
        }

        private void MapEquipmentElementProperties(NodeId nodeId, EquipmentElement element)
        {
            try
            {
                // EquipmentElement의 추가 속성 매핑
                var elementTypeResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.ElementType", nodeId.NamespaceIndex));
                if (elementTypeResult != null && elementTypeResult.Value != null)
                {
                    element.ElementType = elementTypeResult.Value.ToString();
                }

                var supplierResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.Supplier", nodeId.NamespaceIndex));
                if (supplierResult != null && supplierResult.Value != null)
                {
                    element.Supplier = supplierResult.Value.ToString();
                }

                // 추가 속성들도 유사하게 매핑...
                var makeResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.Make", nodeId.NamespaceIndex));
                if (makeResult != null && makeResult.Value != null)
                {
                    element.Make = makeResult.Value.ToString();
                }

                var modelResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.Model", nodeId.NamespaceIndex));
                if (modelResult != null && modelResult.Value != null)
                {
                    element.Model = modelResult.Value.ToString();
                }

                var modelRevisionResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.ModelRevision", nodeId.NamespaceIndex));
                if (modelRevisionResult != null && modelRevisionResult.Value != null)
                {
                    element.ModelRevision = modelRevisionResult.Value.ToString();
                }

                var functionResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.Function", nodeId.NamespaceIndex));
                if (functionResult != null && functionResult.Value != null)
                {
                    element.Function = functionResult.Value.ToString();
                }

                var immutableIdResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.ImmutableId", nodeId.NamespaceIndex));
                if (immutableIdResult != null && immutableIdResult.Value != null)
                {
                    element.ImmutableId = immutableIdResult.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EquipmentElement 속성 매핑 오류: {ex.Message}");
            }
        }

        private void MapExecutionElementProperties(NodeId nodeId, ExecutionElement element)
        {
            try
            {
                // ExecutionElement의 추가 속성 매핑
                var processNameResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.ProcessName", nodeId.NamespaceIndex));
                if (processNameResult != null && processNameResult.Value != null)
                {
                    element.ProcessName = processNameResult.Value.ToString();
                }

                var processTypeResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.ProcessType", nodeId.NamespaceIndex));
                if (processTypeResult != null && processTypeResult.Value != null)
                {
                    var processTypeStr = processTypeResult.Value.ToString();
                    if (Enum.TryParse<ProcessType>(processTypeStr, out var processType))
                    {
                        element.ProcessTypeValue = processType;
                    }
                }

                var recipeTypeResult = _client.ReadNode(new NodeId($"{nodeId.Identifier}.RecipeType", nodeId.NamespaceIndex));
                if (recipeTypeResult != null && recipeTypeResult.Value != null)
                {
                    element.RecipeType = recipeTypeResult.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ExecutionElement 속성 매핑 오류: {ex.Message}");
            }
        }

        private async Task MapModulesAsync(NodeId parentNodeId, Equipment equipment)
        {
            try
            {
                // 모듈 노드 찾기
                var references = _client.Browse(parentNodeId);
                foreach (var reference in references)
                {
                    if (reference.DisplayName.Text.Contains("Module") || reference.BrowseName.Name.Contains("Module"))
                    {
                        var moduleNodeId = new NodeId((NodeId)reference.NodeId);
                        var module = new Module();

                        // 기본 속성 매핑
                        MapBaseProperties(moduleNodeId, module);

                        // 서브컴포넌트 매핑
                        await MapIODevicesAsync(moduleNodeId, module);
                        await MapMaterialLocationsAsync(moduleNodeId, module);
                        // 중첩된 모듈 매핑은 필요에 따라 구현

                        equipment.Modules.Add(module);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"모듈 매핑 오류: {ex.Message}");
            }
        }

        private async Task MapSubsystemsAsync(NodeId parentNodeId, Equipment equipment)
        {
            try
            {
                // 서브시스템 노드 찾기
                var references = _client.Browse(parentNodeId);
                foreach (var reference in references)
                {
                    if (reference.DisplayName.Text.Contains("Subsystem") || reference.BrowseName.Name.Contains("Subsystem"))
                    {
                        var subsystemNodeId = new NodeId((NodeId)reference.NodeId);
                        var subsystem = new Subsystem();

                        // 기본 속성 매핑
                        MapBaseProperties(subsystemNodeId, subsystem);

                        // 서브컴포넌트 매핑
                        await MapIODevicesAsync(subsystemNodeId, subsystem);
                        await MapMaterialLocationsAsync(subsystemNodeId, subsystem);

                        equipment.Subsystems.Add(subsystem);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서브시스템 매핑 오류: {ex.Message}");
            }
        }

        private async Task MapIODevicesAsync<T>(NodeId parentNodeId, T parent) where T : EquipmentElement
        {
            try
            {
                // IO 장치 노드 찾기
                var references = _client.Browse(parentNodeId);
                foreach (var reference in references)
                {
                    if (reference.DisplayName.Text.Contains("IODevice") || reference.BrowseName.Name.Contains("IODevice"))
                    {
                        var ioDeviceNodeId = new NodeId((NodeId)reference.NodeId);
                        var ioDevice = new IODevice();

                        // 기본 속성 매핑
                        MapBaseProperties(ioDeviceNodeId, ioDevice);

                        // 부모 타입에 따라 추가
                        if (parent is Equipment equipment)
                        {
                            equipment.IODevices.Add(ioDevice);
                        }
                        else if (parent is Module module)
                        {
                            module.IODevices.Add(ioDevice);
                        }
                        else if (parent is Subsystem subsystem)
                        {
                            subsystem.IODevices.Add(ioDevice);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IO 장치 매핑 오류: {ex.Message}");
            }
        }

        private async Task MapMaterialLocationsAsync<T>(NodeId parentNodeId, T parent) where T : EquipmentElement
        {
            try
            {
                // 머티리얼 로케이션 노드 찾기
                var references = _client.Browse(parentNodeId);
                foreach (var reference in references)
                {
                    if (reference.DisplayName.Text.Contains("MaterialLocation") || reference.BrowseName.Name.Contains("MaterialLocation"))
                    {
                        var materialLocationNodeId = new NodeId((NodeId)reference.NodeId);
                        var materialLocation = new MaterialLocation();

                        // 기본 속성 매핑
                        MapBaseProperties(materialLocationNodeId, materialLocation);

                        // MaterialLocation 특정 속성 매핑
                        var materialTypeResult = _client.ReadNode(new NodeId($"{materialLocationNodeId.Identifier}.MaterialType",
                                                                           materialLocationNodeId.NamespaceIndex));
                        if (materialTypeResult != null && materialTypeResult.Value != null)
                        {
                            var materialTypeStr = materialTypeResult.Value.ToString();
                            if (Enum.TryParse<MaterialType>(materialTypeStr, out var materialType))
                            {
                                materialLocation.MaterialTypeValue = materialType;
                            }
                        }

                        var materialSubTypeResult = _client.ReadNode(new NodeId($"{materialLocationNodeId.Identifier}.MaterialSubType",
                                                                               materialLocationNodeId.NamespaceIndex));
                        if (materialSubTypeResult != null && materialSubTypeResult.Value != null)
                        {
                            materialLocation.MaterialSubType = materialSubTypeResult.Value.ToString();
                        }

                        // 부모 타입에 따라 추가
                        if (parent is Equipment equipment)
                        {
                            equipment.MaterialLocations.Add(materialLocation);
                        }
                        else if (parent is Module module)
                        {
                            module.MaterialLocations.Add(materialLocation);
                        }
                        else if (parent is Subsystem subsystem)
                        {
                            subsystem.MaterialLocations.Add(materialLocation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"머티리얼 로케이션 매핑 오류: {ex.Message}");
            }
        }

        private async Task MapSoftwareModulesAsync(NodeId parentNodeId, EquipmentElement parent)
        {
            try
            {
                // 소프트웨어 모듈 노드 찾기
                var references = _client.Browse(parentNodeId);
                foreach (var reference in references)
                {
                    if (reference.DisplayName.Text.Contains("SoftwareModule") || reference.BrowseName.Name.Contains("SoftwareModule"))
                    {
                        var swModuleNodeId = new NodeId((NodeId)reference.NodeId);
                        var swModule = new SoftwareModule();

                        // 소프트웨어 모듈 속성 매핑
                        var nameResult = _client.ReadNode(new NodeId($"{swModuleNodeId.Identifier}.Name", swModuleNodeId.NamespaceIndex));
                        if (nameResult != null && nameResult.Value != null)
                        {
                            swModule.Name = nameResult.Value.ToString();
                        }

                        var supplierResult = _client.ReadNode(new NodeId($"{swModuleNodeId.Identifier}.Supplier", swModuleNodeId.NamespaceIndex));
                        if (supplierResult != null && supplierResult.Value != null)
                        {
                            swModule.Supplier = supplierResult.Value.ToString();
                        }

                        var descResult = _client.ReadNode(new NodeId($"{swModuleNodeId.Identifier}.Description", swModuleNodeId.NamespaceIndex));
                        if (descResult != null && descResult.Value != null)
                        {
                            swModule.Description = descResult.Value.ToString();
                        }

                        var versionResult = _client.ReadNode(new NodeId($"{swModuleNodeId.Identifier}.Version", swModuleNodeId.NamespaceIndex));
                        if (versionResult != null && versionResult.Value != null)
                        {
                            swModule.Version = versionResult.Value.ToString();
                        }

                        // 부모에 추가
                        parent.SoftwareModules.Add(swModule);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"소프트웨어 모듈 매핑 오류: {ex.Message}");
            }
        }

        // OPC UA의 새 값을 CEM 모델에 적용하는 메서드
        public void UpdateCemModelFromOpcUa(Equipment equipment, NodeId changedNodeId, DataValue newValue)
        {
            if (!_nodeIdToCemUidMap.TryGetValue(changedNodeId, out var cemUid))
            {
                // 노드 ID에 해당하는 CEM 객체 없음
                return;
            }

            // 모든 Nameable 객체를 순회하여 해당 UID를 가진 객체 찾기
            UpdateNameableRecursive(equipment, cemUid, changedNodeId, newValue);
        }

        private bool UpdateNameableRecursive(Nameable nameable, string targetUid, NodeId changedNodeId, DataValue newValue)
        {
            // 현재 객체가 타겟인지 확인
            if (nameable.Uid == targetUid)
            {
                // 변경된 속성 이름 결정
                var propertyName = changedNodeId.Identifier.ToString().Split('.').LastOrDefault();
                if (string.IsNullOrEmpty(propertyName))
                    return true;

                // 속성별 업데이트 로직
                switch (propertyName.ToLower())
                {
                    case "name":
                        nameable.Name = newValue.Value.ToString();
                        return true;
                    case "description":
                        nameable.Description = newValue.Value.ToString();
                        return true;
                        // 다른 속성들에 대한 처리 추가...
                }

                // EquipmentElement 속성 처리
                if (nameable is EquipmentElement equipmentElement)
                {
                    switch (propertyName.ToLower())
                    {
                        case "elementtype":
                            equipmentElement.ElementType = newValue.Value.ToString();
                            return true;
                        case "supplier":
                            equipmentElement.Supplier = newValue.Value.ToString();
                            return true;
                            // 다른 속성들에 대한 처리 추가...
                    }

                    // ExecutionElement 속성 처리
                    if (nameable is ExecutionElement executionElement)
                    {
                        switch (propertyName.ToLower())
                        {
                            case "processname":
                                executionElement.ProcessName = newValue.Value.ToString();
                                return true;
                            case "processtype":
                                if (Enum.TryParse<ProcessType>(newValue.Value.ToString(), out var processType))
                                {
                                    executionElement.ProcessTypeValue = processType;
                                }
                                return true;
                            case "recipetype":
                                executionElement.RecipeType = newValue.Value.ToString();
                                return true;
                        }
                    }
                }

                // MaterialLocation 속성 처리
                if (nameable is MaterialLocation materialLocation)
                {
                    switch (propertyName.ToLower())
                    {
                        case "materialtype":
                            if (Enum.TryParse<MaterialType>(newValue.Value.ToString(), out var materialType))
                            {
                                materialLocation.MaterialTypeValue = materialType;
                            }
                            return true;
                        case "materialsubtype":
                            materialLocation.MaterialSubType = newValue.Value.ToString();
                            return true;
                    }
                }

                return true;
            }

            // 기본적으로 장비 계층 구조 탐색
            if (nameable is Equipment equipment)
            {
                foreach (var module in equipment.Modules)
                {
                    if (UpdateNameableRecursive(module, targetUid, changedNodeId, newValue))
                        return true;
                }

                foreach (var subsystem in equipment.Subsystems)
                {
                    if (UpdateNameableRecursive(subsystem, targetUid, changedNodeId, newValue))
                        return true;
                }

                foreach (var materialLocation in equipment.MaterialLocations)
                {
                    if (UpdateNameableRecursive(materialLocation, targetUid, changedNodeId, newValue))
                        return true;
                }
            }
            else if (nameable is Module module)
            {
                foreach (var childModule in module.Modules)
                {
                    if (UpdateNameableRecursive(childModule, targetUid, changedNodeId, newValue))
                        return true;
                }

                foreach (var subsystem in module.Subsystems)
                {
                    if (UpdateNameableRecursive(subsystem, targetUid, changedNodeId, newValue))
                        return true;
                }

                foreach (var materialLocation in module.MaterialLocations)
                {
                    if (UpdateNameableRecursive(materialLocation, targetUid, changedNodeId, newValue))
                        return true;
                }
            }
            else if (nameable is Subsystem subsystem)
            {
                foreach (var childSubsystem in subsystem.Subsystems)
                {
                    if (UpdateNameableRecursive(childSubsystem, targetUid, changedNodeId, newValue))
                        return true;
                }

                foreach (var materialLocation in subsystem.MaterialLocations)
                {
                    if (UpdateNameableRecursive(materialLocation, targetUid, changedNodeId, newValue))
                        return true;
                }
            }

            return false;
        }
    }
}