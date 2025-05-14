// EquipmentMetadataManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SemiE120.CEM;
using SemiE125.Core.DataCollection;

namespace SemiE125.Core.Metadata
{
    /// <summary>
    /// SEMI E125 장비 메타데이터 관리 클래스
    /// </summary>
    public class EquipmentMetadataManager
    {
        private readonly ILogger<EquipmentMetadataManager> _logger;
        private readonly IDataSourceRepository _dataSourceRepository;
        private readonly Dictionary<string, EquipmentMetadata> _equipmentMetadataCache;

        public EquipmentMetadataManager(
            ILogger<EquipmentMetadataManager> logger,
            IDataSourceRepository dataSourceRepository)
        {
            _logger = logger;
            _dataSourceRepository = dataSourceRepository;
            _equipmentMetadataCache = new Dictionary<string, EquipmentMetadata>();
        }

        /// <summary>
        /// 장비 메타데이터 생성 또는 업데이트
        /// </summary>
        public async Task<EquipmentMetadata> CreateOrUpdateMetadataAsync(Equipment equipment)
        {
            try
            {
                _logger.LogInformation($"장비 '{equipment.Name}' 메타데이터 생성/업데이트 시작");

                // 기존 메타데이터 확인
                if (_equipmentMetadataCache.TryGetValue(equipment.Uid, out var existingMetadata))
                {
                    _logger.LogDebug($"장비 '{equipment.Name}'의 기존 메타데이터 업데이트");
                    UpdateMetadata(existingMetadata, equipment);
                    return existingMetadata;
                }

                // 새 메타데이터 생성
                var metadata = new EquipmentMetadata
                {
                    EquipmentUid = equipment.Uid,
                    EquipmentName = equipment.Name,
                    MetadataVersion = "1.0",
                    CreatedTimestamp = DateTime.UtcNow,
                    LastModifiedTimestamp = DateTime.UtcNow
                };

                // 장비 세부 정보 설정
                metadata.Attributes.Add("Supplier", equipment.Supplier);
                metadata.Attributes.Add("Model", equipment.Model);
                metadata.Attributes.Add("ElementType", equipment.ElementType);
                metadata.Attributes.Add("ProcessType", equipment.ProcessTypeValue.ToString());

                // 데이터 소스 정보 연결
                await LinkDataSourcesAsync(metadata, equipment);

                // 캐시에 저장
                _equipmentMetadataCache[equipment.Uid] = metadata;

                _logger.LogInformation($"장비 '{equipment.Name}' 메타데이터 생성 완료");
                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"장비 '{equipment.Name}' 메타데이터 생성/업데이트 실패");
                throw;
            }
        }

        /// <summary>
        /// 기존 메타데이터 업데이트
        /// </summary>
        private void UpdateMetadata(EquipmentMetadata metadata, Equipment equipment)
        {
            metadata.EquipmentName = equipment.Name;
            metadata.LastModifiedTimestamp = DateTime.UtcNow;

            // 속성 업데이트
            metadata.Attributes["Supplier"] = equipment.Supplier;
            metadata.Attributes["Model"] = equipment.Model;
            metadata.Attributes["ElementType"] = equipment.ElementType;
            metadata.Attributes["ProcessType"] = equipment.ProcessTypeValue.ToString();

            // 버전 증가
            var versionParts = metadata.MetadataVersion.Split('.');
            if (int.TryParse(versionParts[1], out int minorVersion))
            {
                metadata.MetadataVersion = $"{versionParts[0]}.{minorVersion + 1}";
            }
        }

        /// <summary>
        /// 데이터 소스 연결
        /// </summary>
        private async Task LinkDataSourcesAsync(EquipmentMetadata metadata, Equipment equipment)
        {
            try
            {
                // 장비의 데이터 소스 가져오기
                var dataSources = await _dataSourceRepository.GetAllAsync();
                var equipmentDataSources = dataSources.Where(ds => ds.EquipmentUid == equipment.Uid).ToList();

                // IO 장치에 대한 데이터 소스 연결
                foreach (var ioDevice in GetAllIODevices(equipment))
                {
                    var dataSource = equipmentDataSources.FirstOrDefault(ds => ds.SourcePath.EndsWith(ioDevice.Name));
                    if (dataSource != null)
                    {
                        var metadataItem = new MetadataItem
                        {
                            ItemId = ioDevice.Uid,
                            ItemName = ioDevice.Name,
                            ItemType = "IODevice",
                            Description = ioDevice.Description,
                            DataSourceUid = dataSource.Uid
                        };

                        // 데이터 형식 정보 추가
                        if (ioDevice.Value != null)
                        {
                            metadataItem.Attributes.Add("DataType", ioDevice.Value.GetType().Name);
                            metadataItem.Attributes.Add("EngineeringUnits", ioDevice.EngineeringUnits ?? "");
                        }

                        metadata.Items.Add(metadataItem);
                    }
                }

                _logger.LogInformation($"장비 '{equipment.Name}'에 {metadata.Items.Count}개의 메타데이터 항목 연결됨");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"데이터 소스 연결 중 오류 발생");
            }
        }

        /// <summary>
        /// 장비의 모든 IO 장치 목록 가져오기
        /// </summary>
        private List<IODevice> GetAllIODevices(Equipment equipment)
        {
            var devices = new List<IODevice>(equipment.IODevices);

            // 모듈 내의 IO 장치
            foreach (var module in equipment.Modules)
            {
                devices.AddRange(module.IODevices);

                // 중첩된 모듈 내의 IO 장치
                foreach (var nestedModule in module.Modules)
                {
                    devices.AddRange(nestedModule.IODevices);
                }
            }

            // 서브시스템 내의 IO 장치
            foreach (var subsystem in equipment.Subsystems)
            {
                devices.AddRange(subsystem.IODevices);

                // 중첩된 서브시스템 내의 IO 장치
                foreach (var nestedSubsystem in subsystem.Subsystems)
                {
                    devices.AddRange(nestedSubsystem.IODevices);
                }
            }

            return devices;
        }

        /// <summary>
        /// 장비 메타데이터 조회
        /// </summary>
        public EquipmentMetadata GetMetadata(string equipmentUid)
        {
            if (_equipmentMetadataCache.TryGetValue(equipmentUid, out var metadata))
            {
                return metadata;
            }

            return null;
        }

        /// <summary>
        /// 특정 메타데이터 항목 조회
        /// </summary>
        public MetadataItem GetMetadataItem(string equipmentUid, string itemId)
        {
            var metadata = GetMetadata(equipmentUid);
            if (metadata != null)
            {
                return metadata.Items.FirstOrDefault(item => item.ItemId == itemId);
            }

            return null;
        }

        /// <summary>
        /// 장비 메타데이터 삭제
        /// </summary>
        public bool RemoveMetadata(string equipmentUid)
        {
            return _equipmentMetadataCache.Remove(equipmentUid);
        }
    }

    /// <summary>
    /// 장비 메타데이터 클래스
    /// </summary>
    public class EquipmentMetadata
    {
        public string EquipmentUid { get; set; }
        public string EquipmentName { get; set; }
        public string MetadataVersion { get; set; }
        public DateTime CreatedTimestamp { get; set; }
        public DateTime LastModifiedTimestamp { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public List<MetadataItem> Items { get; set; } = new List<MetadataItem>();
    }

    /// <summary>
    /// 메타데이터 항목 클래스
    /// </summary>
    public class MetadataItem
    {
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; }
        public string Description { get; set; }
        public string DataSourceUid { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }
}