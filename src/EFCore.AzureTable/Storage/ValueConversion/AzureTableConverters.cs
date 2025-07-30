// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.AzureTable.Storage.ValueConversion;

/// <summary>
///     Value converters for Azure Table Storage.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-value-converters">Value converters</see> and
///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
public static class AzureTableConverters
{
    /// <summary>
    ///     A value converter that converts <see cref="DateTime"/> to UTC for Azure Table Storage.
    /// </summary>
    public class DateTimeToUtcConverter : ValueConverter<DateTime, DateTime>
    {
        /// <summary>
        ///     Creates a new instance of this converter.
        /// </summary>
        public DateTimeToUtcConverter()
            : base(
                v => v.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(v, DateTimeKind.Utc) 
                    : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    /// <summary>
    ///     A value converter that converts nullable <see cref="DateTime"/> to UTC for Azure Table Storage.
    /// </summary>
    public class NullableDateTimeToUtcConverter : ValueConverter<DateTime?, DateTime?>
    {
        /// <summary>
        ///     Creates a new instance of this converter.
        /// </summary>
        public NullableDateTimeToUtcConverter()
            : base(
                v => v.HasValue 
                    ? (v.Value.Kind == DateTimeKind.Unspecified 
                        ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) 
                        : v.Value.ToUniversalTime())
                    : null,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null)
        {
        }
    }

    /// <summary>
    ///     A value converter that ensures string values don't exceed Azure Table's 64KB limit.
    /// </summary>
    public class StringSizeLimiterConverter : ValueConverter<string, string>
    {
        private const int MaxStringSize = 32 * 1024; // 32KB to be safe (Azure limit is 64KB)

        /// <summary>
        ///     Creates a new instance of this converter.
        /// </summary>
        public StringSizeLimiterConverter()
            : base(
                v => v != null && v.Length * sizeof(char) > MaxStringSize 
                    ? v.Substring(0, MaxStringSize / sizeof(char)) 
                    : v!,
                v => v)
        {
        }
    }

    /// <summary>
    ///     A value converter that ensures byte arrays don't exceed Azure Table's 64KB limit.
    /// </summary>
    public class ByteArraySizeLimiterConverter : ValueConverter<byte[], byte[]>
    {
        private const int MaxByteArraySize = 64 * 1024; // 64KB

        /// <summary>
        ///     Creates a new instance of this converter.
        /// </summary>
        public ByteArraySizeLimiterConverter()
            : base(
                v => v != null && v.Length > MaxByteArraySize 
                    ? v.Take(MaxByteArraySize).ToArray() 
                    : v!,
                v => v)
        {
        }
    }
}