using System.Diagnostics.CodeAnalysis;
using Domain.Common.Constants;

namespace Application.Common.Utilities;

public static class EmailUtilities
{
    public static bool IsEmailAddress([NotNullWhen(true)] string? input)
    {
        if (input is null)
        {
            return false;
        }

        int inputLength = input.Length;

        if (inputLength is < DbConstraints.EmailMinLength or > DbConstraints.EmailMaxLength)
        {
            return false;
        }

        ReadOnlySpan<char> inputAsSpan = input.AsSpan();

        if (inputAsSpan.ContainsAny('\r', '\n'))
        {
            return false;
        }

        int indexOfAtSymbol = inputAsSpan.IndexOf('@');

        return (indexOfAtSymbol > 0 && indexOfAtSymbol < inputLength - 1);
    }
}
