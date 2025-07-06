using System;
using System.Collections.Generic;

namespace LinqToWiki.Codegen.App;

internal class ArgumentsCollection
{
    private readonly List<string> m_positionalArguments = [];
    private readonly Dictionary<char, string> m_namedArguments = [];

    private ArgumentsCollection()
    {
    }

    public static ArgumentsCollection Parse(string[] args)
    {
        var result = new ArgumentsCollection();

        var i = 0;
        while (i < args.Length)
        {
            var arg = args[i];
            if (arg.StartsWith("-"))
            {
                if (arg.Length != 2)
                {
                    throw new InvalidOperationException($"Invalid argument: '{arg}'.");
                }

                var key = arg[1];
                var value = args[++i];
                result.m_namedArguments.Add(key, value);
            }
            else
            {
                result.m_positionalArguments.Add(arg);
            }

            i++;
        }

        return result;
    }

    public string this[int i] => m_positionalArguments.Count <= i ? null : m_positionalArguments[i];

    public string this[char c]
    {
        get
        {
            m_namedArguments.TryGetValue(c, out string result);
            return result;
        }
    }
}