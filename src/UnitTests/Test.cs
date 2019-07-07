using System;
using System.Collections.Generic;
using System.Linq;
using DeOlho.ETL.EFCore.Destinations;
using DeOlho.ETL.EFCore.Sources;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DeOlho.ETL.EFCore.UnitTests
{
    public class Test
    {

        public class MockData
        {
            public long Id { get; set; }
            public string Nome { get; set; }
            public DateTime? Data { get; set; }
        }

        private MockData mockData1 = new MockData
        {
            Id = 1,
            Nome = "Teste 1",
            Data = new DateTime(2000, 1, 1)
        };

        private MockData mockData2 = new MockData
        {
            Id = 2,
            Nome = "Teste 2",
            Data = new DateTime(2000, 1, 2)
        };

        private class MockDbContext : DbContext
        {
            public MockDbContext(DbContextOptions<MockDbContext> options)
                : base(options) 
            { }
            public DbSet<MockData> MockDatas { get; set; }
        }

        private MockDbContext mockDbContext;

        public Test()
        {
            DbContextOptions<MockDbContext> options;
            var builder = new DbContextOptionsBuilder<MockDbContext>();
            builder.UseInMemoryDatabase("Test");
            options = builder.Options;
            mockDbContext = new MockDbContext(options);
            mockDbContext.Database.EnsureDeleted();
            mockDbContext.Database.EnsureCreated();
            mockDbContext.MockDatas.Add(mockData1);
            mockDbContext.MockDatas.Add(mockData2);
            mockDbContext.SaveChanges();
        }            

        [Fact]
        public async void Source_DbContext_Sem_Where()
        {
            var process = new Process();
            var step  = process.Extract(() => new DbContextSource<MockData>(mockDbContext));
            var load = await step.Load();
            var result = load.Value.Result;
            result.Should().HaveCount(2);
            var item1 = result[0];
            item1.Id.Should().Be(mockData1.Id);
            item1.Nome.Should().Be(mockData1.Nome);
            item1.Data.Should().Be(mockData1.Data);
            var item2 = result[1];
            item2.Id.Should().Be(mockData2.Id);
            item2.Nome.Should().Be(mockData2.Nome);
            item2.Data.Should().Be(mockData2.Data);
        }

        [Fact]
        public async void Source_DbContext_Com_Where()
        {
            var process = new Process();
            var step  = process.Extract(
                () => new DbContextSource<MockData>(mockDbContext, _ => _.Id == 2));
            var load = await step.Load();
            var result = load.Value.Result;
            result.Should().HaveCount(1);
            var item2 = result[0];
            item2.Id.Should().Be(mockData2.Id);
            item2.Nome.Should().Be(mockData2.Nome);
            item2.Data.Should().Be(mockData2.Data);
        }

        [Fact]
        public async void Destination_DbContext()
        {
            var stepMock = new Mock<Step<MockData>>();
            stepMock.Setup(_ => _.Execute())
                .ReturnsAsync(new StepValue<MockData>(
                    new MockData
                    {
                        Id = 3,
                        Nome = "Teste 3",
                        Data = new DateTime(2000,1,3)
                    },
                    null)
                );

            var stepValue = await stepMock.Object.Load(() => new DbContextDestination(mockDbContext));

            var result = await mockDbContext.MockDatas.Where(_ => _.Id == 3).ToListAsync();
            result.Should().HaveCount(1);
            var item = result[0];
            item.Id.Should().Be(3);
            item.Nome.Should().Be("Teste 3");
            item.Data.Should().Be(new DateTime(2000,1,3));
        }

        [Fact]
        public async void Destination_Collection_DbContext()
        {
            var stepList  = new List<StepValue<MockData>> {
                new StepValue<MockData>(
                    new MockData
                    {
                        Id = 3,
                        Nome = "Teste 3",
                        Data = new DateTime(2000,1,3)
                    },
                    null
                ),
                new StepValue<MockData>(
                    new MockData
                    {
                        Id = 4,
                        Nome = "Teste 4",
                        Data = new DateTime(2000,1,4)
                    },
                    null
                )
            };

            var stepValue = await stepList.Load(() => new DbContextDestination(mockDbContext));

            mockDbContext.SaveChanges();

            var result = await mockDbContext.MockDatas.ToListAsync();
            result.Should().HaveCount(4);
            var item = result.LastOrDefault();
            item.Id.Should().Be(4);
            item.Nome.Should().Be("Teste 4");
            item.Data.Should().Be(new DateTime(2000,1,4));
        }

        [Fact]
        public async void Source_DbContext_SingleOrDefault()
        {
            var process = new Process();
            var step  = process.Extract(
                () => new DbContextSingleOrDefaultSource<MockData>(mockDbContext, 2L));
            var load = await step.Load();
            var result = load.Value;
            result.Id.Should().Be(mockData2.Id);
            result.Nome.Should().Be(mockData2.Nome);
            result.Data.Should().Be(mockData2.Data);
        }

    }
}