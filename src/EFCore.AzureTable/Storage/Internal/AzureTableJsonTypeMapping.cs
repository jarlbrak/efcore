// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableJsonTypeMapping : AzureTableTypeMapping
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableJsonTypeMapping(Type clrType, JsonSerializerOptions? jsonSerializerOptions = null)
        : base(
            new CoreTypeMappingParameters(
                clrType,
                converter: CreateJsonConverter(clrType, jsonSerializerOptions ?? DefaultJsonSerializerOptions),
                comparer: null,
                keyComparer: null,
                jsonValueReaderWriter: null))
    {
        JsonSerializerOptions = jsonSerializerOptions ?? DefaultJsonSerializerOptions;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected AzureTableJsonTypeMapping(CoreTypeMappingParameters parameters)
        : base(parameters)
    {
        JsonSerializerOptions = DefaultJsonSerializerOptions;
    }

    /// <summary>
    ///     Gets the JSON serializer options.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override CoreTypeMapping Clone(CoreTypeMappingParameters parameters)
        => new AzureTableJsonTypeMapping(parameters);

    private static ValueConverter CreateJsonConverter(Type clrType, JsonSerializerOptions options)
    {
        var converterType = typeof(JsonValueConverter<>).MakeGenericType(clrType);
        return (ValueConverter)Activator.CreateInstance(converterType, options)!;
    }

    private class JsonValueConverter<T> : ValueConverter<T, string>
    {
        public JsonValueConverter(JsonSerializerOptions options)
            : base(
                v => JsonSerializer.Serialize(v, options),
                v => JsonSerializer.Deserialize<T>(v, options)!)
        {
        }
    }
}