// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.EntityFrameworkCore.AzureTable.DataAnnotations;

/// <summary>
///     Specifies that the property should be used as the partition key in Azure Table Storage.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class PartitionKeyAttribute : Attribute
{
}