using Application.Common.Interfaces.Repositories;
using Application.Common.Result;
using Application.Features.ToDo.Commands.Delete;
using Application.UnitTests.Constants;
using Moq;
using Xunit;

namespace Application.UnitTests.Tests.ToDo.Commands;

public class DeleteToDoCommandHandlerTest
{
    private readonly DeleteToDoCommandHandler _handler;
    private readonly Mock<IToDoRepository> _toDoItemRepositoryMock = new();

    public DeleteToDoCommandHandlerTest()
    {
        _handler = new DeleteToDoCommandHandler(_toDoItemRepositoryMock.Object);
    }

    #region ValidationTests

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", false, ValidatorMessage.InvalidGuid)]
    [InlineData("11111111-1111-1111-1111-111111111111", true, null)]
    public void Validate_ShouldReturnExpectedResult(string id, bool expectedIsValid, string? expectedErrorMessage)
    {
        // Arrange
        var command = new DeleteToDoCommand(Guid.Parse(id));

        // Act
        (bool isValid, string? errorMessage) = command.Validate();

        // Assert
        Assert.Equal(expectedIsValid, isValid);
        Assert.Equal(expectedErrorMessage, errorMessage);
    }

    #endregion

    #region HandlerTests
    [Fact]
    public async Task Handle_ShouldReturnErrorResult_WhenDeletionFails()
    {
        // Arrange
        var toDoItemId = ToDoTestsConstants.ValidToDoId;
        var command = new DeleteToDoCommand(toDoItemId);
        _toDoItemRepositoryMock
            .Setup(repo => repo.DeleteAsync(toDoItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        Result<bool> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Data);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorMessage.NotFound, result.Error.Message);
        _toDoItemRepositoryMock
            .Verify(database => database.DeleteAsync(toDoItemId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessResult_WhenDeletionIsSuccessful()
    {
        // Arrange
        var toDoItemId = ToDoTestsConstants.ValidToDoId;
        var command = new DeleteToDoCommand(toDoItemId);
        _toDoItemRepositoryMock
            .Setup(repo => repo.DeleteAsync(toDoItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<bool> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Null(result.Error);
        Assert.True(result.Data);
        _toDoItemRepositoryMock
            .Verify(database => database.DeleteAsync(toDoItemId, CancellationToken.None), Times.Once);
    }

    #endregion
}
