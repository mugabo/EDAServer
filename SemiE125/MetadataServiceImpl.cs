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
                var metadata = _metadataManager.GetMetadata(request.EquipmentUid);

                if (metadata == null)
                {
                    return Task.FromResult(new EquipmentMetadataResponse
                    {
                        Success = false,
                        ErrorMessage = $"장비 UID '{request.EquipmentUid}'에 대한 메타데이터를 찾을 수 없습니다."
                    });
                }

                return Task.FromResult(new EquipmentMetadataResponse
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
                });
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

        public override Task<MetadataItemResponse> GetMetadataItem(
            GetMetadataItemRequest request, ServerCallContext context)
        {
            try
            {
                var item = _metadataManager.GetMetadataItem(request.EquipmentUid, request.ItemId);

                if (item == null)
                {
                    return Task.FromResult(new MetadataItemResponse
                    {
                        Success = false,
                        ErrorMessage = $"항목 ID '{request.ItemId}'에 대한 메타데이터를 찾을 수 없습니다."
                    });
                }

                return Task.FromResult(new MetadataItemResponse
                {
                    Success = true,
                    Item = ConvertToProto(item)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"메타데이터 항목 조회 중 오류: {ex.Message}");
                return Task.FromResult(new MetadataItemResponse
                {
                    Success = false,
                    ErrorMessage = $"메타데이터 항목 조회 오류: {ex.Message}"
                });
            }
        }

        public override Task<FindMetadataItemsResponse> FindMetadataItems(
            FindMetadataItemsRequest request, ServerCallContext context)
        {
            try
            {
                var metadata = _metadataManager.GetMetadata(request.EquipmentUid);

                if (metadata == null)
                {
                    return Task.FromResult(new FindMetadataItemsResponse
                    {
                        Success = false,
                        ErrorMessage = $"장비 UID '{request.EquipmentUid}'에 대한 메타데이터를 찾을 수 없습니다."
                    });
                }

                var response = new FindMetadataItemsResponse { Success = true };
                var filteredItems = metadata.Items
                    .Where(item => string.IsNullOrEmpty(request.ItemType) || item.ItemType == request.ItemType)
                    .Where(item => string.IsNullOrEmpty(request.NamePattern) ||
                                  item.ItemName.IndexOf(request.NamePattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                foreach (var item in filteredItems)
                {
                    response.Items.Add(ConvertToProto(item));
                }

                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"메타데이터 항목 검색 중 오류: {ex.Message}");
                return Task.FromResult(new FindMetadataItemsResponse
                {
                    Success = false,
                    ErrorMessage = $"메타데이터 항목 검색 오류: {ex.Message}"
                });
            }
        }

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

            foreach (var attr in item.Attributes)
            {
                proto.Attributes.Add(attr.Key, attr.Value);
            }

            return proto;
        }
    }
}