using System;

namespace Notary.Configuration;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class NotaryEnvironmentVariableAttribute : Attribute
{
    public NotaryEnvironmentVariableAttribute(string environmentVariable)
    {
        EnvironmentVariable = environmentVariable;
    }

    public string EnvironmentVariable { get; }
}
