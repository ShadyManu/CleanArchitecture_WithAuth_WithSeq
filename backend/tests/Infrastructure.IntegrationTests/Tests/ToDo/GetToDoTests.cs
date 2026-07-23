using System.Net.Http.Json;
using Application.Common.Result;
using Application.Dtos.ToDo.Response;
using Infrastructure.IntegrationTests.Utilities;
using Xunit;

namespace Infrastructure.IntegrationTests.Tests.ToDo;

public class GetToDoTests : BaseToDoTest
{
    public GetToDoTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    #region Tests

    [Fact]
    public async Task GetAllToDos_ShouldReturnAllEntities_WhenThereAreEntities()
    {
        // Act
        var response = await _httpClientAnonymous.GetAsync(BaseEndpoint, TestContext.Current.CancellationToken);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<ToDoResponse>>>(TestContext.Current.CancellationToken);

        Assert.NotNull(result?.Data);
        Assert.Null(result.Error);
        Assert.True(result.Data.Count == ToDoEntitiesSeed.Count);
        TestUtilities.AssertCollectionsEqual(ToDoEntitiesSeed, result.Data);
    }
    
    [Fact]
    public async Task GetToDoById_ShouldReturnEntity_WhenIdIsValid()
    {
        // Arrange
        var existingEntity = ToDoEntitiesSeed[0];
        var endpoint = $"{BaseEndpoint}/{existingEntity.Id}";

        // Act
        var response = await _httpClientAnonymous.GetAsync(endpoint, TestContext.Current.CancellationToken);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<ToDoResponse>>(TestContext.Current.CancellationToken);

        Assert.NotNull(result?.Data);
        Assert.Null(result.Error);
        TestUtilities.AssertEntityMatchesDto(existingEntity, result.Data);
    }

    [Fact]
    public async Task GetToDoById_ShouldReturnError_WhenIdIsInvalid()
    {
        // Arrange
        var invalidId = Guid.CreateVersion7();
        var endpoint = $"{BaseEndpoint}/{invalidId}";

        // Act
        var response = await _httpClientAnonymous.GetAsync(endpoint, TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<ToDoResponse>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result?.Error);
        Assert.Null(result.Data);
        Assert.Equal(ErrorMessage.NotFound, result.Error.Message);
    }
    
    [Fact]
    public async Task GetToDoById_ShouldReturnValidationError_WhenIdIsEmptyGuid()
    {
        // Arrange
        var emptyGuid = Guid.Empty;
        var endpoint = $"{BaseEndpoint}/{emptyGuid}";

        // Act
        var response = await _httpClientAnonymous.GetAsync(endpoint, TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<ToDoResponse>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result?.Error);
        Assert.Null(result.Data);
        Assert.Equal(ValidatorMessage.InvalidGuid, result.Error.Message);
    }

    #endregion
}
