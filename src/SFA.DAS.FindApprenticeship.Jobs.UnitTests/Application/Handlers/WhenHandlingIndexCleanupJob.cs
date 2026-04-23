using Azure.Search.Documents.Indexes.Models;
using SFA.DAS.FindApprenticeship.Jobs.Application.Handlers;
using SFA.DAS.FindApprenticeship.Jobs.Domain;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Interfaces;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Application.Handlers;

[TestFixture]
public class WhenHandlingIndexCleanupJob
{
    private static string GetIndexName(DateTime date)
    {
        return $"{Constants.IndexPrefix}{date.ToString(Constants.IndexDateSuffixFormat)}";
    }
    
    [Test, MoqAutoData]
    public async Task Then_The_Oldest_Indexes_Are_Deleted(
        [Frozen] Mock<IAzureSearchHelper> azureSearchHelper,
        IndexCleanupJobHandler sut)
    {
        // arrange
        var now = DateTime.UtcNow;
        var indexes = Enumerable.Range(1, 8).Select(x => new SearchIndex(GetIndexName(now.Subtract(new TimeSpan(0, x, 0))))).ToArray();
        var indexName1 = indexes[^1].Name;
        var indexName2 = indexes[^2].Name;
        
        new Random().Shuffle(indexes);
        azureSearchHelper.Setup(x => x.GetIndexes()).ReturnsAsync(() => indexes.ToList());

        // act
        await sut.Handle();

        // assert
        azureSearchHelper.Verify(x => x.DeleteIndex(indexName1), Times.Once);
        azureSearchHelper.Verify(x => x.DeleteIndex(indexName2), Times.Once);
    }

    private static readonly object[] DeletionTestCases = [
        new object[] { 1, Times.Never() },
        new object[] { 2, Times.Never() },
        new object[] { 3, Times.Never() },
        new object[] { 4, Times.Never() },
        new object[] { 5, Times.Never() },
        new object[] { 6, Times.Never() },
        new object[] { 7, Times.Once() },
        new object[] { 8, Times.Exactly(2) },
        new object[] { 9, Times.Exactly(3) },
        new object[] { 10, Times.Exactly(4) },
    ];
    
    [TestCaseSource(nameof(DeletionTestCases))]
    public async Task Then_The_Correct_Number_Of_Deletions_Occur(
        int count,
        Times when)
    {
        // arrange
        var azureSearchHelper = new Mock<IAzureSearchHelper>();
        var sut = new IndexCleanupJobHandler(azureSearchHelper.Object, Mock.Of<ILogger<IndexCleanupJobHandler>>());
        var now = DateTime.UtcNow;
        var indexes = Enumerable.Range(1, count).Select(x => new SearchIndex(GetIndexName(now.Subtract(new TimeSpan(0, x, 0))))).ToArray();
        
        new Random().Shuffle(indexes);
        azureSearchHelper.Setup(x => x.GetIndexes()).ReturnsAsync(() => indexes.ToList());

        // act
        await sut.Handle();

        // assert
        azureSearchHelper.Verify(x => x.DeleteIndex(It.IsAny<string>()), when);
    }

    [Test, MoqAutoData]
    public async Task Then_The_Aliased_Index_Is_Not_Removed_Even_If_It_Is_The_Oldest(
        [Frozen] Mock<IAzureSearchHelper> azureSearchHelper,
        IndexCleanupJobHandler sut)
    {
        // arrange
        var now = DateTime.UtcNow;
        var indexes = Enumerable.Range(1, 10).Select(x => new SearchIndex(GetIndexName(now.Subtract(new TimeSpan(0, x, 0))))).ToArray();
        var oldestIndexName = indexes[^1].Name;
        
        new Random().Shuffle(indexes);
        azureSearchHelper.Setup(x => x.GetIndexes()).ReturnsAsync(() => indexes.ToList());
        azureSearchHelper.Setup(x => x.GetAlias(Constants.AliasName)).ReturnsAsync(() => new SearchAlias(Constants.AliasName, [oldestIndexName]));

        // act
        await sut.Handle();

        // assert
        azureSearchHelper.Verify(x => x.DeleteIndex(oldestIndexName), Times.Never);
    }
        
    [Test, MoqAutoData]
    public async Task Then_Indexes_Not_Conforming_To_Name_Convention_Are_Retained(
        [Frozen] Mock<IAzureSearchHelper> azureSearchHelper,
        IndexCleanupJobHandler sut)
    {
        // arrange
        var indexes = Enumerable.Range(1, 3).Select(x => new SearchIndex($"another_index_{x}")).ToArray();
        indexes = indexes.Append(new SearchIndex(GetIndexName(DateTime.UtcNow.Subtract(new TimeSpan(0, 1, 0))))).ToArray();
        azureSearchHelper.Setup(x => x.GetAlias(Constants.AliasName)).ReturnsAsync(() => new SearchAlias(Constants.AliasName, [indexes[^1].Name]));
        
        new Random().Shuffle(indexes);
        azureSearchHelper.Setup(x => x.GetIndexes()).ReturnsAsync(() => indexes.ToList());

        // act
        await sut.Handle();

        // assert
        azureSearchHelper.Verify(x => x.DeleteIndex(It.IsAny<string>()), Times.Never);
    }
    
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(14)]
    public async Task If_Critical_Index_Threshold_Met_And_No_Indexes_Are_Deleted_Then_LogCritical_Is_Called(int indexCount)
    {
        // arrange
        var log = new Mock<ILogger<IndexCleanupJobHandler>>();
        var azureSearchHelper = new Mock<IAzureSearchHelper>();
        var sut = new IndexCleanupJobHandler(azureSearchHelper.Object, log.Object);
        var indexes = Enumerable.Range(1, indexCount).Select(x => new SearchIndex($"hogging_index_{x}")).ToArray();
        indexes = indexes.Append(new SearchIndex(GetIndexName(DateTime.UtcNow.Subtract(new TimeSpan(0, 1, 0))))).ToArray();
        azureSearchHelper.Setup(x => x.GetAlias(Constants.AliasName)).ReturnsAsync(() => new SearchAlias(Constants.AliasName, [indexes[^1].Name]));
        
        new Random().Shuffle(indexes);
        azureSearchHelper.Setup(x => x.GetIndexes()).ReturnsAsync(() => indexes.ToList());

        // act
        await sut.Handle();

        // assert
        log.Verify(x => x.Log(LogLevel.Critical, 0, It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}