using System.Net;
using System.Net.Http.Json;
using Application.Common.Result;
using Application.Dtos.ToDo.Request;
using Application.Dtos.ToDo.Response;
using Domain.Common.Constants;
using Infrastructure.Data;
using Infrastructure.IntegrationTests.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.IntegrationTests.Tests.ToDo;

public class PostPatchDeleteToDoTests : BaseToDoTest
{
    public PostPatchDeleteToDoTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    #region Tests

    // POST Tests
    [Fact]
    public async Task PostToDo_ShouldCreateEntity_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateToDoRequest
        {
            Title = "New ToDo",
            Priority = 4,
            Note = "This is a new ToDo item",
            Reminder = DateTimeOffset.Now.AddHours(2)
        };

        // Act
        var response = await _httpClientAnonymous.PostAsJsonAsync(BaseEndpoint, request, TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<ToDoResponse>>(TestContext.Current.CancellationToken);

        Assert.NotNull(result?.Data);
        Assert.Null(result.Error);
        Assert.Equal(request.Title, result.Data.Title);
        Assert.Equal(request.Priority, result.Data.Priority);
        Assert.Equal(request.Note, result.Data.Note);
        Assert.Equal(TimeSpan.Zero, result.Data.Reminder?.Offset);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.ToDos.AnyAsync(
            x => x.Id == result.Data.Id && x.Title == request.Title,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PostToDo_ShouldReturnBadRequest_WhenPayloadIsMissing()
    {
        // Act
        var response = await _httpClientAnonymous.PostAsJsonAsync(BaseEndpoint, new object(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0, "CannotBeEmpty", DbConstraints.MinToDoNameLength)]
    [InlineData(DbConstraints.MaxToDoNameLength + 1, "MaxLength", DbConstraints.MaxToDoNameLength)]
    public async Task PostToDo_ShouldReturnValidationError_WhenTitleIsInvalid(
        int titleLength, string errorType, short limit)
    {
        // Arrange
        var title = new string('a', titleLength);
        var request = new CreateToDoRequest { Title = title, Priority = 1 };
        
        // Act
        var response = await _httpClientAnonymous.PostAsJsonAsync(
            BaseEndpoint, request, TestContext.Current.CancellationToken);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<ToDoResponse>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result?.Error);

        var expectedMessage = errorType switch
        {
            "CannotBeEmpty" => ValidatorMessage.CannotBeEmpty(nameof(CreateToDoRequest.Title)),
            "MinLength" => ValidatorMessage.MinLength(nameof(CreateToDoRequest.Title), limit),
            "MaxLength" => ValidatorMessage.MaxLength(nameof(CreateToDoRequest.Title), limit),
            _ => throw new ArgumentException("Invalid error type")
        };

        Assert.Equal(expectedMessage, result.Error.Message);
    }

    [Fact]
    public async Task PostToDo_ShouldReturnValidationError_WhenNoteExceedsDatabaseLimit()
    {
        var request = new CreateToDoRequest
        {
            Title = "Valid title",
            Priority = 1,
            Note = new string('a', DbConstraints.MaxToDoNoteLength + 1)
        };

        var response = await _httpClientAnonymous.PostAsJsonAsync(
            BaseEndpoint,
            request,
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<Result<ToDoResponse>>(
            TestContext.Current.CancellationToken);

        Assert.Equal(
            ValidatorMessage.MaxLength(
                nameof(CreateToDoRequest.Note),
                DbConstraints.MaxToDoNoteLength),
            result?.Error?.Message);
    }

    [Fact]
    public async Task PatchToDo_ShouldReturnValidationError_WhenTitleIsTooLong()
    {
        // Arrange
        var request = new UpdateToDoRequest
        {
            Id = FirstToDoId,
            Title = new string('a', DbConstraints.MaxToDoNameLength + 1),
            Priority = 1
        };

        // Act
        var response = await _httpClientAnonymous.PatchAsJsonAsync(
            BaseEndpoint, request, TestContext.Current.CancellationToken);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<ToDoResponse>>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(result?.Error);
        Assert.Equal(
            ValidatorMessage.MaxLength(nameof(UpdateToDoRequest.Title), DbConstraints.MaxToDoNameLength),
            result.Error.Message);
    }

    // PATCH Tests
    [Fact]
    public async Task PatchToDo_ShouldUpdateEntity_WhenRequestIsValid()
    {
        // Arrange
        var updateRequest = new UpdateToDoRequest
        {
            Id = FirstToDoId,
            Title = "Updated ToDo",
            Priority = 5,
            Note = "Updated Note",
            Reminder = DateTimeOffset.Now.AddDays(1)
        };

        // Act
        var response = await _httpClientAnonymous.PatchAsJsonAsync(BaseEndpoint, updateRequest, TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<ToDoResponse>>(TestContext.Current.CancellationToken);

        Assert.NotNull(result?.Data);
        Assert.Null(result.Error);
        TestUtilities.AssertEntityMatchesDto(updateRequest, result.Data);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await db.ToDos.AsNoTracking().SingleAsync(
            x => x.Id == FirstToDoId, TestContext.Current.CancellationToken);
        Assert.Equal(updateRequest.Title, stored.Title);
        Assert.Equal(updateRequest.Priority, stored.Priority);
        Assert.Equal(updateRequest.Note, stored.Note);
        Assert.NotNull(updateRequest.Reminder);
        Assert.NotNull(stored.Reminder);
        Assert.True(
            (updateRequest.Reminder.Value.ToUniversalTime() - stored.Reminder.Value)
            .Duration() <= TimeSpan.FromMicroseconds(1));
    }

    [Fact]
    public async Task PatchToDo_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Act
        var response = await _httpClientAnonymous.PatchAsJsonAsync(BaseEndpoint, new object(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteToDo_ShouldRemoveEntity_WhenIdIsValid()
    {
        // Arrange
        var idToDelete = SecondToDoId;

        // Act
        var response = await _httpClientAnonymous.DeleteAsync($"{BaseEndpoint}/{idToDelete}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<bool>>(TestContext.Current.CancellationToken);
        Assert.True(result?.Data);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.ToDos.AnyAsync(
            x => x.Id == idToDelete, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteToDo_ShouldReturnNotFound_WhenIdDoesNotExist()
    {
        // Arrange
        var nonExistingId = Guid.CreateVersion7();

        // Act
        var response = await _httpClientAnonymous.DeleteAsync($"{BaseEndpoint}/{nonExistingId}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<bool>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result?.Error);
        Assert.Equal(ErrorMessage.NotFound, result.Error.Message);
    }

    [Fact]
    public async Task DeleteToDo_ShouldReturnValidationError_WhenIdIsEmpty()
    {
        // Act
        var response = await _httpClientAnonymous.DeleteAsync(
            $"{BaseEndpoint}/{Guid.Empty}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<bool>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result?.Error);
        Assert.Equal(ValidatorMessage.InvalidGuid, result.Error.Message);
    }

    #endregion
}
