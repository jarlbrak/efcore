// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.AzureTable.Infrastructure.Internal;

#pragma warning disable EF1001 // Internal EF Core API usage.

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableModelRuntimeInitializer : ModelRuntimeInitializer
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableModelRuntimeInitializer(
        ModelRuntimeInitializerDependencies dependencies,
        AzureTableModelRuntimeInitializerDependencies azureTableDependencies)
        : base(dependencies)
    {
        AzureTableDependencies = azureTableDependencies;
    }

    /// <summary>
    ///     Dependencies for this service.
    /// </summary>
    protected virtual AzureTableModelRuntimeInitializerDependencies AzureTableDependencies { get; }

    /// <inheritdoc />
    protected override void InitializeModel(IModel model, bool designTime, bool prevalidation)
    {
        if (!prevalidation && model is Model mutableModel)
        {
            foreach (var entityType in mutableModel.GetEntityTypes())
            {
                // Add dictionary comparers for collection properties
                foreach (var property in entityType.GetProperties())
                {
                    var propertyType = property.ClrType;
                    
                    if (propertyType.IsGenericType)
                    {
                        var genericTypeDefinition = propertyType.GetGenericTypeDefinition();
                        
                        // Check for Dictionary<string, T> types
                        if (genericTypeDefinition == typeof(Dictionary<,>) ||
                            genericTypeDefinition == typeof(IDictionary<,>) ||
                            genericTypeDefinition == typeof(IReadOnlyDictionary<,>))
                        {
                            var genericArgs = propertyType.GetGenericArguments();
                            if (genericArgs[0] == typeof(string))
                            {
                                var elementType = genericArgs[1];
                                var elementComparer = GetValueComparer(elementType);
                                
                                if (elementComparer != null)
                                {
                                    ValueComparer comparer;
                                    if (elementType.IsValueType && Nullable.GetUnderlyingType(elementType) == null)
                                    {
                                        // Non-nullable value type
                                        var comparerType = typeof(StringDictionaryComparer<,>)
                                            .MakeGenericType(propertyType, elementType);
                                        comparer = (ValueComparer)Activator.CreateInstance(comparerType, elementComparer)!;
                                    }
                                    else
                                    {
                                        // Reference type or nullable value type
                                        var comparerType = typeof(NullableStringDictionaryComparer<,>)
                                            .MakeGenericType(elementType, propertyType);
                                        comparer = (ValueComparer)Activator.CreateInstance(comparerType, elementComparer)!;
                                    }
                                    
                                    ((Property)property).SetValueComparer(comparer, ConfigurationSource.Convention);
                                }
                            }
                        }
                    }
                }
            }
        }

        base.InitializeModel(model, designTime, prevalidation);
    }

    private static ValueComparer? GetValueComparer(Type type)
    {
        // Return appropriate value comparer for the element type
        if (type == typeof(string))
        {
            return new ValueComparer<string>(
                (l, r) => string.Equals(l, r, StringComparison.Ordinal),
                v => v.GetHashCode(),
                v => v);
        }
        
        if (type.IsValueType)
        {
            var comparerType = typeof(ValueComparer<>).MakeGenericType(type);
            return (ValueComparer)Activator.CreateInstance(comparerType)!;
        }
        
        // For complex types, we'll need type mapping
        return null;
    }
}

#pragma warning restore EF1001 // Internal EF Core API usage.