using Application.Common.Interfaces.Repositories;
using Application.Common.Result;
using Application.Features.ToDo.Queries.Get;
using Application.UnitTests.Constants;
using Domain.Entities;
using Moq;
using Xunit;

namespace Application.UnitTests.Tests.ToDo.Queries;

public class GetToDoQueryHandlerTest
{
    private readonly GetToDoQueryHandler _handler;
    private readonly Mock<IToDoRepository> _toDoItemRepositoryMock = new();

    public GetToDoQueryHandlerTest()
    {
        _handler = new GetToDoQueryHandler(_toDoItemRepositoryMock.Object);
    }

    #region ValidationTests

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", false, ValidatorMessage.InvalidGuid)]
    [InlineData("11111111-1111-1111-1111-111111111111", true, null)]
    public void Validate_ShouldReturnExpectedResult(string id, bool expectedIsValid, string? expectedErrorMessage)
    {
        // Arrange
        var query = new GetToDoQuery(Guid.Parse(id));

        // Act
        (bool isValid, string? errorMessage) = query.Validate();

        // Assert
        Assert.Equal(expectedIsValid, isValid);
        Assert.Equal(expectedErrorMessage, errorMessage);
    }

    #endregion

    #region HandlerTests

    [Fact]
    public async Task Handle_ShouldReturnErrorResult_WhenToDoItemDoesNotExist()
    {
        // Arrange
        var toDoItemId = ToDoTestsConstants.ValidToDoId;
        var query = new GetToDoQuery(toDoItemId);
        _toDoItemRepositoryMock
            .Setup(r => r.GetByIdAsNoTrackingAsync(
                toDoItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ToDoEntity?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result.Data);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorMessage.NotFound, result.Error.Message);
        _toDoItemRepositoryMock
            .Verify(database => database.GetByIdAsNoTrackingAsync(
                toDoItemId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessResult_WhenToDoItemExists()
    {
        // Arrange
        var toDoItemId = ToDoTestsConstants.ValidToDoId;
        var toDoItemEntity = new ToDoEntity
        {
            Id = toDoItemId,
            Title = ToDoTestsConstants.ValidTitle,
            Priority = ToDoTestsConstants.ValidPriority
        };
        var query = new GetToDoQuery(toDoItemId);
        _toDoItemRepositoryMock
            .Setup(r => r.GetByIdAsNoTrackingAsync(
                toDoItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(toDoItemEntity);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(toDoItemEntity.Id, result.Data!.Id);
        Assert.Equal(toDoItemEntity.Title, result.Data.Title);
        Assert.Equal(toDoItemEntity.Priority, result.Data.Priority);
        _toDoItemRepositoryMock
            .Verify(database => database.GetByIdAsNoTrackingAsync(
                toDoItemId, CancellationToken.None), Times.Once);
    }

    #endregion
}
