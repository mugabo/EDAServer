// IDataSourceRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemiE125.Core.DataCollection
{
    public interface IDataSourceRepository
    {
        Task<DataSourceDefinition> GetByIdAsync(string id);
        Task<IEnumerable<DataSourceDefinition>> GetAllAsync();
        Task<string> AddAsync(DataSourceDefinition dataSource);
        Task<bool> UpdateAsync(DataSourceDefinition dataSource);
        Task<bool> DeleteAsync(string id);
    }

    public class DataSourceRepository : IDataSourceRepository
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
}