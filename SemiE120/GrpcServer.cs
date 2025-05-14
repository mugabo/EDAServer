// GrpcServer.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using SemiE120.CEM;
using SemiE120.Protobuf;
using SemiE120.Services;

namespace SemiE120.Server
{
    public class GrpcServer
    {
        private readonly Grpc.Core.Server _server;

        public GrpcServer(string host, int port, CemModelAdapter cemAdapter)
        {
            // gRPC 서비스 생성
            var cemService = new CemServiceImpl(cemAdapter);

            // 서버 생성
            _server = new Grpc.Core.Server
            {
                Services = { CemService.BindService(cemService) },
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };
        }

        public void Start()
        {
            _server.Start();

            // 첫 번째 포트 정보 가져오기
            var serverPort = _server.Ports.FirstOrDefault();
            if (serverPort != null)
            {
                Console.WriteLine($"gRPC 서버 시작: {serverPort.Host}:{serverPort.Port}");
            }
            else
            {
                Console.WriteLine("gRPC 서버 시작됨 (포트 정보 없음)");
            }
        }

        public async Task ShutdownAsync()
        {
            await _server.ShutdownAsync();
            Console.WriteLine("gRPC 서버 종료");
        }
    }
}