// MetadataServiceImpl.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using SemiE125.Core.Metadata;
using SemiE125.Protobuf;

namespace SemiE125.Services
{
    public class MetadataServiceImpl : MetadataService.MetadataServiceBase
    {
        private readonly ILogger<MetadataServiceImpl> _logger;
        private readonly EquipmentMetadataManager _metadataManager;

        public MetadataServiceImpl(
            ILogger<MetadataServiceImpl> logger,
            EquipmentMetadataManager metadataManager)
        {
            _logger = logger;
            _metadataManager = metadataManager;
        }

        public override Task<EquipmentMetadataResponse> GetEquipmentMetadata(
    GetEquipmentMetadataRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"장비 메타데이터 요청 수신: EquipmentUid={request.EquipmentUid}");

                var metadata = _metadataManager.GetMetadata(request.EquipmentUid);

                if (metadata == null)
                {
                    _logger.LogWarning($"장비 UID '{request.EquipmentUid}'에 대한 메타데이터를 찾을 수 없습니다.");
                    return Task.FromResult(new EquipmentMetadataResponse
                    {
                        Success = false,
                        ErrorMessage = $"장비 UID '{request.EquipmentUid}'에 대한 메타데이터를 찾을 수 없습니다."
                    });
                }

                var response = new EquipmentMetadataResponse
                {
                    Success = true,
                    Metadata = new EquipmentMetadataProto
                    {
                        EquipmentUid = metadata.EquipmentUid,
                        EquipmentName = metadata.EquipmentName,
                        MetadataVersion = metadata.MetadataVersion,
                        CreatedTimestamp = metadata.CreatedTimestamp.Ticks,
                        LastModifiedTimestamp = metadata.LastModifiedTimestamp.Ticks
                    }
                };

                // 모든 속성 추가 - NULL 값 처리 추가
                foreach (var attr in metadata.Attributes)
                {
                    // null 값을 빈 문자열로 변환하여 추가
                    response.Metadata.Attributes.Add(attr.Key, attr.Value ?? string.Empty);
                }

                // 모든 메타데이터 항목 추가 (장비 구조 요소 표현)
                foreach (var item in metadata.Items)
                {
                    response.Metadata.Items.Add(ConvertToProto(item));
                }

