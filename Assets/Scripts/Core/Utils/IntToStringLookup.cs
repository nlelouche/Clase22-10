namespace FlappyBird.Core.Utils
{
    /// <summary>
    /// Zero-GC string lookup table for integer values in high-frequency rendering and UI loops.
    /// Eliminates heap allocations from int.ToString() for common score ranges [0..999].
    /// </summary>
    public static class IntToStringLookup
    {
        private const int LowerBound = 0;
        private const int UpperBound = 999;
        private const int CacheSize = UpperBound - LowerBound + 1;

        private static readonly string[] Cache;

        static IntToStringLookup()
        {
            Cache = new string[CacheSize];
            for (int i = 0; i < CacheSize; i++)
            {
                Cache[i] = (LowerBound + i).ToString();
            }
        }

        /// <summary>
        /// Retrieves a cached string instance for the given integer value.
        /// Guaranteed 0 bytes GC allocation for values between 0 and 999 inclusive.
        /// Safely falls back to int.ToString() for values out-of-bounds (negative or >= 1000).
        /// </summary>
        /// <param name="value">The integer value to format.</param>
        /// <returns>A string representation of the integer.</returns>
        public static string GetString(int value)
        {
            if (value >= LowerBound && value <= UpperBound)
            {
                return Cache[value - LowerBound];
            }

            return value.ToString();
        }

        /// <summary>
        /// Shorthand alias for GetString(value).
        /// </summary>
        public static string Get(int value) => GetString(value);
    }
}
