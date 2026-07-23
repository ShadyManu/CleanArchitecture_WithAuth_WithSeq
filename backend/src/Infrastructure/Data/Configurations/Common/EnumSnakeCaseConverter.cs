using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Data.Configurations.Common;

public class EnumSnakeCaseConverter<TEnum>()
    : ValueConverter<TEnum, string>(
        v => JsonNamingPolicy.SnakeCaseLower.ConvertName(v.ToString()),
        v => Parse(v))
    where TEnum : struct, Enum
{
    private static TEnum Parse(string value) =>
        Enum.GetValues<TEnum>()
            .First(e => JsonNamingPolicy.SnakeCaseLower.ConvertName(e.ToString()) == value);
}
