using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Opc.Ua;
using SemiE120.CEM;
using SemiE120.OpcUaIntegration;

namespace SemiE120Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("======================================================");
            Console.WriteLine("     SEMI E120 CEM 모델 어댑터 테스트 프로그램");
            Console.WriteLine("======================================================");

            try
            {
                // OPC UA 서버 URL 설정
                string serverUrl = "opc.tcp://localhost:48021";
                Console.Write($"OPC UA 서버 URL을 입력하세요 (기본값: {serverUrl}): ");
                string input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    serverUrl = input;
                }

                Console.WriteLine($"서버 {serverUrl}에 연결 중...");

                // CEM 모델 어댑터 생성 및 연결
                var cemAdapter = new CemModelAdapter(serverUrl);

                bool exit = false;
                while (!exit)
                {
                    Console.WriteLine("\n작업을 선택하세요:");
                    Console.WriteLine("1. CEM 장비 모델 구조 표시");
                    Console.WriteLine("2. 매핑된 태그 목록 표시");
                    Console.WriteLine("3. 태그 값 읽기");
                    Console.WriteLine("4. 태그 값 쓰기");
                    Console.WriteLine("5. 모든 태그 모니터링 시작");
                    Console.WriteLine("0. 종료");

                    Console.Write("\n선택: ");
                    string choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            DisplayEquipmentModel(cemAdapter.GetEquipmentModel());
                            break;

                        case "2":
                            DisplayTagMappings(cemAdapter.GetMappings());
                            break;

                        case "3":
                            await ReadTagValueAsync(cemAdapter);
                            break;

                        case "4":
                            await WriteTagValueAsync(cemAdapter);
                            break;

                        case "5":
                            await MonitorAllTagsAsync(cemAdapter);
                            break;

                        case "0":
                            exit = true;
                            break;

                        default:
                            Console.WriteLine("잘못된 선택입니다. 다시 시도하세요.");
                            break;
                    }
                }

                Console.WriteLine("프로그램을 종료합니다.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\n아무 키나 누르면 종료합니다...");
            Console.ReadKey();
        }

        static void DisplayEquipmentModel(Equipment equipment)
        {
            Console.WriteLine("\n=== 장비 모델 구조 ===");
            Console.WriteLine($"장비: {equipment.Name} ({equipment.Uid})");
            Console.WriteLine($"  설명: {equipment.Description}");
            Console.WriteLine($"  유형: {equipment.ElementType}");
            Console.WriteLine($"  공급자: {equipment.Supplier}");

            Console.WriteLine("\n모듈:");
            foreach (var module in equipment.Modules)
            {
                Console.WriteLine($"  - {module.Name} ({module.Uid})");

                Console.WriteLine("    IO 장치:");
                foreach (var ioDevice in module.IODevices)
                {
                    Console.WriteLine($"      - {ioDevice.Name} ({ioDevice.Uid})");
                }

                Console.WriteLine("    머티리얼 로케이션:");
                foreach (var matLoc in module.MaterialLocations)
                {
                    Console.WriteLine($"      - {matLoc.Name}: {matLoc.MaterialTypeValue} ({matLoc.MaterialSubType})");
                }
            }

            Console.WriteLine("\n서브시스템:");
            foreach (var subsystem in equipment.Subsystems)
            {
                Console.WriteLine($"  - {subsystem.Name} ({subsystem.Uid})");

                Console.WriteLine("    IO 장치:");
                foreach (var ioDevice in subsystem.IODevices)
                {
                    Console.WriteLine($"      - {ioDevice.Name} ({ioDevice.Uid})");
                }
            }
        }

        static void DisplayTagMappings(Dictionary<string, string> mappings)
        {
            Console.WriteLine("\n=== 매핑된 태그 목록 ===");

            if (mappings.Count == 0)
            {
                Console.WriteLine("매핑된 태그가 없습니다.");
                return;
            }

            Console.WriteLine("CEM 경로 <-> OPC UA 노드 ID");
            Console.WriteLine("----------------------------------");

            foreach (var mapping in mappings)
            {
                Console.WriteLine($"{mapping.Key} <-> {mapping.Value}");
            }
        }

        static async Task ReadTagValueAsync(CemModelAdapter cemAdapter)
        {
            Console.Write("\nCEM 경로를 입력하세요 (예: Equipment1.Modules.Chamber1.IODevices.PressureSensor.Value): ");
            string cemPath = Console.ReadLine();

            try
            {
                var value = await cemAdapter.GetValueAsync(cemPath);
                Console.WriteLine($"값: {value}");
                Console.WriteLine($"타입: {value?.GetType().Name ?? "null"}");
                Console.WriteLine($"상태: {value != null}");
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine($"오류: 경로 '{cemPath}'에 대한 매핑이 없습니다.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류: {ex.Message}");
            }
        }

        static async Task WriteTagValueAsync(CemModelAdapter cemAdapter)
        {
            Console.Write("\nCEM 경로를 입력하세요: ");
            string cemPath = Console.ReadLine();

            Console.Write("쓸 값을 입력하세요: ");
            string valueStr = Console.ReadLine();

            try
            {
                var dataType = cemAdapter.GetValueDataType(cemPath);
                object value = Convert.ChangeType(valueStr, dataType);

                bool success = await cemAdapter.SetValueAsync(cemPath, value);

                if (success)
                {
                    Console.WriteLine("값을 성공적으로 썼습니다.");
                }
                else
                {
                    Console.WriteLine("값 쓰기에 실패했습니다.");
                }
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine($"오류: 경로 '{cemPath}'에 대한 매핑이 없습니다.");
            }
            catch (FormatException)
            {
                Console.WriteLine("오류: 입력한 값이 올바른 형식이 아닙니다.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류: {ex.Message}");
            }
        }

        static async Task MonitorAllTagsAsync(CemModelAdapter cemAdapter)
        {
            Console.WriteLine("\n모든 태그 모니터링을 시작합니다. 중지하려면 아무 키나 누르세요...");

            // 모니터링 시작
            cemAdapter.StartMonitoring((path, value) => {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | {path}: {value}");
            });

            // 키 입력 대기
            Console.ReadKey();

            // 모니터링 중지
            cemAdapter.StopMonitoring();
            Console.WriteLine("모니터링이 중지되었습니다.");
        }
    }

    // 확장된 CemModelAdapter 클래스
    

    //// OPC UA 클라이언트를 위한 간단한 래퍼 클래스
    //// 실제 구현은 앞서 작성한 OpcUaClient 클래스를 사용하세요
    //public class OpcUaClient
    //{
    //    // 임시 구현 - 실제 OpcUaClient 클래스로 대체해야 합니다

    //    public async Task<bool> ConnectAsync(string endpointUrl)
    //    {
    //        // 실제 연결 로직은 앞서 구현한 OpcUaClient 클래스 사용
    //        Console.WriteLine($"OPC UA 서버 {endpointUrl}에 연결됨 (시뮬레이션)");
    //        return true;
    //    }

    //    public DataValue ReadNode(NodeId nodeId)
    //    {
    //        // 시뮬레이션 데이터
    //        if (nodeId.Identifier.ToString().Contains("Pick"))
    //        {
    //            return new DataValue(new Variant(123.45));
    //        }
    //        else if (nodeId.Identifier.ToString().Contains("Place"))
    //        {
    //            return new DataValue(new Variant(true));
    //        }
    //        else if (nodeId.Identifier.ToString().Contains("_commStatus"))
    //        {
    //            return new DataValue(new Variant((short)1));
    //        }

    //        return new DataValue(new Variant("Unknown tag"));
    //    }

    //    public uint WriteNode(NodeId nodeId, object value)
    //    {
    //        Console.WriteLine($"노드 {nodeId}에 값 {value} 쓰기 (시뮬레이션)");
    //        return StatusCodes.Good;
    //    }
    //}
}