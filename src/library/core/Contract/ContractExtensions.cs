namespace Notary.Contract
{
    public static class ContractExtensions
    {
        /// <summary>
        /// Make the slug for the entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>The slug based on the defined parameters of the contract</returns>
        public static string Slugify(this Entity entity)
        {
            return entity.SlugProperties().MakeSlug();
        }
    }
}
