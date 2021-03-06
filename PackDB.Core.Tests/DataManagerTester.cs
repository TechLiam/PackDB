﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using PackDB.Core.Auditing;
using PackDB.Core.Data;
using PackDB.Core.Indexing;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace PackDB.Core.Tests
{
    [TestFixture]
    [TestOf(typeof(DataManager))]
    [ExcludeFromCodeCoverage]
    public class DataManagerTester
    {
        [SetUp]
        public void Setup()
        {
            Randomizer = Randomizer.CreateRandomizer();

            ExpectedBasicEntity1 = new BasicEntity {Id = Randomizer.Next()};
            ExpectedBasicEntity2 = new BasicEntity {Id = Randomizer.Next()};
            ExpectedIndexedEntity = new IndexedEntity {Id = Randomizer.Next(), IndexedValue = Randomizer.GetString()};
            ExpectedAuditedEntity = new AuditedEntity {Id = Randomizer.Next()};
            ExpectedAuditedEntityAuditLog = new AuditLog();
            ExpectedIndexKey = new IndexKey<string>() { Value = Randomizer.GetString() };

            MockDataStream = new Mock<IDataWorker>();
            MockDataStream
                .Setup(x => x.Exists<BasicEntity>(ExpectedBasicEntity1.Id))
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Read<BasicEntity>(ExpectedBasicEntity1.Id))
                .ReturnsAsync(ExpectedBasicEntity1);
            MockDataStream
                .Setup(x => x.Exists<BasicEntity>(ExpectedIndexedEntity.Id))
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Exists<IndexedEntity>(ExpectedIndexedEntity.Id))
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Read<BasicEntity>(ExpectedIndexedEntity.Id))
                .ReturnsAsync(ExpectedIndexedEntity);
            MockDataStream
                .Setup(x => x.Read<IndexedEntity>(ExpectedIndexedEntity.Id))
                .ReturnsAsync(ExpectedIndexedEntity);
            MockDataStream
                .Setup(x => x.Exists<BasicEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Read<BasicEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(ExpectedAuditedEntity);
            MockDataStream
                .Setup(x => x.Read<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(ExpectedAuditedEntity);
            MockDataStream
                .Setup(x => x.WriteAndCommit(ExpectedBasicEntity1.Id, ExpectedBasicEntity1))
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.WriteAndCommit(ExpectedIndexedEntity.Id, ExpectedIndexedEntity))
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Write(ExpectedAuditedEntity.Id, ExpectedAuditedEntity))
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Commit<BasicEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Delete<BasicEntity>(ExpectedBasicEntity1.Id))
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Delete<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(true);
            ExpectedNextBasicEntityId = Randomizer.Next();
            MockDataStream
                .Setup(x => x.NextId<BasicEntity>())
                .Returns(ExpectedNextBasicEntityId);
            MockDataStream
                .Setup(x => x.ReadAll<BasicEntity>())
                .Returns(ExpectedBasicEntityList);

            MockIndexer = new Mock<IIndexWorker>();
            MockIndexer
                .Setup(x => x.IndexExist<IndexedEntity>("IndexedValue"))
                .ReturnsAsync(true);
            MockIndexer
                .Setup(
                    x => x.GetIdsFromIndex<IndexedEntity, string>("IndexedValue", ExpectedIndexedEntity.IndexedValue))
                .Returns(ExpectedIndexEntityList);
            MockIndexer
                .Setup(
                    x => x.GetKeysFromIndex<IndexedEntity, string>("IndexedValue"))
                .Returns(ExpectedIndexKeysEntityList);
            MockIndexer
                .Setup(x => x.Index(ExpectedBasicEntity1))
                .ReturnsAsync(true);
            MockIndexer
                .Setup(x => x.Index(ExpectedIndexedEntity))
                .ReturnsAsync(true);
            MockIndexer
                .Setup(x => x.Index(ExpectedAuditedEntity))
                .ReturnsAsync(true);
            MockIndexer
                .Setup(x => x.Unindex(ExpectedBasicEntity1))
                .ReturnsAsync(true);
            MockIndexer
                .Setup(x => x.Unindex(ExpectedAuditedEntity))
                .ReturnsAsync(true);

            MockAudit = new Mock<IAuditWorker>();
            MockAudit
                .Setup(x => x.CreationEvent(ExpectedAuditedEntity))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.UpdateEvent(ExpectedAuditedEntity, ExpectedAuditedEntity))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.DeleteEvent(ExpectedAuditedEntity))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.ReadAllEvents<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(ExpectedAuditedEntityAuditLog);

            MockLogger = new Mock<ILogger>();

            DataManager = new DataManager(MockDataStream.Object, MockIndexer.Object, MockAudit.Object,
                MockLogger.Object);
        }

        private async IAsyncEnumerable<BasicEntity> ExpectedBasicEntityList()
        {
            yield return await Task.FromResult(ExpectedBasicEntity1);
            yield return await Task.FromResult(ExpectedBasicEntity2);
        }

        private async IAsyncEnumerable<int> ExpectedIndexEntityList()
        {
            yield return await Task.FromResult(ExpectedIndexedEntity.Id);
        }

        private async IAsyncEnumerable<int> EmptyIndexEntityList()
        {
            await Task.CompletedTask;
            yield break;
        }

        private async IAsyncEnumerable<int> RandomIndexEntityList()
        {
            yield return await Task.FromResult(Randomizer.Next());
        }

        private async IAsyncEnumerable<IndexKey<string>> EmptyIndexKeysEntityList()
        {
            await Task.CompletedTask;
            yield break;
        }

        private async IAsyncEnumerable<IndexKey<string>> RandomIndexKeysEntityList()
        {
            yield return await Task.FromResult(new IndexKey<string>(){ Value = Randomizer.GetString() });
        }

        private async IAsyncEnumerable<IndexKey<string>> ExpectedIndexKeysEntityList()
        {
            yield return await Task.FromResult(ExpectedIndexKey);
        }

        private DataManager DataManager { get; set; }
        private static BasicEntity ExpectedBasicEntity1 { get; set; }
        private static BasicEntity ExpectedBasicEntity2 { get; set; }
        private static IndexedEntity ExpectedIndexedEntity { get; set; }
        private static AuditedEntity ExpectedAuditedEntity { get; set; }
        private static AuditLog ExpectedAuditedEntityAuditLog { get; set; }
        private static int ExpectedNextBasicEntityId { get; set; }
        private static IndexKey<string> ExpectedIndexKey { get; set; }
        private Randomizer Randomizer { get; set; }
        private Mock<IDataWorker> MockDataStream { get; set; }
        private Mock<IIndexWorker> MockIndexer { get; set; }
        private Mock<IAuditWorker> MockAudit { get; set; }
        private Mock<ILogger> MockLogger { get; set; }

        [Test(Author = "PackDB Creator")]
        public async Task ReadWhenThereNoData()
        {
            Assert.IsNull(await DataManager.Read<BasicEntity>(Randomizer.Next()));
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadWhenThereIsData()
        {
            Assert.AreSame(ExpectedBasicEntity1, await DataManager.Read<BasicEntity>(ExpectedBasicEntity1.Id));
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadMultipleWhenThereNoData()
        {
            var results = DataManager.Read<BasicEntity>(new[] {Randomizer.Next(), Randomizer.Next()});
            await foreach (var result in results) Assert.IsFalse(result != null);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadMultipleWhenThereIsData()
        {
            var data = DataManager.Read<BasicEntity>(new[] {ExpectedBasicEntity1.Id, ExpectedIndexedEntity.Id});
            var results = new List<BasicEntity>();
            await foreach (var d in data) results.Add(d);
            Assert.AreEqual(2, results.Count());
            Assert.AreSame(ExpectedBasicEntity1, results.ElementAt(0));
            Assert.AreSame(ExpectedIndexedEntity, results.ElementAt(1));
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadAll()
        {
            var data = DataManager.ReadAll<BasicEntity>();
            var results = new List<BasicEntity>();
            await foreach (var d in data) results.Add(d);
            Assert.AreEqual(2, results.Count());
            Assert.AreSame(ExpectedBasicEntity1, results.ElementAt(0));
            Assert.AreSame(ExpectedBasicEntity2, results.ElementAt(1));
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadWhenIndexPropertyIsNotIndexed()
        {
            var data = DataManager.ReadIndex<BasicEntity, string>(ExpectedBasicEntity1.Value1, x => x.Value1);
            var result = new List<BasicEntity>();
            await foreach (var d in data) result.Add(d);
            Assert.IsEmpty(result);
            MockIndexer.Verify(x => x.IndexExist<BasicEntity>(It.IsAny<string>()), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadWhenIndexDoesNotExist()
        {
            MockIndexer.Setup(x => x.IndexExist<IndexedEntity>("IndexedValue")).ReturnsAsync(false);
            var data = DataManager.ReadIndex<IndexedEntity, string>(ExpectedBasicEntity1.Value1, x => x.IndexedValue);
            var result = new List<BasicEntity>();
            await foreach (var d in data) result.Add(d);
            Assert.IsEmpty(result);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadWhenIndexHasNoValue()
        {
            MockIndexer
                .Setup(
                    x => x.GetIdsFromIndex<IndexedEntity, string>("IndexedValue", ExpectedIndexedEntity.IndexedValue))
                .Returns(EmptyIndexEntityList);
            var data = DataManager.ReadIndex<IndexedEntity, string>(ExpectedIndexedEntity.IndexedValue,
                x => x.IndexedValue);
            var results = new List<IndexedEntity>();
            await foreach (var d in data) results.Add(d);
            Assert.IsFalse(results.Any());
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadWhenIndexHasAValueButThereNoData()
        {
            MockIndexer
                .Setup(
                    x => x.GetIdsFromIndex<IndexedEntity, string>("IndexedValue", ExpectedIndexedEntity.IndexedValue))
                .Returns(RandomIndexEntityList);
            var data = DataManager.ReadIndex<IndexedEntity, string>(ExpectedIndexedEntity.IndexedValue,
                x => x.IndexedValue);
            var results = new List<IndexedEntity>();
            await foreach (var d in data) results.Add(d);
            Assert.IsFalse(results.Any(x => x != null));
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadWhenIndexHasAValueAndThereIsData()
        {
            var data = DataManager.ReadIndex<IndexedEntity, string>(ExpectedIndexedEntity.IndexedValue,
                x => x.IndexedValue);
            var result = new List<IndexedEntity>();
            await foreach (var d in data) result.Add(d);
            Assert.AreEqual(result.Count(), 1);
            Assert.AreSame(ExpectedIndexedEntity, result.ElementAt(0));
        }
        
        [Test(Author = "PackDB Creator")]
        public async Task ReadKeysWhenIndexPropertyIsNotIndexed()
        {
            var data = DataManager.ReadIndexKeys<BasicEntity, string>(x => x.Value1);
            var result = new List<IndexKey<string>>();
            await foreach (var d in data) result.Add(d);
            Assert.IsEmpty(result);
            MockIndexer.Verify(x => x.IndexExist<BasicEntity>(It.IsAny<string>()), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadKeysWhenIndexDoesNotExist()
        {
            MockIndexer.Setup(x => x.IndexExist<IndexedEntity>("IndexedValue")).ReturnsAsync(false);
            var data = DataManager.ReadIndexKeys<IndexedEntity, string>(x => x.IndexedValue);
            var result = new List<IndexKey<string>>();
            await foreach (var d in data) result.Add(d);
            Assert.IsEmpty(result);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadKeysWhenIndexHasNoValue()
        {
            MockIndexer
                .Setup(
                    x => x.GetKeysFromIndex<IndexedEntity, string>("IndexedValue"))
                .Returns(EmptyIndexKeysEntityList);
            var data = DataManager.ReadIndexKeys<IndexedEntity, string>(x => x.IndexedValue);
            var results = new List<IndexKey<string>>();
            await foreach (var d in data) results.Add(d);
            Assert.IsFalse(results.Any());
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadKeysWhenIndexHasAValueAndThereIsData()
        {
            var data = DataManager.ReadIndexKeys<IndexedEntity, string>(x => x.IndexedValue);
            var result = new List<IndexKey<string>>();
            await foreach (var d in data) result.Add(d);
            Assert.AreEqual(result.Count(), 1);
            Assert.AreSame(ExpectedIndexKey, result.ElementAt(0));
        }
        
        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteDataWithNoAuditFails()
        {
            MockDataStream
                .Setup(x => x.WriteAndCommit(It.IsAny<int>(), It.IsAny<DataEntity>()))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedBasicEntity1);
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteDataWithNoAuditIndexingFails()
        {
            MockIndexer
                .Setup(x => x.Index(It.IsAny<DataEntity>()))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedIndexedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedIndexedEntity.Id, ExpectedIndexedEntity));
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteNewDataWithNoAuditIndexingFails()
        {
            MockDataStream
                .Setup(x => x.Read<IndexedEntity>(ExpectedIndexedEntity.Id))
                .ReturnsAsync((IndexedEntity) null);
            MockIndexer
                .Setup(x => x.Index(It.IsAny<DataEntity>()))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedIndexedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedIndexedEntity.Id, (DataEntity) null));
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> WriteDataWithNoAuditOrIndexSuccess()
        {
            var result = await DataManager.Write(ExpectedBasicEntity1);
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> WriteDataWithNoAuditButIndexSuccess()
        {
            var result = await DataManager.Write(ExpectedIndexedEntity);
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteDataWithAuditWriteFails()
        {
            MockDataStream
                .Setup(x => x.Write(It.IsAny<int>(), It.IsAny<DataEntity>()))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Commit<BasicEntity>(It.IsAny<int>()), Times.Never);
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteDataWithAuditCreateEventFails()
        {
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false);
            MockAudit
                .Setup(x => x.CreationEvent(ExpectedAuditedEntity))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.DiscardChanges<BasicEntity>(ExpectedAuditedEntity.Id));
            MockDataStream
                .Verify(x => x.Commit<BasicEntity>(It.IsAny<int>()), Times.Never);
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteDataWithAuditUpdateEventFails()
        {
            MockAudit
                .Setup(x => x.UpdateEvent(ExpectedAuditedEntity, ExpectedAuditedEntity))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.DiscardChanges<BasicEntity>(ExpectedAuditedEntity.Id));
            MockDataStream
                .Verify(x => x.Commit<BasicEntity>(It.IsAny<int>()), Times.Never);
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteNewDataWithAuditCommitFails()
        {
            MockDataStream
                .Setup(x => x.Commit<BasicEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false);
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedAuditedEntity);
            MockAudit
                .Verify(x => x.DiscardEvents(ExpectedAuditedEntity));
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteDataWithAuditCommitFails()
        {
            MockDataStream
                .Setup(x => x.Commit<BasicEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedAuditedEntity);
            MockAudit
                .Verify(x => x.DiscardEvents(ExpectedAuditedEntity));
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteNewDataWithAuditWhenAuditCommitFails()
        {
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .ReturnsAsync(false);
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, (DataEntity) null), Times.Once);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteDataWithAuditWhenAuditCommitFails()
        {
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, ExpectedAuditedEntity), Times.Once);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteNewDataWithAuditWhenIndexingFails()
        {
            MockIndexer
                .Setup(x => x.Index(ExpectedAuditedEntity))
                .ReturnsAsync(false);
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, (DataEntity) null), Times.Once);
            MockIndexer
                .Verify(x => x.Index(ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.RollbackEvent(ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteDataWithAuditWhenIndexingFails()
        {
            MockIndexer
                .Setup(x => x.Index(ExpectedAuditedEntity))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, ExpectedAuditedEntity), Times.Once);
            MockIndexer
                .Verify(x => x.Index(ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.RollbackEvent(ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> WriteNewDataWithAuditWhenIndexingSuccess()
        {
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false);
            var result = await DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, (DataEntity) null), Times.Never);
            MockIndexer
                .Verify(x => x.Index(ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.RollbackEvent(ExpectedAuditedEntity), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> WriteDataWithAuditWhenIndexingSuccess()
        {
            var result = await DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, ExpectedAuditedEntity), Times.Never);
            MockIndexer
                .Verify(x => x.Index(ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.RollbackEvent(ExpectedAuditedEntity), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteDataWhenItDoesNotExist()
        {
            MockDataStream
                .Setup(x => x.Exists<BasicEntity>(ExpectedBasicEntity1.Id))
                .ReturnsAsync(false);
            return await DataManager.Delete<BasicEntity>(ExpectedBasicEntity1.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteDataWhenDeleteFails()
        {
            MockDataStream
                .Setup(x => x.Delete<DataEntity>(ExpectedBasicEntity1.Id))
                .ReturnsAsync(false);
            return await DataManager.Delete<BasicEntity>(ExpectedBasicEntity1.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteDataUnindexFails()
        {
            MockIndexer
                .Setup(x => x.Unindex(ExpectedBasicEntity1))
                .ReturnsAsync(false);
            return await DataManager.Delete<BasicEntity>(ExpectedBasicEntity1.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> DeleteDataSuccess()
        {
            return await DataManager.Delete<BasicEntity>(ExpectedBasicEntity1.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteDataWithAuditWhenItDoesNotExist()
        {
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false);
            var result = await DataManager.Delete<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockAudit
                .Verify(x => x.DeleteEvent(ExpectedAuditedEntity), Times.Never);
            MockDataStream
                .Verify(x => x.Delete<AuditedEntity>(It.IsAny<int>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteDataWithAuditDeleteEventFails()
        {
            MockAudit
                .Setup(x => x.DeleteEvent(It.IsAny<AuditedEntity>()))
                .ReturnsAsync(false);
            var result = await DataManager.Delete<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockDataStream
                .Verify(x => x.Delete<AuditedEntity>(It.IsAny<int>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteDataWithAuditDeleteFails()
        {
            MockDataStream
                .Setup(x => x.Delete<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false);
            var result = await DataManager.Delete<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockAudit
                .Verify(x => x.CommitEvents(ExpectedAuditedEntity), Times.Never);
            MockAudit
                .Verify(x => x.DiscardEvents(ExpectedAuditedEntity), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteDataWithAuditCommitEventFails()
        {
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .ReturnsAsync(false);
            var result = await DataManager.Delete<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, ExpectedAuditedEntity), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteDataWithAuditUnindexFails()
        {
            MockIndexer
                .Setup(x => x.Unindex(ExpectedAuditedEntity))
                .ReturnsAsync(false);
            var result = await DataManager.Delete<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.RollbackEvent(ExpectedAuditedEntity), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> DeleteDataWithAuditDeleteSuccessful()
        {
            var result = await DataManager.Delete<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockDataStream
                .Verify(x => x.Undelete<AuditedEntity>(ExpectedAuditedEntity.Id), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> RestoreDataWhenItExists()
        {
            return await DataManager.Restore<BasicEntity>(ExpectedBasicEntity1.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> RestoreDataWhenDoesNotExistButUndeleteFails()
        {
            MockDataStream
                .Setup(x => x.Exists<BasicEntity>(ExpectedBasicEntity1.Id))
                .ReturnsAsync(false);
            MockDataStream
                .Setup(x => x.Undelete<BasicEntity>(ExpectedBasicEntity1.Id))
                .ReturnsAsync(false);
            return await DataManager.Restore<BasicEntity>(ExpectedBasicEntity1.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> RestoreDataWithAuditingWhenDoesNotExistButUndeleteEventFails()
        {
            MockDataStream
                .SetupSequence(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false)
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Undelete<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.UndeleteEvent(ExpectedAuditedEntity))
                .ReturnsAsync(false);
            var result = await DataManager.Restore<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockDataStream
                .Verify(x => x.Delete<AuditedEntity>(ExpectedAuditedEntity.Id), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> RestoreDataWithAuditingWhenDoesNotExistButCommitEventFails()
        {
            MockDataStream
                .SetupSequence(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false)
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Undelete<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.UndeleteEvent(ExpectedAuditedEntity))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .ReturnsAsync(false);
            var result = await DataManager.Restore<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockAudit
                .Verify(x => x.DiscardEvents(ExpectedAuditedEntity), Times.Once);
            MockDataStream
                .Verify(x => x.Delete<AuditedEntity>(ExpectedAuditedEntity.Id), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> RestoreDataWithAuditingWhenDoesNotExistButIndexingFails()
        {
            MockDataStream
                .SetupSequence(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false)
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Undelete<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.UndeleteEvent(ExpectedAuditedEntity))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .ReturnsAsync(true);
            MockIndexer
                .Setup(x => x.Index(ExpectedAuditedEntity))
                .ReturnsAsync(false);
            var result = await DataManager.Restore<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockAudit
                .Verify(x => x.RollbackEvent(ExpectedAuditedEntity), Times.Once);
            MockDataStream
                .Verify(x => x.Delete<AuditedEntity>(ExpectedAuditedEntity.Id), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> RestoreDataWithAuditingWhenDoesNotExistAndRestoredSuccessfully()
        {
            MockDataStream
                .SetupSequence(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(false)
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Undelete<AuditedEntity>(ExpectedAuditedEntity.Id))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.UndeleteEvent(ExpectedAuditedEntity))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .ReturnsAsync(true);
            MockIndexer
                .Setup(x => x.Index(ExpectedAuditedEntity))
                .ReturnsAsync(true);
            return await DataManager.Restore<AuditedEntity>(ExpectedAuditedEntity.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> RestoreDataWhenDoesNotExistButIndexingFails()
        {
            MockDataStream
                .SetupSequence(x => x.Exists<BasicEntity>(ExpectedBasicEntity1.Id))
                .ReturnsAsync(false)
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Undelete<BasicEntity>(ExpectedBasicEntity1.Id))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.UndeleteEvent(ExpectedBasicEntity1))
                .ReturnsAsync(true);
            MockIndexer
                .Setup(x => x.Index(ExpectedBasicEntity1))
                .ReturnsAsync(false);
            var result = await DataManager.Restore<BasicEntity>(ExpectedBasicEntity1.Id);
            MockDataStream
                .Verify(x => x.Delete<BasicEntity>(ExpectedBasicEntity1.Id), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> RestoreDataWhenDoesNotExistRestoredSuccessfully()
        {
            MockDataStream
                .SetupSequence(x => x.Exists<BasicEntity>(ExpectedBasicEntity1.Id))
                .ReturnsAsync(false)
                .ReturnsAsync(true);
            MockDataStream
                .Setup(x => x.Undelete<BasicEntity>(ExpectedBasicEntity1.Id))
                .ReturnsAsync(true);
            MockAudit
                .Setup(x => x.UndeleteEvent(ExpectedBasicEntity1))
                .ReturnsAsync(true);
            MockIndexer
                .Setup(x => x.Index(ExpectedBasicEntity1))
                .ReturnsAsync(true);
            var result = await DataManager.Restore<BasicEntity>(ExpectedBasicEntity1.Id);
            MockDataStream
                .Verify(x => x.Delete<BasicEntity>(ExpectedBasicEntity1.Id), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator")]
        public void GetNextIdReturnsDataStreamsNextId()
        {
            Assert.AreEqual(ExpectedNextBasicEntityId, DataManager.GetNextId<BasicEntity>());
        }

        [Test(Author = "PackDB Creator")]
        public void GetAuditLogWhenNotAnAuditableType()
        {
            var result = DataManager.GetAuditLog<BasicEntity>(ExpectedBasicEntity1.Id);
            Assert.IsNull(result);
        }
        
        [Test(Author = "PackDB Creator")]
        public  async Task GetAuditLogWhenAnAuditableType()
        {
            var result = await DataManager.GetAuditLog<AuditedEntity>(ExpectedAuditedEntity.Id);
            Assert.AreSame(ExpectedAuditedEntityAuditLog,result);
        }

    }
}