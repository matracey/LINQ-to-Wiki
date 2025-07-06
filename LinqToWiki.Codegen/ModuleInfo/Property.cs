using System;
using System.Xml.Linq;

namespace LinqToWiki.Codegen.ModuleInfo
{
    /// <summary>
    /// Property of the result of a module.
    /// </summary>
    public class Property : IEquatable<Property>
    {
        /// <summary>
        /// Name of the property
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Type of the property
        /// </summary>
        public ParameterType Type { get; internal set; }

        /// <summary>
        /// Is the property nullable?
        /// </summary>
        public bool Nullable { get; internal set; }

        /// <summary>
        /// Parses the <c>property</c> XML element.
        /// </summary>
        public static Property Parse(XElement element)
        {
            return new Property
            {
                Name = (string)element.Attribute("name"),
                Type = ParameterType.Parse(element),
                Nullable = element.Attribute("nullable") != null
            };
        }

        public override string ToString()
        {
            return $"Name: {Name}, Type: {Type}";
        }

        public bool Equals(Property other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(other.Name, Name) && Equals(other.Type, Type);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == typeof(Property) && Equals((Property)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Name.GetHashCode() * 397) ^ Type.GetHashCode();
            }
        }
    }
}