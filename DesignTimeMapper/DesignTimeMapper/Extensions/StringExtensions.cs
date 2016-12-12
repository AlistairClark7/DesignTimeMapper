using System;

namespace DesignTimeMapper.Extensions
{
    public static class StringExtensions
    {
        // Convert the string to camel case.
        public static string ToCamelCase(this string input)
        {
            // If there are 0 or 1 characters, just return the string.
            if (input == null || input.Length < 2)
                return input;

            // Split the string into words.
            string[] words = input.Split(
                new char[] { },
                StringSplitOptions.RemoveEmptyEntries);

            // Combine the words.
            string result = words[0].ToLower();
            for (int i = 1; i < words.Length; i++)
            {
                result +=
                    words[i].Substring(0, 1).ToUpper() +
                    words[i].Substring(1);
            }

            return result;
        }
    }
}