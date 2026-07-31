using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace GrpcWebBridge.Domain
{
    /// <summary>
    /// Extension methods for enum types used throughout the library.
    /// Provides a friendly display string (respecting <see cref="DisplayAttribute"/> if present)
    /// and a case‑insensitive TryParse helper.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Returns a display string for the enum value.
        /// If the enum member is decorated with <see cref="DisplayAttribute"/>,
        /// the <c>Name</c> property of that attribute is returned; otherwise,
        /// the enum member name is returned.
        /// </summary>
        /// <param name="value">The enum value.</param>
        /// <returns>A user‑friendly string representation.</returns>
        public static string ToDisplayString(this Enum value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            Type enumType = value.GetType();
            string memberName = value.ToString();

            // Try to get the field representing the enum member (handles simple enums)
            FieldInfo? fieldInfo = enumType.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
            if (fieldInfo != null)
            {
                var displayAttr = fieldInfo.GetCustomAttribute<DisplayAttribute>();
                if (displayAttr?.Name != null)
                {
                    return displayAttr.Name;
                }
            }

            // Fallback to the raw enum name
            return memberName;
        }

        /// <summary>
        /// Tries to parse a string into the specified enum type.
        /// The parse is case‑insensitive and will succeed if the string matches
        /// either the enum member name or its <see cref="DisplayAttribute.Name"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to parse into.</typeparam>
        /// <param name="value">The string representation of the enum value.</param>
        /// <param name="result">When this method returns, contains the parsed enum value if successful; otherwise the default value.</param>
        /// <returns>True if the parse succeeded; otherwise false.</returns>
        public static bool TryParse<TEnum>(this string? value, out TEnum result) where TEnum : struct, Enum
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // First try a normal enum parse (case‑insensitive)
            if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
            {
                result = parsed;
                return true;
            }

            // If that fails, attempt to match against DisplayAttribute.Name values
            var enumType = typeof(TEnum);
            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var displayAttr = field.GetCustomAttribute<DisplayAttribute>();
                if (displayAttr != null && string.Equals(displayAttr.Name, value, StringComparison.OrdinalIgnoreCase))
                {
                    result = (TEnum)field.GetValue(null)!;
                    return true;
                }
            }

            return false;
        }
    }
}
