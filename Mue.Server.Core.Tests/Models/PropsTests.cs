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
        Assert.Null(actual.ObjectIdValue);
        Assert.True(actual.NumberValue.HasValue);
        Assert.Equal(expected, actual.NumberValue.Value);
        Assert.Empty(actual.ListValue);
    }

    [Fact]
    public void ConstructorValidString()
    {
        var expected = "test";
        var actual = new PropValue(expected);
        Assert.False(actual.IsNull);
        Assert.Equal(PropValueType.String, actual.ValueType);
        Assert.Equal(expected, actual.StringValue);
        Assert.Null(actual.ObjectIdValue);
        Assert.False(actual.NumberValue.HasValue);
        Assert.Empty(actual.ListValue);
    }

    [Fact]
    public void ConstructorValidObjectId()
    {
        var expected = new ObjectId("p:0");
        var actual = new PropValue(expected);
        Assert.False(actual.IsNull);
        Assert.Equal(PropValueType.ObjectId, actual.ValueType);
        Assert.Null(actual.StringValue);
        Assert.Equal(expected, actual.ObjectIdValue);
        Assert.False(actual.NumberValue.HasValue);
        Assert.Empty(actual.ListValue);
    }

    [Fact]
    public void ConstructorValidList()
    {
        var expected = new List<FlatPropValue>() { new FlatPropValue("a"), new FlatPropValue(2), new FlatPropValue(new ObjectId("p:0")) };
        var actual = new PropValue(expected);
        Assert.False(actual.IsNull);
        Assert.Equal(PropValueType.List, actual.ValueType);
        Assert.Null(actual.StringValue);
        Assert.Null(actual.ObjectIdValue);
        Assert.Null(actual.NumberValue);
        Assert.NotEmpty(actual.ListValue);
        Assert.Equal(3, actual.ListValue.Count());
        Assert.Contains(actual.ListValue, p => p.StringValue == "a");
        Assert.Contains(actual.ListValue, p => p.NumberValue == 2);
        Assert.Contains(actual.ListValue, p => p.ObjectIdValue == new ObjectId("p:0"));
    }

    [Fact]
    public void EqualsWithNull()
    {
        var prop1 = new PropValue();
        var prop2 = new PropValue();
        Assert.Equal(prop1, prop2);
    }

    [Fact]
    public void EqualsWithString()
    {
        var prop1 = new PropValue("a");
        var prop2 = new PropValue("a");
        Assert.Equal(prop1, prop2);
    }

    [Fact]
    public void EqualsWithNumber()
    {
        var prop1 = new PropValue(2);
        var prop2 = new PropValue(2);
        Assert.Equal(prop1, prop2);
    }

    [Fact]
    public void EqualsWithObjectId()
    {
        var prop1 = new PropValue(new ObjectId("p:0"));
        var prop2 = new PropValue(new ObjectId("p:0"));
        Assert.Equal(prop1, prop2);
    }

    [Fact]
    public void EqualsWithList()
    {
        var prop1 = new PropValue(new[] { new FlatPropValue("a"), new FlatPropValue(2), new FlatPropValue(new ObjectId("p:0")) });
        var prop2 = new PropValue(new[] { new FlatPropValue("a"), new FlatPropValue(2), new FlatPropValue(new ObjectId("p:0")) });
        Assert.Equal(prop1, prop2);
    }

    [Fact]
    public void EqualsWithEmptyList()
    {
        var prop = PropValue.FromJsonString("[]");
        Assert.Equal(prop, PropValue.EmptyList);
    }

    [Fact]
    public void NotEqualsWithWrongStrings()
    {
        var prop1 = new PropValue("a");
        var prop2 = new PropValue("b");
        Assert.NotEqual(prop1, prop2);
    }

    [Fact]
    public void NotEqualsWithWrongTypes()
    {
        var prop1 = new PropValue("a");
        var prop2 = new PropValue(2);
        Assert.NotEqual(prop1, prop2);
    }

    [Fact]
    public void NotEqualsWithWrongListStrings()
    {
        var prop1 = new PropValue(new[] { new FlatPropValue("a") });
        var prop2 = new PropValue(new[] { new FlatPropValue("b") });
        Assert.NotEqual(prop1, prop2);
    }

    [Fact]
    public void NotEqualsWithWrongListTypes()
    {
        var prop1 = new PropValue(new[] { new FlatPropValue("a") });
        var prop2 = new PropValue(new[] { new FlatPropValue(2) });
        Assert.NotEqual(prop1, prop2);
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
    public void ProperlyJsonSerializesObjectId()
    {
        var prop = new PropValue(new ObjectId("p:0"));
        var actual = prop.ToJsonString();
        Assert.Equal("\"_oid:p:0\"", actual);
    }

    [Fact]
    public void ProperlyJsonSerializesList()
    {
        var list = new List<FlatPropValue>() { new FlatPropValue("a"), new FlatPropValue(2), new FlatPropValue(new ObjectId("p:0")) };
        var prop = new PropValue(list);
        var actual = prop.ToJsonString();
        Assert.Equal("[\"a\",2,\"_oid:p:0\"]", actual);
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
    public void ProperlyJsonDeserializesObjectId()
    {
        var expected = new PropValue(new ObjectId("p:0"));
        var actual = PropValue.FromJsonString("\"_oid:p:0\"");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProperlyJsonDeserializesList()
    {
        var actual = PropValue.FromJsonString("[\"a\",2,\"_oid:p:0\"]");
        Assert.Equal(PropValueType.List, actual.ValueType);
        Assert.Equal(3, actual.ListValue.Count());
        Assert.Contains(actual.ListValue, p => p.StringValue == "a");
        Assert.Contains(actual.ListValue, p => p.NumberValue == 2);
        Assert.Contains(actual.ListValue, p => p.ObjectIdValue == new ObjectId("p:0"));
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