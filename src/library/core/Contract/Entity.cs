using System;

using Newtonsoft.Json;

namespace Notary.Contract
{
    public abstract class Entity : IComparable<Entity>, IEquatable<Entity>, ISluggable
    {
        protected Entity()
        {
            Slug = string.Empty;
            CreatedBySlug = string.Empty;
            UpdatedBySlug = string.Empty;
        }

        /// <summary>
        /// Get or set identifying slug of the entit
        /// </summary>
        [JsonProperty("slug", Required = Required.Always)]
        public string Slug
        {
            get; set;
        }

        /// <summary>
        /// Get or set in which the entity was created
        /// </summary>
        [JsonProperty("created", Required = Required.Always)]
        public DateTime Created
        {
            get; set;
        }

        [JsonProperty("createdBySlug", Required = Required.Always)]
        public string CreatedBySlug { get; set; }

        /// <summary>
        /// Get or set the last time the entity was updated
        /// </summary>
        [JsonProperty("updated", Required = Required.AllowNull)]
        public DateTime? Updated
        {
            get; set;
        }

        [JsonProperty("updatedBySlug", Required = Required.AllowNull)]
        public string UpdatedBySlug { get; set; }

        /// <summary>
        /// Get or set whether this entity is active
        /// </summary>
        [JsonProperty("active", Required = Required.Always)]
        public bool Active
        {
            get; set;
        }

        public int CompareTo(Entity other)
        {
            var result = string.Compare(Slug, other.Slug, true);
            return result;
        }

        public bool Equals(Entity other)
        {
            if (other == null)
                return false;

            return string.Compare(Slug, other.Slug, true) == 0;
        }

        public abstract string[] SlugProperties();
    }
}