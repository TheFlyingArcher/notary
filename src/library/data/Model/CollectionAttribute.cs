using System;

namespace Notary.Data.Model
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CollectionAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string collectionName;

        /// <summary>
        /// Specifies the name of a collection to use on a model
        /// </summary>
        /// <param name="name">The name of the collection</param>
        public CollectionAttribute(string name)
        {
            this.collectionName = name;
        }

        public string Name
        {
            get { return collectionName; }
        }
    }
}
