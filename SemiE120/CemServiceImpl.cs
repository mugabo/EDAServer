// CemServiceImpl.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using SemiE120.CEM;
using SemiE120.Converters;
using SemiE120.Protobuf;

namespace SemiE120.Services
{
    /// <summary>
    /// SEMI E120.2 CEM 서비스 구현
    /// </summary>
    public class CemServiceImpl : CemService.CemServiceBase
    {
        private readonly CemModelAdapter _cemAdapter;

        public CemServiceImpl(CemModelAdapter cemAdapter)
        {
            _cemAdapter = cemAdapter ?? throw new ArgumentNullException(nameof(cemAdapter));
        }

        /// <summary>
        /// 장비 정보 조회
        /// </summary>
        public override Task<EquipmentProto> GetEquipment(GetEquipmentRequest request, ServerCallContext context)
        {
            try
            {
                // 장비 ID로 장비 조회
                var equipment = _cemAdapter.GetEquipmentModel();

                // CEM -> Protobuf 변환
                var equipmentProto = equipment.ToProtobuf();

                return Task.FromResult(equipmentProto);
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(Grpc.Core.StatusCode.InvalidArgument, ex.Message));
            }
        }

        /// <summary>
        /// 특정 모듈 정보 조회
        /// </summary>
        public override Task<ModuleProto> GetModule(GetModuleRequest request, ServerCallContext context)
        {
            try
            {
                // 장비와 모듈 ID로 모듈 조회
                var equipment = _cemAdapter.GetEquipmentModel();

                // 모듈 찾기
                Module module = null;
                foreach (var m in equipment.Modules)
                {
                    if (m.Uid == request.ModuleId)
                    {
                        module = m;
                        break;
                    }
                }

                if (module == null)
                {
                    throw new KeyNotFoundException($"모듈 ID '{request.ModuleId}'를 찾을 수 없습니다.");
                }

                // CEM -> Protobuf 변환
                var moduleProto = module.ToProtobuf();

                return Task.FromResult(moduleProto);
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(Grpc.Core.StatusCode.InvalidArgument, ex.Message));
            }
        }

        /// <summary>
        /// IO 장치 값 읽기
        /// </summary>
        public override async Task<ReadIODeviceResponse> ReadIODeviceValue(ReadIODeviceRequest request, ServerCallContext context)
        {
            try
            {
                // 경로에서 IO 장치 찾기
                var cemPath = request.DevicePath;
                var valuePath = cemPath + ".Value";

                // 값 읽기
                var value = await _cemAdapter.GetValueAsync(valuePath);

                // IO 장치 프로토 생성
                var ioDeviceProto = new IODeviceProto
                {
                    Name = cemPath.Split('.').Last(),
                    Uid = Guid.NewGuid().ToString() // 실제 구현에서는 실제 UID 사용
                };

                // 값 설정
                if (value is bool boolValue)
                    ioDeviceProto.BoolValue = boolValue;
                else if (value is double doubleValue)
                    ioDeviceProto.DoubleValue = doubleValue;
                else if (value is float floatValue)
                    ioDeviceProto.FloatValue = floatValue;
                else if (value is int intValue)
                    ioDeviceProto.Int32Value = intValue;
                else if (value is long longValue)
                    ioDeviceProto.Int64Value = longValue;
                else if (value is string stringValue)
                    ioDeviceProto.StringValue = stringValue;
                else if (value != null)
                    ioDeviceProto.StringValue = value.ToString();

                return new ReadIODeviceResponse
                {
                    Success = true,
                    Device = ioDeviceProto
                };
            }
            catch (Exception ex)
            {
                return new ReadIODeviceResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// IO 장치 값 쓰기
        /// </summary>
        public override async Task<WriteIODeviceResponse> WriteIODeviceValue(WriteIODeviceRequest request, ServerCallContext context)
        {
            try
            {
                // 경로에서 IO 장치 찾기
                var cemPath = request.DevicePath;
                var valuePath = cemPath + ".Value";

                // 값 추출
                object value = null;
                switch (request.ValueCase)
                {
                    case WriteIODeviceRequest.ValueOneofCase.BoolValue:
                        value = request.BoolValue;
                        break;
                    case WriteIODeviceRequest.ValueOneofCase.DoubleValue:
                        value = request.DoubleValue;
                        break;
                    case WriteIODeviceRequest.ValueOneofCase.FloatValue:
                        value = request.FloatValue;
                        break;
                    case WriteIODeviceRequest.ValueOneofCase.Int32Value:
                        value = request.Int32Value;
                        break;
                    case WriteIODeviceRequest.ValueOneofCase.Int64Value:
                        value = request.Int64Value;
                        break;
                    case WriteIODeviceRequest.ValueOneofCase.StringValue:
                        value = request.StringValue;
                        break;
                }

                // 값 쓰기
                bool success = await _cemAdapter.SetValueAsync(valuePath, value);

                return new WriteIODeviceResponse
                {
                    Success = success,
                    ErrorMessage = success ? "" : "값 쓰기에 실패했습니다."
                };
            }
            catch (Exception ex)
            {
                return new WriteIODeviceResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}