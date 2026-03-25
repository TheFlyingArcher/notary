using System;

namespace Notary.Data.Model;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CollectionAttribute : Attribute
{
    // See the attribute guidelines at 
    //  http://go.microsoft.com/fwlink/?LinkId=85236

    /// <summary>
    ///     Specifies the name of a collection to use on a model
    /// </summary>
    /// <param name="name">The name of the collection</param>
    public CollectionAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}