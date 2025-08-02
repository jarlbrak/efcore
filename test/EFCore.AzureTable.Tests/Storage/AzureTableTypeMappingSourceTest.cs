// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Microsoft.EntityFrameworkCore.AzureTable.Storage;

public class AzureTableTypeMappingSourceTest
{
    private readonly AzureTableTypeMappingSource _typeMappingSource;

    public AzureTableTypeMappingSourceTest()
    {
        var options = new DbContextOptionsBuilder().Options;
        _typeMappingSource = new AzureTableTypeMappingSource(
            new TypeMappingSourceDependencies(
                new ValueConverterSelector(new ValueConverterSelectorDependencies()), 
                new JsonValueReaderWriterSource(new JsonValueReaderWriterSourceDependencies()),
                Array.Empty<ITypeMappingSourcePlugin>()),
            options);
    }

    [Theory]
    [InlineData(typeof(string), "String")]
    [InlineData(typeof(int), "Int32")]
    [InlineData(typeof(long), "Int64")]
    [InlineData(typeof(double), "Double")]
    [InlineData(typeof(bool), "Boolean")]
    [InlineData(typeof(DateTime), "DateTime")]
    [InlineData(typeof(DateTimeOffset), "DateTimeOffset")]
    [InlineData(typeof(Guid), "Guid")]
    [InlineData(typeof(byte[]), "Binary")]
    public void Maps_primitive_types_correctly(Type clrType, string expectedStoreType)
    {
        var mapping = _typeMappingSource.FindMapping(clrType);

        Assert.NotNull(mapping);
        Assert.Equal(expectedStoreType, mapping.StoreType);
    }

    [Theory]
    [InlineData(typeof(int?))]
    [InlineData(typeof(long?))]
    [InlineData(typeof(double?))]
    [InlineData(typeof(bool?))]
    [InlineData(typeof(DateTime?))]
    [InlineData(typeof(DateTimeOffset?))]
    [InlineData(typeof(Guid?))]
    public void Maps_nullable_primitive_types_correctly(Type clrType)
    {
        var mapping = _typeMappingSource.FindMapping(clrType);

        Assert.NotNull(mapping);
    }

    [Fact]
    public void Maps_complex_types_to_json()
    {
        var mapping = _typeMappingSource.FindMapping(typeof(ComplexType));

        Assert.NotNull(mapping);
        Assert.IsType<AzureTableJsonTypeMapping>(mapping);
        Assert.Equal("String", mapping.StoreType);
    }

    [Fact]
    public void Maps_enum_types_to_string()
    {
        var mapping = _typeMappingSource.FindMapping(typeof(TestEnum));

        Assert.NotNull(mapping);
        Assert.Equal("String", mapping.StoreType);
    }

    [Fact]
    public void Throws_for_unsupported_collection_types()
    {
        Assert.Throws<NotSupportedException>(() =>
            _typeMappingSource.FindMapping(typeof(List<string>)));

        Assert.Throws<NotSupportedException>(() =>
            _typeMappingSource.FindMapping(typeof(string[])));

        Assert.Throws<NotSupportedException>(() =>
            _typeMappingSource.FindMapping(typeof(Dictionary<string, object>)));
    }

    [Fact]
    public void Json_type_mapping_serializes_and_deserializes_correctly()
    {
        var mapping = _typeMappingSource.FindMapping(typeof(ComplexType)) as AzureTableJsonTypeMapping;
        Assert.NotNull(mapping);

        var originalObject = new ComplexType { Name = "Test", Value = 42 };
        
        var serialized = mapping.ConvertToProvider(originalObject);
        Assert.IsType<string>(serialized);
        
        var deserialized = mapping.ConvertFromProvider(serialized) as ComplexType;
        Assert.NotNull(deserialized);
        Assert.Equal(originalObject.Name, deserialized.Name);
        Assert.Equal(originalObject.Value, deserialized.Value);
    }

    [Fact]
    public void Json_type_mapping_handles_null_values()
    {
        var mapping = _typeMappingSource.FindMapping(typeof(ComplexType)) as AzureTableJsonTypeMapping;
        Assert.NotNull(mapping);

        var serialized = mapping.ConvertToProvider(null);
        Assert.Null(serialized);
        
        var deserialized = mapping.ConvertFromProvider(null);
        Assert.Null(deserialized);
    }

    [Fact]
    public void String_type_mapping_respects_max_length()
    {
        var mapping = _typeMappingSource.FindMapping(typeof(string), storeTypeName: null, keyOrIndex: false, unicode: true, maxLength: 100);

        Assert.NotNull(mapping);
        Assert.Equal(100, mapping.Size);
    }

    [Fact]
    public void Binary_type_mapping_respects_max_length()
    {
        var mapping = _typeMappingSource.FindMapping(typeof(byte[]), storeTypeName: null, keyOrIndex: false, unicode: null, maxLength: 1024);

        Assert.NotNull(mapping);
        Assert.Equal(1024, mapping.Size);
    }

    [Fact]
    public void DateTime_values_are_converted_to_utc()
    {
        var mapping = _typeMappingSource.FindMapping(typeof(DateTime));
        Assert.NotNull(mapping);

        var localDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var converted = mapping.ConvertToProvider(localDateTime);
        
        Assert.IsType<DateTime>(converted);
        var convertedDateTime = (DateTime)converted;
        Assert.Equal(DateTimeKind.Utc, convertedDateTime.Kind);
    }

    private class ComplexType
    {
        public string Name { get; set; } = null!;
        public int Value { get; set; }
    }

    private enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }
}