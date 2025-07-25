namespace LinqToWiki.Codegen.ModuleInfo
{
    /// <summary>
    /// A simple or primitive type represented just by its name.
    /// </summary>
    internal class SimpleParameterType : ParameterType
    {
        /// <summary>
        /// Name of the type.
        /// </summary>
        public string Name { get; private set; }

        public SimpleParameterType(string name)
        {
            Name = name;
        }

        public override bool Equals(ParameterType other)
        {
            if (!(other is SimpleParameterType otherSimple))
            {
                return false;
            }

            return this.Name == otherSimple.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as ParameterType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}