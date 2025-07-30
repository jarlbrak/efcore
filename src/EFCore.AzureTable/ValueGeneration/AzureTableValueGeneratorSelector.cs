// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.AzureTable.ValueGeneration;

/// <summary>
///     Selects value generators for properties of entities to be saved to Azure Table Storage.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-value-generation">Value generation</see> and
///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
public class AzureTableValueGeneratorSelector : ValueGeneratorSelector
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AzureTableValueGeneratorSelector" /> class.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this service.</param>
    public AzureTableValueGeneratorSelector(ValueGeneratorSelectorDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     Selects the appropriate value generator for a given property.
    /// </summary>
    /// <param name="property">The property to get the value generator for.</param>
    /// <param name="typeBase">The entity type or complex type.</param>
    /// <param name="clrType">The CLR type of the property.</param>
    /// <returns>The value generator to be used.</returns>
    protected override ValueGenerator? FindForType(IProperty property, ITypeBase typeBase, Type clrType)
    {
        // Handle timestamp properties
        if (property.IsTimestamp())
        {
            return new AzureTableTimestampValueGenerator();
        }

        // Handle ETag properties
        if (property.IsETag())
        {
            return new AzureTableETagValueGenerator();
        }

        // Handle RowKey generation if not set
        if (property.IsRowKey() && property.ValueGenerated == ValueGenerated.OnAdd)
        {
            if (clrType == typeof(string))
            {
                return new AzureTableRowKeyValueGenerator();
            }
        }

        return base.FindForType(property, typeBase, clrType);
    }
}