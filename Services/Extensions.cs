namespace photo_reviewer_4net;

public static class StringExtensions
{
    public static bool EqualsIgnoreCase(this string a, string b) {
        return a.Equals(b, StringComparison.OrdinalIgnoreCase);
    }

    public static bool StartsWithIgnoreCase(this string a, string b) {
        return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
    }

    public static int CompareToIgnoreCase(this string a, string b) {
        return String.Compare(a, b, ignoreCase: true);
    }
}

public static class ExceptionExtensions
{
    // Return true if this exception or any of its descendants are of type T
    public static bool ContainsException<T>(this Exception exception) {
        for (Exception? cur = exception; cur != null; cur = cur.InnerException) {
            if (cur is T) {
                return true;
            }
        }
        return false;
    }
}
