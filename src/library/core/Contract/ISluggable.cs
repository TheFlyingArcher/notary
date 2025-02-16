namespace Notary.Contract
{
    /// <summary>
    /// Marks the entity as supporting slug properties
    /// </summary>
    public interface ISluggable
    {
        /// <summary>
        /// The application key used to identify the record in the database
        /// </summary>
        string Slug { get; set; }

        /// <summary>
        /// Defines the properties for generating the slug
        /// </summary>
        /// <returns>The properties within the class to generate the slug</returns>
        string[] SlugProperties();
    }
}
