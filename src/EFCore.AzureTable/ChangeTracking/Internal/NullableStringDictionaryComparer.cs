// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Microsoft.EntityFrameworkCore.AzureTable.ChangeTracking.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public sealed class NullableStringDictionaryComparer<TElement, TCollection> : ValueComparer<TCollection>
    where TCollection : IDictionary<string, TElement?>
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public NullableStringDictionaryComparer(ValueComparer elementComparer)
        : base(
            (a, b) => Compare(a, b, elementComparer),
            o => GetHashCode(o, elementComparer),
            source => Snapshot(source, elementComparer)!)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Type Type => typeof(TCollection);

    private static bool Compare(TCollection? a, TCollection? b, ValueComparer elementComparer)
    {
        if (a is null)
        {
            return b is null;
        }

        if (b is null || a.Count != b.Count)
        {
            return false;
        }

        foreach (var pair in a)
        {
            if (!b.TryGetValue(pair.Key, out var value))
            {
                return false;
            }

            if (!elementComparer.Equals(pair.Value, value))
            {
                return false;
            }
        }

        return true;
    }

    private static int GetHashCode(TCollection? source, ValueComparer elementComparer)
    {
        if (source is null)
        {
            return 0;
        }
        
        var hash = new HashCode();

        foreach (var pair in source.OrderBy(p => p.Key))
        {
            hash.Add(pair.Key);
            hash.Add(pair.Value!, elementComparer);
        }

        return hash.ToHashCode();
    }

    private static TCollection? Snapshot(TCollection? source, ValueComparer elementComparer)
    {
        if (source is null)
        {
            return default;
        }
        
        var snapshot = (source is ICloneable cloneable)
            ? (TCollection)cloneable.Clone()
            : (TCollection)Activator.CreateInstance(source.GetType())!;

        foreach (var pair in source)
        {
            snapshot[pair.Key] = pair.Value is ICloneable cloneableValue
                ? (TElement?)cloneableValue.Clone()
                : (TElement?)elementComparer.Snapshot(pair.Value);
        }

        return snapshot;
    }
}