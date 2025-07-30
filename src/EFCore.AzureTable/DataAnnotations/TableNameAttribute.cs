// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.EntityFrameworkCore.AzureTable.DataAnnotations;

/// <summary>
///     Specifies the Azure Table Storage table name for the entity type.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class TableNameAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TableNameAttribute" /> class.
    /// </summary>
    /// <param name="name">The name of the table in Azure Table Storage.</param>
    public TableNameAttribute(string name)
    {
        Check.NotNull(name, nameof(name));
        Name = name;
    }

    /// <summary>
    ///     Gets the name of the table in Azure Table Storage.
    /// </summary>
    public string Name { get; }
}