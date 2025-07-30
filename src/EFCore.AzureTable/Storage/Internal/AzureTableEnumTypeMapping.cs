// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableEnumTypeMapping : AzureTableTypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableEnumTypeMapping(Type enumType)
        : base(
            new CoreTypeMappingParameters(
                enumType,
                converter: CreateEnumToStringConverter(enumType),
                comparer: CreateEnumComparer(enumType),
                keyComparer: null,
                jsonValueReaderWriter: null))
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected AzureTableEnumTypeMapping(CoreTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override CoreTypeMapping Clone(CoreTypeMappingParameters parameters)
        => new AzureTableEnumTypeMapping(parameters);

    private static ValueConverter CreateEnumToStringConverter(Type enumType)
    {
        var underlyingType = Nullable.GetUnderlyingType(enumType);
        var isNullable = underlyingType != null;
        var actualEnumType = underlyingType ?? enumType;

        if (isNullable)
        {
            var converterType = typeof(NullableEnumToStringConverter<>).MakeGenericType(actualEnumType);
            return (ValueConverter)Activator.CreateInstance(converterType)!;
        }
        else
        {
            var converterType = typeof(EnumToStringConverter<>).MakeGenericType(actualEnumType);
            return (ValueConverter)Activator.CreateInstance(converterType)!;
        }
    }

    private static ValueComparer CreateEnumComparer(Type enumType)
    {
        var underlyingType = Nullable.GetUnderlyingType(enumType);
        var actualEnumType = underlyingType ?? enumType;
        var comparerType = underlyingType != null
#pragma warning disable EF1001 // Internal EF Core API usage.
            ? typeof(NullableValueComparer<>).MakeGenericType(actualEnumType)
#pragma warning restore EF1001 // Internal EF Core API usage.
            : typeof(ValueComparer<>).MakeGenericType(actualEnumType);

        return (ValueComparer)Activator.CreateInstance(comparerType)!;
    }

    private class NullableEnumToStringConverter<TEnum> : ValueConverter<TEnum?, string?>
        where TEnum : struct, Enum
    {
        public NullableEnumToStringConverter()
            : base(
                v => v.HasValue ? v.Value.ToString() : null,
                v => v != null ? Enum.Parse<TEnum>(v) : null)
        {
        }
    }
}