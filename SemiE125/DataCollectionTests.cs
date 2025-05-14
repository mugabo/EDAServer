// DataCollectionTests.cs
using System;
using System.Threading.Tasks;
using SemiE125.Core.DataCollection;
using SemiE125.Services;
using Xunit;

namespace SemiE125.Tests
{
    public class DataCollectionTests
    {
        [Fact]
        public void DataSourceDefinition_Properties_ShouldWork()
        {
            // Arrange
            var dataSource = new DataSourceDefinition
            {
                Uid = "DS001",
                Name = "TestDataSource",
                Description = "Test Data Source",
                SourcePath = "ns=2;s=Device1.Tag1",
                SourceType = DataSourceType.OpcUa,
                DataType = "Double",
                SamplingRate = 1000,
                IsEnabled = true,
                Priority = 1
            };

            // Assert
            Assert.Equal("DS001", dataSource.Uid);
            Assert.Equal("TestDataSource", dataSource.Name);
            Assert.Equal("Test Data Source", dataSource.Description);
            Assert.Equal("ns=2;s=Device1.Tag1", dataSource.SourcePath);
            Assert.Equal(DataSourceType.OpcUa, dataSource.SourceType);
            Assert.Equal("Double", dataSource.DataType);
            Assert.Equal(1000, dataSource.SamplingRate);
            Assert.True(dataSource.IsEnabled);
            Assert.Equal(1, dataSource.Priority);
        }

        [Fact]
        public async Task RegisterDataSource_ShouldSucceed()
        {
            // Arrange
            var service = new DataCollectionServiceImpl();
            var request = new Protobuf.DataSourceDefinition
            {
                Name = "TestDataSource",
                Description = "Test Data Source",
                SourcePath = "ns=2;s=Device1.Tag1",
                SourceType = Protobuf.DataSourceType.Opcua,
                DataType = "Double",
                SamplingRate = 1000,
                IsEnabled = true,
                Priority = 1
            };

            // Act
            var response = await service.RegisterDataSource(request, null);

            // Assert
            Assert.True(response.Success);
            Assert.False(string.IsNullOrEmpty(response.DataSourceUid));
        }

        [Fact]
        public void DataCollectionPipeline_Start_ShouldNotThrow()
        {
            // Arrange
            var dataSource = new DataSourceDefinition
            {
                Uid = "DS001",
                Name = "TestDataSource",
                SourcePath = "test/path",
                SourceType = DataSourceType.File,
                IsEnabled = true
            };

            var samplingStrategy = new DefaultSamplingStrategy();
            var compressionAlgorithm = new NoCompressionAlgorithm();

            var pipeline = new DataCollectionPipeline(
                new[] { dataSource },
                samplingStrategy,
                compressionAlgorithm);

            // Act & Assert
            var exception = Record.Exception(() => {
                pipeline.Start();
                Task.Delay(100).Wait(); // 약간의 실행 시간 허용
                pipeline.Stop();
            });

            Assert.Null(exception);
        }
    }

    // 테스트를 위한 기본 구현
    public class DefaultSamplingStrategy : ISamplingStrategy
    {
        public byte[] ApplySampling(byte[] rawData)
        {
            return rawData; // 샘플링 없음
        }
    }

    public class NoCompressionAlgorithm : ICompressionAlgorithm
    {
        public byte[] Compress(byte[] data)
        {
            return data; // 압축 없음
        }

        public byte[] Decompress(byte[] data)
        {
            return data; // 압축 해제 없음
        }
    }
}