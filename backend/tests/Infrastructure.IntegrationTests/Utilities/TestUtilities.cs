using System.Reflection;
using Xunit;

namespace Infrastructure.IntegrationTests.Utilities;

public static class TestUtilities
{
    public static void AssertCollectionsEqual<T>(IReadOnlyList<T> expectedList, IReadOnlyList<T> actualList)
    {
        Assert.Equal(expectedList.Count, actualList.Count);

        for (var i = 0; i < expectedList.Count; i++)
        {
            AssertObjectsEqual(expectedList.ElementAtOrDefault(i), actualList.ElementAtOrDefault(i));
        }
    }
    
    public static void AssertCollectionsEqual<TEntity, TDto>(IReadOnlyList<TEntity> expectedList, IReadOnlyList<TDto> actualList)
    {
        Assert.Equal(expectedList.Count, actualList.Count);

        for (var i = 0; i < expectedList.Count; i++)
        {
            AssertEntityMatchesDto(expectedList.ElementAtOrDefault(i), actualList.ElementAtOrDefault(i));
        }
    }
    
    public static void AssertEntityMatchesDto<TEntity, TDto>(TEntity entity, TDto dto)
    {
        var entityType = typeof(TEntity);
        var dtoType = typeof(TDto);

        var dtoProperties = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var dtoProp in dtoProperties)
        {
            var entityProp = entityType.GetProperty(dtoProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (entityProp == null) continue;

            var dtoValue = dtoProp.GetValue(dto);
            var entityValue = entityProp.GetValue(entity);

            Assert.Equal(entityValue, dtoValue);
        }
    }
    
    private static void AssertObjectsEqual<T>(T expected, T actual)
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var expectedValue = prop.GetValue(expected);
            var actualValue = prop.GetValue(actual);

            Assert.Equal(expectedValue, actualValue);
        }
    }
}
