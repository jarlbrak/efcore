// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Resources;

namespace Microsoft.EntityFrameworkCore.AzureTable.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static class AzureTableStrings
    {
        private static readonly ResourceManager _resourceManager
            = new ResourceManager("Microsoft.EntityFrameworkCore.AzureTable.Properties.AzureTableStrings", typeof(AzureTableStrings).Assembly);

        /// <summary>
        ///     Azure Table Storage connection must be configured. Provide either a connection string, account name and key, or service URI with SAS token.
        /// </summary>
        public static string NoConnectionConfiguration
            => GetString("NoConnectionConfiguration");

        /// <summary>
        ///     Azure Table Storage does not support relationships between entities. Navigation properties must be ignored or handled client-side.
        /// </summary>
        public static string RelationshipsNotSupported
            => GetString("RelationshipsNotSupported");

        /// <summary>
        ///     Complex types are not directly supported by Azure Table Storage. Consider serializing to JSON or using primitive properties.
        /// </summary>
        public static string ComplexTypesNotSupported
            => GetString("ComplexTypesNotSupported");

        /// <summary>
        ///     Collection properties are not supported by Azure Table Storage. Consider storing as serialized JSON or using separate entities.
        /// </summary>
        public static string CollectionsNotSupported
            => GetString("CollectionsNotSupported");

        /// <summary>
        ///     Azure Table entities require a PartitionKey property. Configure using HasPartitionKey() or use the [PartitionKey] attribute.
        /// </summary>
        public static string PartitionKeyRequired
            => GetString("PartitionKeyRequired");

        /// <summary>
        ///     Azure Table entities require a RowKey property. Configure using HasRowKey() or use the [RowKey] attribute.
        /// </summary>
        public static string RowKeyRequired
            => GetString("RowKeyRequired");

        /// <summary>
        ///     Cross-partition queries may have poor performance. Consider filtering by PartitionKey when possible.
        /// </summary>
        public static string CrossPartitionQueriesWarning
            => GetString("CrossPartitionQueriesWarning");

        /// <summary>
        ///     LINQ operator '{operator}' is not supported by Azure Table Storage. Only basic filtering, ordering, and projection are supported.
        /// </summary>
        public static string UnsupportedLinqOperator(object? @operator)
            => string.Format(
                GetString("UnsupportedLinqOperator"),
                @operator);

        /// <summary>
        ///     Entity type '{entityType}' is missing a partition key. All Azure Table entities must have a partition key configured using HasPartitionKey() or the [PartitionKey] attribute.
        /// </summary>
        public static string MissingPartitionKey(object? entityType)
            => string.Format(
                GetString("MissingPartitionKey"),
                entityType);

        /// <summary>
        ///     Entity type '{entityType}' is missing a row key. All Azure Table entities must have a row key configured using HasRowKey() or the [RowKey] attribute.
        /// </summary>
        public static string MissingRowKey(object? entityType)
            => string.Format(
                GetString("MissingRowKey"),
                entityType);

        /// <summary>
        ///     The partition key property '{propertyName}' on entity type '{entityType}' must be of type string, but was '{typeName}'.
        /// </summary>
        public static string InvalidPartitionKeyType(object? propertyName, object? entityType, object? typeName)
            => string.Format(
                GetString("InvalidPartitionKeyType"),
                propertyName, entityType, typeName);

        /// <summary>
        ///     The row key property '{propertyName}' on entity type '{entityType}' must be of type string, but was '{typeName}'.
        /// </summary>
        public static string InvalidRowKeyType(object? propertyName, object? entityType, object? typeName)
            => string.Format(
                GetString("InvalidRowKeyType"),
                propertyName, entityType, typeName);

        /// <summary>
        ///     The same property '{propertyName}' on entity type '{entityType}' cannot be used as both partition key and row key.
        /// </summary>
        public static string SamePropertyPartitionAndRowKey(object? propertyName, object? entityType)
            => string.Format(
                GetString("SamePropertyPartitionAndRowKey"),
                propertyName, entityType);

        /// <summary>
        ///     Property '{propertyName}' on entity type '{entityType}' has type '{typeName}' which is not supported by Azure Table Storage.
        /// </summary>
        public static string UnsupportedPropertyType(object? propertyName, object? entityType, object? typeName)
            => string.Format(
                GetString("UnsupportedPropertyType"),
                propertyName, entityType, typeName);

        /// <summary>
        ///     Property '{propertyName}' on entity type '{entityType}' has complex type '{typeName}' which will be JSON serialized. This may impact query performance and functionality.
        /// </summary>
        public static string ComplexTypeWillBeJsonSerialized(object? propertyName, object? entityType, object? typeName)
            => string.Format(
                GetString("ComplexTypeWillBeJsonSerialized"),
                propertyName, entityType, typeName);

        private static string GetString(string name, params string[] formatterNames)
        {
            var value = _resourceManager.GetString(name) ?? name;
            return value;
        }
    }
}