                _logger.LogInformation($"장비 '{metadata.EquipmentName}'의 메타데이터 조회 성공: {metadata.Items.Count}개 항목, 버전 {metadata.MetadataVersion}");
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"장비 메타데이터 조회 중 오류: {ex.Message}");
                return Task.FromResult(new EquipmentMetadataResponse
                {
                    Success = false,
                    ErrorMessage = $"메타데이터 조회 오류: {ex.Message}"
                });
            }
        }

        // ConvertToProto 메서드도 null 값 처리 추가
        private MetadataItemProto ConvertToProto(MetadataItem item)
        {
            var proto = new MetadataItemProto
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                ItemType = item.ItemType,
                Description = item.Description ?? "",
                DataSourceUid = item.DataSourceUid ?? ""
            };

            // 이 항목의 모든 속성 추가 - NULL 값 처리
            foreach (var attr in item.Attributes)
            {
                proto.Attributes.Add(attr.Key, attr.Value ?? string.Empty);
            }

            return proto;
        }

        public override Task<GetUnitsResponse> GetUnits(GetUnitsRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"단위 메타데이터 요청 수신: EquipmentUid={request.EquipmentUid}");

                // 인증 확인 (실제 환경에서는 SEMI E132와 통합)
                // TODO: 적절한 E132 인증 확인 구현

                // 단위 메타데이터 조회
                var units = _metadataManager.GetUnits(request.EquipmentUid);

                // 응답 구성
                var response = new GetUnitsResponse
                {
                    Success = true
                };

                // 조회된 단위 정보를 응답에 추가
                foreach (var unit in units)
                {
                    var unitProto = new Protobuf.UnitDefinition
                    {
                        UnitId = unit.UnitId,
                        UnitName = unit.UnitName,
                        UnitSymbol = unit.UnitSymbol,
                        Description = unit.Description ?? ""
                    };

                    // 단위의 추가 속성 설정 (있는 경우)
                    foreach (var attr in unit.Attributes)
                    {
                        unitProto.Attributes.Add(attr.Key, attr.Value ?? "");
                    }

                    response.Units.Add(unitProto);
                }

                _logger.LogInformation($"단위 메타데이터 응답 전송: {response.Units.Count}개 단위 정의");
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"단위 메타데이터 조회 중 오류: {ex.Message}");
                return Task.FromResult(new GetUnitsResponse
                {
                    Success = false,
                    ErrorMessage = $"단위 메타데이터 조회 오류: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 장비의 타입 정의 메타데이터 조회
        /// </summary>
        public override Task<GetTypeDefinitionsResponse> GetTypeDefinitions(GetTypeDefinitionsRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"타입 정의 메타데이터 요청 수신: EquipmentUid={request.EquipmentUid}");

                // 인증 확인 (실제 환경에서는 SEMI E132와 통합)
                // TODO: 적절한 E132 인증 확인 구현

                // 타입 정의 메타데이터 조회
                var typeDefinitions = _metadataManager.GetTypeDefinitions(request.EquipmentUid);

                // 응답 구성
                var response = new GetTypeDefinitionsResponse
                {
                    Success = true
                };

                // 조회된 타입 정의 정보를 응답에 추가
                foreach (var typeDef in typeDefinitions)
                {
                    var typeDefProto = new Protobuf.TypeDefinition
                    {
                        TypeId = typeDef.TypeId,
                        TypeName = typeDef.TypeName,
                        BaseType = typeDef.BaseType ?? "",
                        Description = typeDef.Description ?? ""
                    };

                    // 타입의 속성 추가
                    foreach (var prop in typeDef.Properties)
                    {
                        var propProto = new Protobuf.TypeProperty
                        {
                            PropertyName = prop.PropertyName,
                            PropertyType = prop.PropertyType,
                            IsRequired = prop.IsRequired,
                            Description = prop.Description ?? ""
                        };

                        // 프로토 파일에 필드가 추가된 후에만 아래 코드가 작동합니다
                        if (prop.DefaultValue != null)
                            propProto.DefaultValue = prop.DefaultValue;

                        if (prop.UnitId != null)
                            propProto.UnitId = prop.UnitId;

                        typeDefProto.Properties.Add(propProto);
                    }

                    // 타입의 추가 속성 설정 (있는 경우)
                    foreach (var attr in typeDef.Attributes)
                    {
                        typeDefProto.Attributes.Add(attr.Key, attr.Value ?? "");
                    }

                    response.TypeDefinitions.Add(typeDefProto);
                }

                _logger.LogInformation($"타입 정의 메타데이터 응답 전송: {response.TypeDefinitions.Count}개 타입 정의");
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"타입 정의 메타데이터 조회 중 오류: {ex.Message}");
                return Task.FromResult(new GetTypeDefinitionsResponse
                {
                    Success = false,
                    ErrorMessage = $"타입 정의 메타데이터 조회 오류: {ex.Message}"
                });
            }
        }

        public override Task<GetStateMachinesResponse> GetStateMachines(GetStateMachinesRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"상태 머신 메타데이터 요청 수신: EquipmentUid={request.EquipmentUid}");

                // 인증 확인 (실제 환경에서는 SEMI E132와 통합)
                // TODO: 적절한 E132 인증 확인 구현

                // 상태 머신 메타데이터 조회
                var stateMachines = _metadataManager.GetStateMachines(request.EquipmentUid);

                // 응답 구성
                var response = new GetStateMachinesResponse
                {
                    Success = true
                };

                // 조회된 상태 머신 정보를 응답에 추가
                foreach (var stateMachine in stateMachines)
                {
                    var stateMachineProto = new Protobuf.StateMachineDefinition
                    {
                        StateMachineId = stateMachine.StateMachineId,
                        StateMachineName = stateMachine.StateMachineName,
                        Description = stateMachine.Description ?? ""
                    };

                    // 상태 추가
                    foreach (var state in stateMachine.States)
                    {
                        stateMachineProto.States.Add(new Protobuf.StateDefinition
                        {
                            StateId = state.StateId,
                            StateName = state.StateName,
                            Description = state.Description ?? "",
                            IsInitialState = state.IsInitialState
                        });
                    }

                    // 전이 추가
                    foreach (var transition in stateMachine.Transitions)
                    {
                        stateMachineProto.Transitions.Add(new Protobuf.TransitionDefinition
                        {
                            TransitionId = transition.TransitionId,
                            FromStateId = transition.FromStateId,
                            ToStateId = transition.ToStateId,
                            EventTrigger = transition.EventTrigger ?? "",
                            Description = transition.Description ?? ""
                        });
                    }

                    // 속성 추가 (있는 경우)
                    foreach (var attr in stateMachine.Attributes)
                    {
                        stateMachineProto.Attributes.Add(attr.Key, attr.Value ?? "");
                    }

                    response.StateMachines.Add(stateMachineProto);
                }

                _logger.LogInformation($"상태 머신 메타데이터 응답 전송: {response.StateMachines.Count}개 상태 머신 정의");
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"상태 머신 메타데이터 조회 중 오류: {ex.Message}");
                return Task.FromResult(new GetStateMachinesResponse
                {
                    Success = false,
                    ErrorMessage = $"상태 머신 메타데이터 조회 오류: {ex.Message}"
                });
            }
        }

        public override Task<GetSEMIObjTypesResponse> GetSEMIObjTypes(GetSEMIObjTypesRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"SEMI 표준 객체 타입 메타데이터 요청 수신: EquipmentUid={request.EquipmentUid}");

                // 인증 확인 (실제 환경에서는 SEMI E132와 통합)
                // TODO: 적절한 E132 인증 확인 구현

                // SEMI 표준 객체 타입 메타데이터 조회
                var objTypes = _metadataManager.GetSEMIObjTypes(request.EquipmentUid);

                // 응답 구성
                var response = new GetSEMIObjTypesResponse
                {
                    Success = true
                };

                // 조회된 객체 타입 정보를 응답에 추가
                foreach (var objType in objTypes)
                {
                    var objTypeProto = new Protobuf.SEMIObjTypeDefinition
                    {
                        ObjTypeId = objType.ObjTypeId,
                        ObjTypeName = objType.ObjTypeName,
                        StandardReference = objType.StandardReference,
                        Description = objType.Description ?? ""
                    };

                    // 속성 추가 (있는 경우)
                    foreach (var attr in objType.Attributes)
                    {
                        objTypeProto.Attributes.Add(attr.Key, attr.Value ?? "");
                    }

                    response.ObjTypes.Add(objTypeProto);
                }

                _logger.LogInformation($"SEMI 표준 객체 타입 메타데이터 응답 전송: {response.ObjTypes.Count}개 객체 타입 정의");
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SEMI 표준 객체 타입 메타데이터 조회 중 오류: {ex.Message}");
                return Task.FromResult(new GetSEMIObjTypesResponse
                {
                    Success = false,
                    ErrorMessage = $"SEMI 표준 객체 타입 메타데이터 조회 오류: {ex.Message}"
                });
            }
        }

        public override Task<GetExceptionsResponse> GetExceptions(GetExceptionsRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"예외 메타데이터 요청 수신: EquipmentUid={request.EquipmentUid}");

                // 인증 확인 (실제 환경에서는 SEMI E132와 통합)
                // TODO: 적절한 E132 인증 확인 구현

                // 예외 메타데이터 조회
                var exceptions = _metadataManager.GetExceptions(request.EquipmentUid);

                // 응답 구성
                var response = new GetExceptionsResponse
                {
                    Success = true
                };

                // 조회된 예외 정보를 응답에 추가
                foreach (var exception in exceptions)
                {
                    var exceptionProto = new Protobuf.ExceptionDefinition
                    {
                        ExceptionId = exception.ExceptionId,
                        ExceptionName = exception.ExceptionName,
                        ExceptionCode = exception.ExceptionCode,
                        Severity = exception.Severity,
                        Description = exception.Description ?? ""
                    };

                    // Category 필드가 proto 정의에 있다면 설정
                    if (exception.Category != null)
                    {
                        // 속성에 추가
                        exceptionProto.Attributes.Add("Category", exception.Category);
                    }

                    // 다른 속성 추가 (있는 경우)
                    foreach (var attr in exception.Attributes)
                    {
                        exceptionProto.Attributes.Add(attr.Key, attr.Value ?? "");
                    }

                    response.Exceptions.Add(exceptionProto);
                }

                _logger.LogInformation($"예외 메타데이터 응답 전송: {response.Exceptions.Count}개 예외 정의");
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"예외 메타데이터 조회 중 오류: {ex.Message}");
                return Task.FromResult(new GetExceptionsResponse
                {
                    Success = false,
                    ErrorMessage = $"예외 메타데이터 조회 오류: {ex.Message}"
                });
            }
        }

        public override Task<GetParametersResponse> GetParameters(GetParametersRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"매개변수 메타데이터 요청 수신: EquipmentUid={request.EquipmentUid}, NodeId={request.NodeId}");

                // 인증 확인 (실제 환경에서는 SEMI E132와 통합)
                // TODO: 적절한 E132 인증 확인 구현

                // 매개변수 메타데이터 조회
                var parameters = _metadataManager.GetParameters(request.EquipmentUid, request.NodeId);

                // 응답 구성
                var response = new GetParametersResponse
                {
                    Success = true
                };

                // 조회된 매개변수 정보를 응답에 추가
                foreach (var parameter in parameters)
                {
                    var parameterProto = new Protobuf.ParameterDefinition
                    {
                        ParameterId = parameter.ParameterId,
                        ParameterName = parameter.ParameterName,
                        DataType = parameter.DataType,
                        DefaultValue = parameter.DefaultValue ?? "",
                        Description = parameter.Description ?? "",
                        UnitId = parameter.UnitId ?? ""
                    };

                    // 최소값 및 최대값이 있는 경우 설정
                    if (!string.IsNullOrEmpty(parameter.MinValue))
                        parameterProto.MinValue = parameter.MinValue;

                    if (!string.IsNullOrEmpty(parameter.MaxValue))
                        parameterProto.MaxValue = parameter.MaxValue;

                    // 노드 ID 설정
                    if (!string.IsNullOrEmpty(parameter.NodeId))
                        parameterProto.NodeId = parameter.NodeId;

                    // 읽기 전용 여부 설정
                    parameterProto.ReadOnly = parameter.ReadOnly;

                    // 카테고리 및 기타 속성 추가
                    if (!string.IsNullOrEmpty(parameter.Category))
                        parameterProto.Attributes.Add("Category", parameter.Category);

                    // 다른 속성 추가 (있는 경우)
                    foreach (var attr in parameter.Attributes)
                    {
                        parameterProto.Attributes.Add(attr.Key, attr.Value ?? "");
                    }

                    response.Parameters.Add(parameterProto);
                }

                _logger.LogInformation($"매개변수 메타데이터 응답 전송: {response.Parameters.Count}개 매개변수 정의");
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"매개변수 메타데이터 조회 중 오류: {ex.Message}");
                return Task.FromResult(new GetParametersResponse
                {
                    Success = false,
                    ErrorMessage = $"매개변수 메타데이터 조회 오류: {ex.Message}"
                });
            }
        }

        public override Task<GetSimpleEventsResponse> GetSimpleEvents(GetSimpleEventsRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"간단한 이벤트 메타데이터 요청 수신: EquipmentUid={request.EquipmentUid}, NodeId={request.NodeId}");

                // 인증 확인 (실제 환경에서는 SEMI E132와 통합)
                // TODO: 적절한 E132 인증 확인 구현

                // 간단한 이벤트 메타데이터 조회
                var events = _metadataManager.GetSimpleEvents(request.EquipmentUid, request.NodeId);

                // 응답 구성
                var response = new GetSimpleEventsResponse
                {
                    Success = true
                };

                // 조회된 이벤트 정보를 응답에 추가
                foreach (var simpleEvent in events)
                {
                    var eventProto = new Protobuf.SimpleEventDefinition
                    {
                        EventId = simpleEvent.EventId,
                        EventName = simpleEvent.EventName,
                        EventType = simpleEvent.EventType,
                        Description = simpleEvent.Description ?? "",
                        NodeId = simpleEvent.NodeId ?? ""
                    };

                    // 다른 속성 추가 (있는 경우)
                    foreach (var attr in simpleEvent.Attributes)
                    {
                        eventProto.Attributes.Add(attr.Key, attr.Value ?? "");
                    }

                    response.Events.Add(eventProto);
                }

                _logger.LogInformation($"간단한 이벤트 메타데이터 응답 전송: {response.Events.Count}개 이벤트 정의");
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"간단한 이벤트 메타데이터 조회 중 오류: {ex.Message}");
                return Task.FromResult(new GetSimpleEventsResponse
                {
                    Success = false,
                    ErrorMessage = $"간단한 이벤트 메타데이터 조회 오류: {ex.Message}"
                });
            }
        }

        public override Task<GetEquipmentStructureResponse> GetEquipmentStructure(GetEquipmentStructureRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"장비 구조 메타데이터 요청 수신: EquipmentUid={request.EquipmentUid}");

                // 인증 확인 (실제 환경에서는 SEMI E132와 통합)
                // TODO: 적절한 E132 인증 확인 구현

                // 장비 구조 메타데이터 조회
                var equipmentStructure = _metadataManager.GetEquipmentStructure(request.EquipmentUid);

                if (equipmentStructure == null)
                {
                    return Task.FromResult(new GetEquipmentStructureResponse
                    {
                        Success = false,
                        ErrorMessage = $"장비 UID '{request.EquipmentUid}'에 대한 구조 메타데이터를 찾을 수 없습니다."
                    });
                }

                // 응답 구성
                var response = new GetEquipmentStructureResponse
                {
                    Success = true,
                    EquipmentStructure = new Protobuf.EquipmentStructure
                    {
                        EquipmentUid = equipmentStructure.EquipmentUid,
                        EquipmentName = equipmentStructure.EquipmentName,
                        EquipmentType = equipmentStructure.EquipmentType,
                        Description = equipmentStructure.Description ?? ""
                    }
                };

                // 속성 추가
                foreach (var attr in equipmentStructure.Attributes)
                {
                    response.EquipmentStructure.Attributes.Add(attr.Key, attr.Value ?? "");
                }

                // 모듈 노드 추가
                foreach (var module in equipmentStructure.Modules)
                {
                    response.EquipmentStructure.Modules.Add(ConvertToProtoNode(module));
                }

                // 서브시스템 노드 추가
                foreach (var subsystem in equipmentStructure.Subsystems)
                {
                    response.EquipmentStructure.Subsystems.Add(ConvertToProtoNode(subsystem));
                }

                // IO 장치 노드 추가
                foreach (var ioDevice in equipmentStructure.IODevices)
                {
                    response.EquipmentStructure.IoDevices.Add(ConvertToProtoNode(ioDevice));
                }

                _logger.LogInformation($"장비 구조 메타데이터 응답 전송: 모듈 {equipmentStructure.Modules.Count}개, 서브시스템 {equipmentStructure.Subsystems.Count}개");
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"장비 구조 메타데이터 조회 중 오류: {ex.Message}");
                return Task.FromResult(new GetEquipmentStructureResponse
                {
                    Success = false,
                    ErrorMessage = $"장비 구조 메타데이터 조회 오류: {ex.Message}"
                });
            }
        }

        public override Task<GetEquipmentNodeDescriptionsResponse> GetEquipmentNodeDescriptions(
    GetEquipmentNodeDescriptionsRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"노드 상세 설명 메타데이터 요청 수신: EquipmentUid={request.EquipmentUid}, " +
                                      $"NodeIds={string.Join(", ", request.NodeIds)}");

                // 인증 확인 (실제 환경에서는 SEMI E132와 통합)
                // TODO: 적절한 E132 인증 확인 구현

                // 노드 상세 설명 메타데이터 조회
                var (nodeDescriptions, unrecognizedNodeIds) =
                    _metadataManager.GetEquipmentNodeDescriptions(request.EquipmentUid, request.NodeIds.ToList());

                // 응답 구성
                var response = new Protobuf.GetEquipmentNodeDescriptionsResponse
                {
                    Success = true
                };

                // 조회된 노드 설명 정보를 응답에 추가
                foreach (var nodeDesc in nodeDescriptions)
                {
                    var nodeDescProto = new Protobuf.EquipmentNodeDescription
                    {
                        NodeId = nodeDesc.NodeId,
                        NodeName = nodeDesc.NodeName,
                        NodeType = nodeDesc.NodeType,
                        Description = nodeDesc.Description ?? ""
                    };

                    // 매개변수 ID 추가
                    foreach (var parameterId in nodeDesc.ParameterIds)
                    {
                        nodeDescProto.ParameterIds.Add(parameterId);
                    }

                    // 예외 ID 추가
                    foreach (var exceptionId in nodeDesc.ExceptionIds)
                    {
                        nodeDescProto.ExceptionIds.Add(exceptionId);
                    }

                    // 객체 타입 ID 추가
                    foreach (var objTypeId in nodeDesc.ObjTypeIds)
                    {
                        nodeDescProto.ObjTypeIds.Add(objTypeId);
                    }

                    // 상태 머신 ID 추가
                    foreach (var stateMachineId in nodeDesc.StateMachineIds)
                    {
                        nodeDescProto.StateMachineIds.Add(stateMachineId);
                    }

                    // 간단한 이벤트 ID 추가
                    foreach (var eventId in nodeDesc.SimpleEventIds)
                    {
                        nodeDescProto.SimpleEventIds.Add(eventId);
                    }

                    // 속성 추가
                    foreach (var attr in nodeDesc.Attributes)
                    {
                        nodeDescProto.Attributes.Add(attr.Key, attr.Value ?? "");
                    }

                    response.NodeDescriptions.Add(nodeDescProto);
                }

                // 인식되지 않은 노드 ID 추가
                foreach (var unrecognizedId in unrecognizedNodeIds)
                {
                    response.UnrecognizedNodeIds.Add(unrecognizedId);
                }

                _logger.LogInformation($"노드 상세 설명 메타데이터 응답 전송: {response.NodeDescriptions.Count}개 노드 정보, " +
                                      $"{response.UnrecognizedNodeIds.Count}개 인식되지 않음");
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"노드 상세 설명 메타데이터 조회 중 오류: {ex.Message}");
                return Task.FromResult(new Protobuf.GetEquipmentNodeDescriptionsResponse
                {
                    Success = false,
                    ErrorMessage = $"노드 상세 설명 메타데이터 조회 오류: {ex.Message}"
                });
            }
        }

        // EquipmentNode를 Protobuf.EquipmentNode로 변환하는 재귀 메서드
        private Protobuf.EquipmentNode ConvertToProtoNode(SemiE125.Core.Metadata.EquipmentNode node)
        {
            var protoNode = new Protobuf.EquipmentNode
            {
                NodeId = node.NodeId,
                NodeName = node.NodeName,
                NodeType = node.NodeType,
                Description = node.Description ?? ""
            };

            // 속성 추가
            foreach (var attr in node.Attributes)
            {
                protoNode.Attributes.Add(attr.Key, attr.Value ?? "");
            }

            // 자식 노드 추가 (재귀 호출)
            foreach (var child in node.Children)
            {
                protoNode.Children.Add(ConvertToProtoNode(child));
            }

            return protoNode;
        }
    }
}