// E120Extensions.cs
using System;
using System.Collections.Generic;
using SemiE120.CEM;
using SemiE125.Core.DataCollection;

namespace SemiE125.Core.Extensions
{
    public static class E120Extensions
    {
        private static readonly Dictionary<string, List<DataSourceDefinition>> _equipmentDataSources =
            new Dictionary<string, List<DataSourceDefinition>>();

        /// <summary>
        /// 장비에 데이터 소스 추가
        /// </summary>
        public static void AddDataSource(this Equipment equipment, DataSourceDefinition dataSource)
        {
            if (!_equipmentDataSources.ContainsKey(equipment.Uid))
            {
                _equipmentDataSources[equipment.Uid] = new List<DataSourceDefinition>();
            }

            _equipmentDataSources[equipment.Uid].Add(dataSource);
        }

        /// <summary>
        /// 장비에서 데이터 소스 제거
        /// </summary>
        public static void RemoveDataSource(this Equipment equipment, string dataSourceUid)
        {
            if (_equipmentDataSources.TryGetValue(equipment.Uid, out var dataSources))
            {
                dataSources.RemoveAll(ds => ds.Uid == dataSourceUid);
            }
        }

        /// <summary>
        /// 장비의 모든 데이터 소스 가져오기
        /// </summary>
        public static IEnumerable<DataSourceDefinition> GetDataSources(this Equipment equipment)
        {
            if (_equipmentDataSources.TryGetValue(equipment.Uid, out var dataSources))
            {
                return dataSources;
            }

            return Array.Empty<DataSourceDefinition>();
        }

        /// <summary>
        /// 장비의 특정 데이터 소스 가져오기
        /// </summary>
        public static DataSourceDefinition GetDataSource(this Equipment equipment, string dataSourceUid)
        {
            if (_equipmentDataSources.TryGetValue(equipment.Uid, out var dataSources))
            {
                return dataSources.Find(ds => ds.Uid == dataSourceUid);
            }

            return null;
        }

        /// <summary>
        /// 장비의 특정 IODevice에 대한 데이터 소스 생성
        /// </summary>
        public static DataSourceDefinition CreateDataSourceForIODevice(
            this IODevice ioDevice,
            string sourcePath,
            int samplingRate = 1000,
            DataSourceType sourceType = DataSourceType.OpcUa)
        {
            var dataSource = new DataSourceDefinition
            {
                Uid = Guid.NewGuid().ToString(),
                Name = $"DS_{ioDevice.Name}",
                Description = $"자동 생성된 데이터 소스: {ioDevice.Description}",
                SourcePath = sourcePath,
                SourceType = sourceType,
                DataType = ioDevice.Value?.GetType().Name ?? "Unknown",
                SamplingRate = samplingRate,
                IsEnabled = true,
                Priority = 1
            };

            return dataSource;
        }
    }
}