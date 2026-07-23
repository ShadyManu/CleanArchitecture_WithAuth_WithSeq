using Application.Dtos.ToDo.Response;
using Domain.Entities;
using Xunit; 

namespace Application.UnitTests.Tests.Mapper;

public class MapperTests
{
    [Fact]
    public void ToDoEntity_ToDoResponse_Mapper_AvoidMissingFields()
    {
        // Arrange
        IReadOnlyList<string> ignoredFields =
        [
            nameof(ToDoEntity.Created), 
            nameof(ToDoEntity.CreatedBy),
            nameof(ToDoEntity.LastModified),
            nameof(ToDoEntity.LastModifiedBy)
        ];

        var entityProperties = typeof(ToDoEntity).GetProperties()
            .Select(p => p.Name)
            .ToList();

        var dtoProperties = typeof(ToDoResponse).GetProperties()
            .Select(p => p.Name)
            .ToList();

        // Act
        var missingInDto = entityProperties
            .Except(ignoredFields)
            .Except(dtoProperties)
            .ToList();

        // Assert
        Assert.Empty(missingInDto);
    }
}
