using System.IO;

namespace LinqToWiki.Download
{
    public abstract class HttpQueryParameterBase
    {
        public string Name { get; private set; }

        public HttpQueryParameterBase(string name)
        {
            Name = name;
        }
    }

    public class HttpQueryParameter : HttpQueryParameterBase
    {
        public string Value { get; private set; }

        public HttpQueryParameter(string name, string value)
            : base(name)
        {
            Value = value;
        }

        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }

    public class HttpQueryFileParameter : HttpQueryParameterBase
    {
        public Stream File
        {
            get
            {
                if (m_memoryStream == null)
                {
                    m_memoryStream = new MemoryStream();
                    m_file.CopyTo(m_memoryStream);
                }

                m_memoryStream.Position = 0; // Reset the position for reading
                return m_memoryStream;
            }
        }

        private readonly Stream m_file;
        private MemoryStream m_memoryStream;

        public HttpQueryFileParameter(string name, Stream file)
            : base(name)
        {
            m_file = file;
        }

        public override string ToString()
        {
            return $"{Name}=<file>";
        }
    }
}