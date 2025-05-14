using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SemiE125.Core.DataCollection;
using SemiE125.Core.E120Integration;
using Microsoft.Extensions.Logging;
using CoreModel = SemiE125.Core.DataCollection;
using ProtoModel = SemiE125.Protobuf;

namespace SemiE125.Services
{
    public class DataCollectionServiceImpl : ProtoModel.DataCollectionService.DataCollectionServiceBase
    {
        private readonly IDataSourceRepository _dataSourceRepository;
        private readonly ISamplingStrategy _samplingStrategy;
        private readonly ICompressionAlgorithm _compressionAlgorithm;
        private readonly CemIntegrationManager _cemIntegrationManager;
        private readonly ILogger<DataCollectionServiceImpl> _logger;
        private readonly string _equipmentModelPath;

        public DataCollectionServiceImpl(
            ILogger<DataCollectionServiceImpl> logger,
            IDataSourceRepository dataSourceRepository,
            CemIntegrationManager cemIntegrationManager = null)
        {
            _logger = logger;
            _dataSourceRepository = dataSourceRepository ?? throw new ArgumentNullException(nameof(dataSourceRepository));
            _samplingStrategy = new SemiE125.Core.DataCollection.DefaultSamplingStrategy();
            _compressionAlgorithm = new SemiE125.Core.DataCollection.NoCompressionAlgorithm();
            _cemIntegrationManager = cemIntegrationManager;

            // equipment_model.json 파일 경로 설정
            _equipmentModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "equipment_model.json");

            // 서비스 시작 시 equipment_model.json에서 데이터 소스 로드
            LoadDataSourcesFromEquipmentModel();
        }

