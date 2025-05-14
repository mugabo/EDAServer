using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using SemiE125.Core.DataCollection;
using SemiE125.Core.E120Integration;
using SemiE125.Tests;
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

        private readonly Dictionary<string, CoreModel.DataSourceDefinition> _dataSources =
            new Dictionary<string, CoreModel.DataSourceDefinition>();
        private readonly Dictionary<string, CoreModel.DataCollectionPipeline> _pipelines =
            new Dictionary<string, CoreModel.DataCollectionPipeline>();

        public DataCollectionServiceImpl()
        {
            _dataSourceRepository = new InMemoryDataSourceRepository();
            _samplingStrategy = new SemiE125.Core.DataCollection.DefaultSamplingStrategy();
            _compressionAlgorithm = new SemiE125.Core.DataCollection.NoCompressionAlgorithm();
        }

        // 데이터 소스 리포지토리만 받는 생성자
        public DataCollectionServiceImpl(IDataSourceRepository dataSourceRepository, CemIntegrationManager cemIntegrationManager = null)
        {
            _dataSourceRepository = dataSourceRepository ?? throw new ArgumentNullException(nameof(dataSourceRepository));
            _samplingStrategy = new SemiE125.Core.DataCollection.DefaultSamplingStrategy();
            _compressionAlgorithm = new SemiE125.Core.DataCollection.NoCompressionAlgorithm();
            _cemIntegrationManager = cemIntegrationManager;
        }

        public class InMemoryDataSourceRepository : IDataSourceRepository
        {
            private readonly Dictionary<string, DataSourceDefinition> _dataSources =
                new Dictionary<string, DataSourceDefinition>();

            public Task<DataSourceDefinition> GetByIdAsync(string id)
            {
                if (_dataSources.TryGetValue(id, out var dataSource))
                {
                    return Task.FromResult(dataSource);
                }

                return Task.FromResult<DataSourceDefinition>(null);
            }

            public Task<IEnumerable<DataSourceDefinition>> GetAllAsync()
            {
                return Task.FromResult<IEnumerable<DataSourceDefinition>>(_dataSources.Values);
            }

            public Task<string> AddAsync(DataSourceDefinition dataSource)
            {
                if (string.IsNullOrEmpty(dataSource.Uid))
                {
                    dataSource.Uid = Guid.NewGuid().ToString();
                }

                _dataSources[dataSource.Uid] = dataSource;
                return Task.FromResult(dataSource.Uid);
            }

            public Task<bool> UpdateAsync(DataSourceDefinition dataSource)
            {
                if (string.IsNullOrEmpty(dataSource.Uid) || !_dataSources.ContainsKey(dataSource.Uid))
                {
                    return Task.FromResult(false);
                }

                _dataSources[dataSource.Uid] = dataSource;
                return Task.FromResult(true);
            }

            public Task<bool> DeleteAsync(string id)
            {
                return Task.FromResult(_dataSources.Remove(id));
            }
        }

        // 모든 의존성을 받는 생성자
        public DataCollectionServiceImpl(
            IDataSourceRepository dataSourceRepository,
            ISamplingStrategy samplingStrategy,
            ICompressionAlgorithm compressionAlgorithm)
        {
            _dataSourceRepository = dataSourceRepository ?? throw new ArgumentNullException(nameof(dataSourceRepository));
            _samplingStrategy = samplingStrategy ?? throw new ArgumentNullException(nameof(samplingStrategy));
            _compressionAlgorithm = compressionAlgorithm ?? throw new ArgumentNullException(nameof(compressionAlgorithm));
        }

        public override Task<ProtoModel.RegisterDataSourceResponse> RegisterDataSource(
            ProtoModel.DataSourceDefinition request, ServerCallContext context)
        {
            try
            {
                var dataSource = new CoreModel.DataSourceDefinition
                {
                    Uid = string.IsNullOrEmpty(request.Uid) ? Guid.NewGuid().ToString() : request.Uid,
                    Name = request.Name,
                    Description = request.Description,
                    SourcePath = request.SourcePath,
                    SourceType = ConvertDataSourceType(request.SourceType),
                    DataType = request.DataType,
                    SamplingRate = request.SamplingRate,
                    IsEnabled = request.IsEnabled,
                    Priority = request.Priority
                };

                _dataSources[dataSource.Uid] = dataSource;

                return Task.FromResult(new ProtoModel.RegisterDataSourceResponse
                {
                    Success = true,
                    DataSourceUid = dataSource.Uid
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ProtoModel.RegisterDataSourceResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<ProtoModel.CollectDataResponse> CollectData(
            ProtoModel.CollectDataRequest request, ServerCallContext context)
        {
            try
            {
                if (!_dataSources.TryGetValue(request.DataSourceUid, out var dataSource))
                {
                    return Task.FromResult(new ProtoModel.CollectDataResponse
                    {
                        Success = false,
                        ErrorMessage = $"데이터 소스 UID를 찾을 수 없음: {request.DataSourceUid}"
                    });
                }

                // 실제 데이터 수집 로직 구현
                var data = new byte[100]; // 임시 데이터
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
                return Task.FromResult(new ProtoModel.CollectDataResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public override Task<ProtoModel.GetDataSourcesResponse> GetDataSources(
            ProtoModel.GetDataSourcesRequest request, ServerCallContext context)
        {
            try
            {
                var response = new ProtoModel.GetDataSourcesResponse
                {
                    Success = true
                };

                foreach (var dataSource in _dataSources.Values)
                {
                    response.DataSources.Add(ConvertToProto(dataSource));
                }

                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ProtoModel.GetDataSourcesResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
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

            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    foreach (var uid in dataSourceUids)
                    {
                        if (_dataSources.TryGetValue(uid, out var dataSource) && dataSource.IsEnabled)
                        {
                            // 실제 데이터 수집 로직 구현
                            var data = new byte[100]; // 임시 데이터
                            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                            await responseStream.WriteAsync(new ProtoModel.DataStreamResponse
                            {
                                DataSourceUid = uid,
                                Data = Google.Protobuf.ByteString.CopyFrom(data),
                                Timestamp = timestamp
                            });
                        }
                    }

                    await Task.Delay(sampleInterval);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"데이터 스트림 오류: {ex.Message}");
            }
        }

        // 데이터 소스 타입 변환 메서드
        private CoreModel.DataSourceType ConvertDataSourceType(ProtoModel.DataSourceType sourceType)
        {
            switch (sourceType)
            {
                case ProtoModel.DataSourceType.Opcua:
                    return CoreModel.DataSourceType.OpcUa;
                case ProtoModel.DataSourceType.DirectIo:
                    return CoreModel.DataSourceType.DirectIO;
                case ProtoModel.DataSourceType.File:
                    return CoreModel.DataSourceType.File;
                case ProtoModel.DataSourceType.Database:
                    return CoreModel.DataSourceType.Database;
                case ProtoModel.DataSourceType.Custom:
                    return CoreModel.DataSourceType.Custom;
                default:
                    return CoreModel.DataSourceType.Custom;
            }
        }

        // 프로토버프 타입으로 변환
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

        // CoreModel 객체를 ProtoModel 객체로 변환
        private ProtoModel.DataSourceDefinition ConvertToProto(CoreModel.DataSourceDefinition dataSource)
        {
            return new ProtoModel.DataSourceDefinition
            {
                Uid = dataSource.Uid,
                Name = dataSource.Name,
                Description = dataSource.Description,
                SourcePath = dataSource.SourcePath,
                SourceType = ConvertDataSourceType(dataSource.SourceType),
                DataType = dataSource.DataType,
                SamplingRate = dataSource.SamplingRate,
                IsEnabled = dataSource.IsEnabled,
                Priority = dataSource.Priority
            };
        }
    }
}