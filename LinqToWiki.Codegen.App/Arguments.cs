namespace LinqToWiki.Codegen.App;

internal class Arguments
{
    private readonly ArgumentsCollection m_argumentsCollection;

    private Arguments(ArgumentsCollection argumentsCollection)
    {
        m_argumentsCollection = argumentsCollection;
    }

    public static Arguments Parse(string[] args)
    {
        var argumentsCollection = ArgumentsCollection.Parse(args);
        return new Arguments(argumentsCollection);
    }

    public string Url => m_argumentsCollection[0];

    public string Namespace => m_argumentsCollection[1] ?? "LinqToWiki.Generated";

    public string OutputName => m_argumentsCollection[2] ?? Namespace;

    public string Directory => m_argumentsCollection['d'] ?? string.Empty;

    public string PropsFile => m_argumentsCollection['p'];
}