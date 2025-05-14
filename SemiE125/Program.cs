using SemiE125.Logging;
using SemiE125.Core.Metadata;
using SemiE125.Core.DataCollection;
using SemiE125.Core.E120Integration;
using SemiE125.Services;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using Grpc.Core;

class Program
{
    static void Main(string[] args)
    {
        // 로깅 설정
        var loggerProvider = new SemiE125LoggerProvider(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "semiE125.log"));

        // 의존성 설정
        var dataSourceRepository = new DataSourceRepository();

        var equipmentMetadataManager = new EquipmentMetadataManager(
            new ConsoleLogger<EquipmentMetadataManager>(
                Microsoft.Extensions.Logging.LogLevel.Information),
            dataSourceRepository);

        // CEM 통합 매니저 생성
        var cemIntegrationManager = new CemIntegrationManager(
            new ConsoleLogger<CemIntegrationManager>(
                Microsoft.Extensions.Logging.LogLevel.Information),
            equipmentMetadataManager,
            "opc.tcp://localhost:48021",  // OPC UA 서버 URL
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "equipment_model.json"));  // 설정 파일 경로

        // 장비 메타데이터 업데이트
        Console.WriteLine("장비 메타데이터 업데이트 중...");
        cemIntegrationManager.UpdateEquipmentMetadataAsync().Wait();
        Console.WriteLine("장비 메타데이터 업데이트 완료");

        // 서비스 구현 생성
        var metadataService = new MetadataServiceImpl(
            new ConsoleLogger<MetadataServiceImpl>(
                Microsoft.Extensions.Logging.LogLevel.Information),
            equipmentMetadataManager);

        var dataCollectionService = new DataCollectionServiceImpl(
            new ConsoleLogger<DataCollectionServiceImpl>(Microsoft.Extensions.Logging.LogLevel.Information), // 첫 번째: logger
            dataSourceRepository,  // 두 번째: dataSourceRepository
            cemIntegrationManager  // 세 번째: cemIntegrationManager (선택적)
        );

        // 서버 설정
        var server = new Server
        {
            Services =
            {
                // gRPC 서비스 등록
                SemiE125.Protobuf.MetadataService.BindService(metadataService),
                SemiE125.Protobuf.DataCollectionService.BindService(dataCollectionService)
            },
            Ports = { new ServerPort("localhost", 5001, ServerCredentials.Insecure) }
        };

        // 서버 시작
        server.Start();

        Console.WriteLine("gRPC 서버가 시작되었습니다. localhost:5001");
        Console.WriteLine("장비 메타데이터 ID: " + (cemIntegrationManager.CurrentEquipment?.Uid ?? "알 수 없음"));
        Console.WriteLine("종료하려면 아무 키나 누르세요...");
        Console.ReadKey();

        // 서버 종료
        server.ShutdownAsync().Wait();
    }
}