        // equipment_model.json 파일에서 데이터 소스 로드
        private void LoadDataSourcesFromEquipmentModel()
        {
            try
            {
                _logger.LogInformation($"equipment_model.json에서 데이터 소스 로드 시작: {_equipmentModelPath}");

                if (!File.Exists(_equipmentModelPath))
                {
                    _logger.LogWarning($"equipment_model.json 파일을 찾을 수 없습니다: {_equipmentModelPath}");
                    return;
                }

                // JSON 파일 읽기
                string jsonContent = File.ReadAllText(_equipmentModelPath);
                var equipmentModel = JObject.Parse(jsonContent);

                // 장비 기본 정보
                var equipment = equipmentModel["equipment"];
                string equipmentUid = equipment["uid"].ToString();

                // 데이터 소스 목록 초기화
                List<DataSourceDefinition> dataSources = new List<DataSourceDefinition>();

                // 모듈에서 IO 장치 추출
                if (equipment["modules"] != null)
                {
                    foreach (var module in equipment["modules"])
                    {
                        string moduleId = module["uid"].ToString();

                        // 모듈의 IO 장치 처리
                        if (module["ioDevices"] != null)
                        {
                            foreach (var ioDevice in module["ioDevices"])
                            {
                                AddDataSourceFromIODevice(dataSources, ioDevice, moduleId, equipmentUid);
                            }
                        }
                    }
                }

                // 서브시스템에서 IO 장치 추출
                if (equipment["subsystems"] != null)
                {
                    foreach (var subsystem in equipment["subsystems"])
                    {
                        string subsystemId = subsystem["uid"].ToString();

                        // 서브시스템의 IO 장치 처리
                        if (subsystem["ioDevices"] != null)
                        {
                            foreach (var ioDevice in subsystem["ioDevices"])
                            {
                                AddDataSourceFromIODevice(dataSources, ioDevice, subsystemId, equipmentUid);
                            }
                        }
                    }
                }

                // 데이터 소스 저장소에 추가
                foreach (var dataSource in dataSources)
                {
                    _dataSourceRepository.AddAsync(dataSource).Wait();
                }

                _logger.LogInformation($"equipment_model.json에서 {dataSources.Count}개의 데이터 소스를 로드했습니다.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"equipment_model.json에서 데이터 소스 로드 중 오류 발생: {ex.Message}");
            }
        }

        // IO 장치에서 데이터 소스 생성
        private void AddDataSourceFromIODevice(List<DataSourceDefinition> dataSources, JToken ioDevice, string parentId, string equipmentUid)
        {
            string deviceId = ioDevice["uid"].ToString();
            string deviceName = ioDevice["name"].ToString();
            string deviceType = ioDevice["elementType"].ToString();
            string description = ioDevice["description"]?.ToString() ?? $"{deviceName} 데이터 소스";

            // 데이터 타입 추론
            string dataType = "double"; // 기본값
            if (deviceType.Contains("Sensor") || deviceType.Contains("Controller"))
            {
                if (deviceName.Contains("Pressure"))
                    dataType = "double";
                else if (deviceName.Contains("Temperature"))
                    dataType = "double";
                else if (deviceName.Contains("Flow") || deviceName.Contains("MFC"))
                    dataType = "double";
                else if (deviceName.Contains("Power"))
                    dataType = "double";
                else if (deviceName.Contains("Status") || deviceName.Contains("State"))
                    dataType = "string";
            }

            // 소스 타입 결정
            DataSourceType sourceType = DataSourceType.OpcUa; // 기본값
            if (deviceName.Contains("MFC") || deviceName.Contains("Controller"))
                sourceType = DataSourceType.OpcUa;
            else if (deviceType.Contains("Sensor"))
                sourceType = DataSourceType.OpcUa;
            else if (deviceType.Contains("Robot"))
                sourceType = DataSourceType.OpcUa;

            // 소스 경로 생성
            string sourcePath = $"ns=2;s={parentId}.{deviceId}";

            // 데이터 소스 정의 생성
            var dataSource = new DataSourceDefinition
            {
                Uid = Guid.NewGuid().ToString(),
                Name = deviceName,
                Description = description,
                SourceType = sourceType,
                SourcePath = sourcePath,
                DataType = dataType,
                SamplingRate = 1000, // 기본값: 1초
                IsEnabled = true,
                Priority = 1,
                EquipmentUid = equipmentUid
            };

            dataSources.Add(dataSource);
        }

        // RegisterDataSource 메서드 제거 (더 이상 필요 없음)

        public override Task<ProtoModel.CollectDataResponse> CollectData(
            ProtoModel.CollectDataRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"데이터 수집 요청: DataSourceUid={request.DataSourceUid}");

                var dataSourceTask = _dataSourceRepository.GetByIdAsync(request.DataSourceUid);
                dataSourceTask.Wait();
                var dataSource = dataSourceTask.Result;

                if (dataSource == null)
                {
                    return Task.FromResult(new ProtoModel.CollectDataResponse
                    {
                        Success = false,
                        ErrorMessage = $"데이터 소스 UID를 찾을 수 없음: {request.DataSourceUid}"
                    });
                }

                // 실제 데이터 수집 로직 구현 - 여기서는 임시 데이터 생성
                byte[] data;
                double value = 0;

                // 데이터 소스 유형에 따라 가상 데이터 생성
                if (dataSource.SourcePath.Contains("Pressure"))
                {
                    // 압력 센서 - 가상 데이터 생성 (0.001~0.1 Torr 사이의 임의 값)
                    value = new Random().NextDouble() * 0.099 + 0.001;
                    data = BitConverter.GetBytes(value);
                }
                else if (dataSource.SourcePath.Contains("Temperature"))
                {
                    // 온도 센서 - 가상 데이터 생성 (100~300℃ 사이의 임의 값)
                    value = new Random().NextDouble() * 200 + 100;
                    data = BitConverter.GetBytes(value);
                }
                else if (dataSource.SourcePath.Contains("MFC"))
                {
                    // 가스 유량 컨트롤러 - 가상 데이터 생성 (10~100 sccm 사이의 임의 값)
                    value = new Random().NextDouble() * 90 + 10;
                    data = BitConverter.GetBytes(value);
                }
                else if (dataSource.SourcePath.Contains("Power"))
                {
                    // 전원 공급 장치 - 가상 데이터 생성 (1000~5000W 사이의 임의 값)
                    value = new Random().NextDouble() * 4000 + 1000;
                    data = BitConverter.GetBytes(value);
                }
                else
                {
                    // 기타 센서 - 일반 임의 값 생성 (0~100 사이)
                    value = new Random().NextDouble() * 100;
                    data = BitConverter.GetBytes(value);
                }

                _logger.LogInformation($"데이터 수집 완료: DataSourceUid={request.DataSourceUid}, 값={value}");

                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                return Task.FromResult(new ProtoModel.CollectDataResponse
                {
                    Success = true,
                    Data = Google.Protobuf.ByteString.CopyFrom(data),
                    Timestamp = timestamp
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"데이터 수집 중 오류: {ex.Message}");
                return Task.FromResult(new ProtoModel.CollectDataResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override async Task<ProtoModel.GetDataSourcesResponse> GetDataSources(
            ProtoModel.GetDataSourcesRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"데이터 소스 목록 요청: EquipmentUid={request.EquipmentUid}");

                // 모든 데이터 소스 가져오기
                var dataSources = await _dataSourceRepository.GetAllAsync();

                // EquipmentUid로 필터링 (지정된 경우)
                if (!string.IsNullOrEmpty(request.EquipmentUid))
                {
                    dataSources = dataSources.Where(ds => ds.EquipmentUid == request.EquipmentUid).ToList();
                }

                var response = new ProtoModel.GetDataSourcesResponse
                {
                    Success = true
                };

                foreach (var dataSource in dataSources)
                {
                    response.DataSources.Add(ConvertToProto(dataSource));
                }

                _logger.LogInformation($"데이터 소스 목록 응답: {response.DataSources.Count}개 데이터 소스 반환");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"데이터 소스 목록 조회 중 오류: {ex.Message}");
                return new ProtoModel.GetDataSourcesResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public override async Task SubscribeToDataStream(
            ProtoModel.SubscribeToDataStreamRequest request,
            IServerStreamWriter<ProtoModel.DataStreamResponse> responseStream,
            ServerCallContext context)
        {
            var dataSourceUids = request.DataSourceUids;
            var sampleInterval = request.SampleIntervalMs;

            if (sampleInterval <= 0)
                sampleInterval = 1000; // 기본값 1초

            _logger.LogInformation($"데이터 스트림 구독 시작: DataSourceUids={string.Join(",", dataSourceUids)}, Interval={sampleInterval}ms");

            try
            {
                // 요청된 데이터 소스 로드
                var dataSourceTasks = dataSourceUids.Select(uid => _dataSourceRepository.GetByIdAsync(uid)).ToList();
                await Task.WhenAll(dataSourceTasks);

                var dataSources = dataSourceTasks.Select(task => task.Result).Where(ds => ds != null).ToList();

                if (dataSources.Count == 0)
                {
                    _logger.LogWarning("유효한 데이터 소스를 찾을 수 없습니다.");
                    return;
                }

                // 스트리밍 시작
                var random = new Random();
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    foreach (var dataSource in dataSources.Where(ds => ds.IsEnabled))
                    {
                        // 실제 데이터 수집 로직 대신 임시 데이터 생성
                        double value = 0;
                        byte[] data;

                        // 데이터 소스 유형에 따라 가상 데이터 생성
                        if (dataSource.SourcePath.Contains("Pressure"))
                        {
                            // 압력 센서 데이터 (0.001~0.1 Torr)
                            value = random.NextDouble() * 0.099 + 0.001;
                            data = BitConverter.GetBytes(value);
                        }
                        else if (dataSource.SourcePath.Contains("Temperature"))
                        {
                            // 온도 센서 데이터 (100~300℃)
                            value = random.NextDouble() * 200 + 100;
                            data = BitConverter.GetBytes(value);
                        }
                        else if (dataSource.SourcePath.Contains("MFC"))
                        {
                            // 가스 유량 데이터 (10~100 sccm)
                            value = random.NextDouble() * 90 + 10;
                            data = BitConverter.GetBytes(value);
                        }
                        else if (dataSource.SourcePath.Contains("Power"))
                        {
                            // 전력 데이터 (1000~5000W)
                            value = random.NextDouble() * 4000 + 1000;
                            data = BitConverter.GetBytes(value);
                        }
                        else
                        {
                            // 기타 센서 데이터 (0~100)
                            value = random.NextDouble() * 100;
                            data = BitConverter.GetBytes(value);
                        }

                        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        // 스트림으로 데이터 전송
                        await responseStream.WriteAsync(new ProtoModel.DataStreamResponse
                        {
                            DataSourceUid = dataSource.Uid,
                            Data = Google.Protobuf.ByteString.CopyFrom(data),
                            Timestamp = timestamp
                        });

                        _logger.LogDebug($"스트림 데이터 전송: DataSourceUid={dataSource.Uid}, 값={value}");
                    }

                    await Task.Delay(sampleInterval);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"데이터 스트림 제공 중 오류: {ex.Message}");
            }

            _logger.LogInformation("데이터 스트림 구독 종료");
        }

        // CoreModel.DataSourceDefinition을 ProtoModel.DataSourceDefinition으로 변환
        private ProtoModel.DataSourceDefinition ConvertToProto(CoreModel.DataSourceDefinition dataSource)
        {
            return new ProtoModel.DataSourceDefinition
            {
                Uid = dataSource.Uid,
                Name = dataSource.Name,
                Description = dataSource.Description ?? "",
                SourceType = ConvertDataSourceType(dataSource.SourceType),
                SourcePath = dataSource.SourcePath,
                DataType = dataSource.DataType,
                SamplingRate = dataSource.SamplingRate,
                IsEnabled = dataSource.IsEnabled,
                Priority = dataSource.Priority
            };
        }

        // 데이터 소스 타입 변환 메서드
        private ProtoModel.DataSourceType ConvertDataSourceType(CoreModel.DataSourceType sourceType)
        {
            switch (sourceType)
            {
                case CoreModel.DataSourceType.OpcUa:
                    return ProtoModel.DataSourceType.Opcua;
                case CoreModel.DataSourceType.DirectIO:
                    return ProtoModel.DataSourceType.DirectIo;
                case CoreModel.DataSourceType.File:
                    return ProtoModel.DataSourceType.File;
                case CoreModel.DataSourceType.Database:
                    return ProtoModel.DataSourceType.Database;
                case CoreModel.DataSourceType.Custom:
                    return ProtoModel.DataSourceType.Custom;
                default:
                    return ProtoModel.DataSourceType.Unknown;
            }
        }
    }
}