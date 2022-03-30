using System;
using System.Collections.Generic;
using System.Linq;
using Mue.Server.Core.Models;
using Xunit;

public class PropsTests
{
    [Fact]
    public void ConstructorValidNull()
    {
        var actual = new PropValue();
        Assert.True(actual.IsNull);
    }

    [Fact]
    public void ConstructorValidNumber()
    {
        var expected = 3;
        var actual = new PropValue(expected);
        Assert.False(actual.IsNull);
        Assert.Equal(PropValueType.Number, actual.ValueType);
        Assert.Null(actual.StringValue);
        Assert.True(actual.NumberValue.HasValue);
        Assert.Equal(expected, actual.NumberValue.Value);
        Assert.Null(actual.ListValue);
    }

    [Fact]
    public void ConstructorValidString()
    {
        var expected = "test";
        var actual = new PropValue(expected);
        Assert.False(actual.IsNull);
        Assert.Equal(PropValueType.String, actual.ValueType);
        Assert.Equal(expected, actual.StringValue);
        Assert.False(actual.NumberValue.HasValue);
        Assert.Null(actual.ListValue);
    }

    [Fact]
    public void ConstructorValidList()
    {
        var expected = new List<FlatPropValue>() { new FlatPropValue("a"), new FlatPropValue(2) };
        var actual = new PropValue(expected);
        Assert.False(actual.IsNull);
        Assert.Equal(PropValueType.List, actual.ValueType);
        Assert.Null(actual.StringValue);
        Assert.False(actual.NumberValue.HasValue);
        Assert.Equal(2, actual.ListValue.Count());
        Assert.Contains(actual.ListValue, p => p.StringValue == "a");
        Assert.Contains(actual.ListValue, p => p.NumberValue == 2);
    }

    [Fact]
    public void ProperlyJsonSerializesNull()
    {
        var prop = new PropValue();
        var actual = prop.ToJsonString();
        Assert.Null(actual);
    }

    [Fact]
    public void ProperlyJsonSerializesNumber()
    {
        var prop = new PropValue(3);
        var actual = prop.ToJsonString();
        Assert.Equal("3", actual);
    }

    [Fact]
    public void ProperlyJsonSerializesString()
    {
        var prop = new PropValue("3");
        var actual = prop.ToJsonString();
        Assert.Equal("\"3\"", actual);
    }

    [Fact]
    public void ProperlyJsonSerializesList()
    {
        var list = new List<FlatPropValue>() { new FlatPropValue("a"), new FlatPropValue(2) };
        var prop = new PropValue(list);
        var actual = prop.ToJsonString();
        Assert.Equal("[\"a\",2]", actual);
    }

    [Fact]
    public void ProperlyJsonDeserializesNull()
    {
        var expected = new PropValue();
        var actual = PropValue.FromJsonString("null");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProperlyJsonDeserializesNullType()
    {
        var expected = new PropValue();
        var actual = PropValue.FromJsonString(null);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProperlyJsonDeserializesNumber()
    {
        var expected = new PropValue(3);
        var actual = PropValue.FromJsonString("3");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProperlyJsonDeserializesString()
    {
        var expected = new PropValue("3");
        var actual = PropValue.FromJsonString("\"3\"");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProperlyJsonDeserializesList()
    {
        var actual = PropValue.FromJsonString("[\"a\",2]");
        Assert.Equal(PropValueType.List, actual.ValueType);
        Assert.Equal(2, actual.ListValue.Count());
        Assert.Contains(actual.ListValue, p => p.StringValue == "a");
        Assert.Contains(actual.ListValue, p => p.NumberValue == 2);
    }

    [Fact]
    public void ThrowsOnInvalidRootJson()
    {
        Func<PropValue> actual = () => PropValue.FromJsonString("{}");
        Assert.Throws<Exception>(actual);
    }

    [Fact]
    public void ThrowsOnInvalidListJson()
    {
        Func<PropValue> actual = () => PropValue.FromJsonString("[{}]");
        Assert.Throws<Exception>(actual);
    }

    [Fact]
    public void ThrowsOnNestedListJson()
    {
        Func<PropValue> actual = () => PropValue.FromJsonString("[[]]");
        Assert.Throws<Exception>(actual);
    }
}