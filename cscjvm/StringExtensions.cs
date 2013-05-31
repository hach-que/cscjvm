namespace cscjvm
{
    public static class StringExtensions
    {
        public static string ToJavaNamespace(this string str)
        {
            return str.Replace('.', '/');
        }
    }
}

