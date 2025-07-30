// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableTypeMappingSource : TypeMappingSourceBase
{
    private readonly Dictionary<Type, CoreTypeMapping> _clrTypeMappings;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableTypeMappingSource(TypeMappingSourceDependencies dependencies)
        : base(dependencies)
    {
        _clrTypeMappings = new Dictionary<Type, CoreTypeMapping>
        {
            { typeof(string), new AzureTableStringTypeMapping() },
            { typeof(int), new AzureTableIntTypeMapping(typeof(int)) },
            { typeof(int?), new AzureTableIntTypeMapping(typeof(int?)) },
            { typeof(long), new AzureTableLongTypeMapping(typeof(long)) },
            { typeof(long?), new AzureTableLongTypeMapping(typeof(long?)) },
            { typeof(double), new AzureTableDoubleTypeMapping(typeof(double)) },
            { typeof(double?), new AzureTableDoubleTypeMapping(typeof(double?)) },
            { typeof(bool), new AzureTableBoolTypeMapping(typeof(bool)) },
            { typeof(bool?), new AzureTableBoolTypeMapping(typeof(bool?)) },
            { typeof(DateTime), new AzureTableDateTimeTypeMapping(typeof(DateTime)) },
            { typeof(DateTime?), new AzureTableDateTimeTypeMapping(typeof(DateTime?)) },
            { typeof(DateTimeOffset), new AzureTableDateTimeOffsetTypeMapping(typeof(DateTimeOffset)) },
            { typeof(DateTimeOffset?), new AzureTableDateTimeOffsetTypeMapping(typeof(DateTimeOffset?)) },
            { typeof(Guid), new AzureTableGuidTypeMapping(typeof(Guid)) },
            { typeof(Guid?), new AzureTableGuidTypeMapping(typeof(Guid?)) },
            { typeof(byte[]), new AzureTableByteArrayTypeMapping() }
        };
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override CoreTypeMapping? FindMapping(in TypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;

        if (clrType != null)
        {
            // Check for direct mappings
            if (_clrTypeMappings.TryGetValue(clrType, out var mapping))
            {
                return mapping;
            }

            // Handle enums - they'll be stored as strings in Azure Table
            if (clrType.IsEnum)
            {
                return new AzureTableEnumTypeMapping(clrType);
            }

            var underlyingType = Nullable.GetUnderlyingType(clrType);
            if (underlyingType?.IsEnum == true)
            {
                return new AzureTableEnumTypeMapping(clrType);
            }

            // Complex types will be JSON serialized
            if (!clrType.IsValueType || clrType.IsArray)
            {
                return new AzureTableJsonTypeMapping(clrType);
            }
        }

        return base.FindMapping(mappingInfo);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override CoreTypeMapping? FindMapping(IProperty property)
    {
        return FindMapping(property.ClrType);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override CoreTypeMapping? FindMapping(IElementType elementType)
        => FindMapping(elementType.ClrType);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override CoreTypeMapping? FindMapping(Type type)
        => FindMapping(type, model: null);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override CoreTypeMapping? FindMapping(Type type, IModel? model, CoreTypeMapping? elementMapping = null)
    {
        return FindMapping(new TypeMappingInfo(
            type: type,
            elementTypeMapping: elementMapping));
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override CoreTypeMapping? FindMapping(MemberInfo member)
        => FindMapping(member.GetMemberType());
}