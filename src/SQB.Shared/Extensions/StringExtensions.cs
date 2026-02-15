using System.Text.RegularExpressions;

namespace SQB.Shared.Extensions;

public static partial class StringExtensions
{
    public static bool IsValidEmail(this string s)
    {
        return s switch
        {
            null => throw new ArgumentNullException(),
            _ => EmailValidation().IsMatch(s)
        };
    }

    [GeneratedRegex(@"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex EmailValidation();
}

