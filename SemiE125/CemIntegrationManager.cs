using SemiE120.CEM;
using SemiE125.Core.Metadata;
using SemiE125.Core.Communication;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace SemiE125.Core.E120Integration
{
    public class CemIntegrationManager
    {
        private readonly ILogger<CemIntegrationManager> _logger;
        private readonly CemModelAdapter _cemAdapter;
        private readonly EquipmentMetadataManager _metadataManager;
        private readonly string _configPath;

        public Equipment CurrentEquipment => _cemAdapter.GetEquipmentModel();

        public CemIntegrationManager(
            ILogger<CemIntegrationManager> logger,
            EquipmentMetadataManager metadataManager,
            string opcServerUrl = null,
            string configPath = null)
        {
            _logger = logger;
            _metadataManager = metadataManager;

            // 설정 파일 경로 결정
            _configPath = configPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "equipment_model.json");

            // OPC UA 서버 URL이 제공되지 않은 경우 기본값 사용
            string serverUrl = opcServerUrl ?? "opc.tcp://localhost:4840";

            // SemiE120의 CemModelAdapter 인스턴스 생성
            _cemAdapter = new CemModelAdapter(_configPath);

            _logger.LogInformation($"CEM 통합 매니저가 초기화되었습니다. 설정 파일: {_configPath}");
        }

        public async Task<EquipmentMetadata> UpdateEquipmentMetadataAsync()
        {
            try
            {
                // 현재 장비 모델 가져오기
                var equipment = _cemAdapter.GetEquipmentModel();
                if (equipment == null)
                {
                    _logger.LogWarning("장비 모델을 가져올 수 없습니다.");
                    return null;
                }

                // 장비 메타데이터 생성/업데이트
                var metadata = await _metadataManager.CreateOrUpdateMetadataAsync(equipment); 
                _logger.LogInformation($"장비 '{equipment.Name}'의 메타데이터가 업데이트되었습니다.");

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "장비 메타데이터 업데이트 중 오류가 발생했습니다.");
                throw;
            }
        }

        public async Task<object> GetTagValueAsync(string cemPath)
        {
            try
            {
                return await _cemAdapter.GetValueAsync(cemPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"태그 값 '{cemPath}' 읽기 중 오류가 발생했습니다.");
                throw;
            }
        }

        public async Task<bool> SetTagValueAsync(string cemPath, object value)
        {
            try
            {
                return await _cemAdapter.SetValueAsync(cemPath, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"태그 값 '{cemPath}' 쓰기 중 오류가 발생했습니다.");
                throw;
            }
        }

        public Dictionary<string, string> GetTagMappings()
        {
            return _cemAdapter.GetMappings();
        }

        public void StartMonitoring(Action<string, object> callback)
        {
            _cemAdapter.StartMonitoring(callback);
            _logger.LogInformation("장비 모니터링이 시작되었습니다.");
        }

        public void StopMonitoring()
        {
            _cemAdapter.StopMonitoring();
            _logger.LogInformation("장비 모니터링이 중지되었습니다.");
        }
    }
}