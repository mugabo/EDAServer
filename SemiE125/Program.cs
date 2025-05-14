// Program.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Grpc.Core;
using SemiE125.Services;
using SemiE125.Core.Metadata;
using SemiE125.Core.DataCollection;
using static SemiE125.Services.DataCollectionServiceImpl;

namespace SemiE125
{
    class Program
    {
        static void Main(string[] args)
        {
            // 의존성 설정
            var dataSourceRepository = new InMemoryDataSourceRepository();
            var equipmentMetadataManager = new EquipmentMetadataManager(
                new SemiE125.Logging.ConsoleLogger<EquipmentMetadataManager>(
                    Microsoft.Extensions.Logging.LogLevel.Information),
                dataSourceRepository);

            var metadataService = new MetadataServiceImpl(
                new SemiE125.Logging.ConsoleLogger<MetadataServiceImpl>(
                    Microsoft.Extensions.Logging.LogLevel.Information),
                equipmentMetadataManager);

            var dataCollectionService = new DataCollectionServiceImpl(dataSourceRepository);

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
            Console.WriteLine("종료하려면 아무 키나 누르세요...");
            Console.ReadKey();

            // 서버 종료
            server.ShutdownAsync().Wait();
        }
    }
}