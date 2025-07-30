// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Metadata;
using Microsoft.EntityFrameworkCore.Design.Internal;

namespace Microsoft.EntityFrameworkCore.AzureTable.Design.Internal;

#pragma warning disable EF1001 // Internal EF Core API usage.

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableCSharpRuntimeAnnotationCodeGenerator : CSharpRuntimeAnnotationCodeGenerator
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableCSharpRuntimeAnnotationCodeGenerator(
        CSharpRuntimeAnnotationCodeGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <inheritdoc />
    public override void Generate(IModel model, CSharpRuntimeAnnotationCodeGeneratorParameters parameters)
    {
        if (!parameters.IsRuntime)
        {
            parameters.Annotations.Remove(AzureTableAnnotationNames.TableName);
        }

        base.Generate(model, parameters);
    }

    /// <inheritdoc />
    public override void Generate(IEntityType entityType, CSharpRuntimeAnnotationCodeGeneratorParameters parameters)
    {
        if (!parameters.IsRuntime)
        {
            parameters.Annotations.Remove(AzureTableAnnotationNames.TableName);
            parameters.Annotations.Remove(AzureTableAnnotationNames.PartitionKey);
            parameters.Annotations.Remove(AzureTableAnnotationNames.RowKey);
            parameters.Annotations.Remove(AzureTableAnnotationNames.ETag);
            parameters.Annotations.Remove(AzureTableAnnotationNames.Timestamp);
        }

        base.Generate(entityType, parameters);
    }

    /// <inheritdoc />
    public override void Generate(IProperty property, CSharpRuntimeAnnotationCodeGeneratorParameters parameters)
    {
        if (!parameters.IsRuntime)
        {
            parameters.Annotations.Remove(AzureTableAnnotationNames.PartitionKey);
            parameters.Annotations.Remove(AzureTableAnnotationNames.RowKey);
            parameters.Annotations.Remove(AzureTableAnnotationNames.ETag);
            parameters.Annotations.Remove(AzureTableAnnotationNames.Timestamp);
        }

        base.Generate(property, parameters);
    }
}

#pragma warning restore EF1001 // Internal EF Core API usage.