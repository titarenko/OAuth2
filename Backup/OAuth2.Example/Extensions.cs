namespace OAuth2.Example
{
    /// <summary>
    /// Several extension methods used in this example app.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Cuts line to be not longer than specified length.
        /// </summary>
        public static string Cut(this string line, int maxLength)
        {
            return line.Length > maxLength ? line.Substring(0, maxLength - 3) + "..." : line;
        }
    }
}