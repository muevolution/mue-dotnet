using System;
using Mue.Server.Core.Utils;
using Xunit;

public class SecurityTests
{
    [Fact]
    public void HashPasswordHashesPassword()
    {
        var actual = Security.HashPassword("samplepassword");
        Assert.NotEqual("samplepassword", actual);
    }

    [Fact]
    public void ComparePasswordTrueOnMatch()
    {
        var hash = Security.HashPassword("samplepassword");
        var actual = Security.ComparePasswords(hash, "samplepassword");
        Assert.True(actual);
    }

    [Fact]
    public void ComparePasswordsFalseOnUnmatch()
    {
        var hash = Security.HashPassword("samplepassword");
        var actual = Security.ComparePasswords(hash, "differentpassword");
        Assert.False(actual);
    }
}