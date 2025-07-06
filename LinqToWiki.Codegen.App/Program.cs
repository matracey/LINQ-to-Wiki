using LinqToWiki.Download;
using System;
using System.CodeDom.Compiler;
using System.IO;

namespace LinqToWiki.Codegen.App;

internal static class Program
{
    private static void Main(string[] args)
    {
        Downloader.LogDownloading = true;

        if (args.Length == 0)
        {
            Usage();
            return;
        }
        try
        {
            Run(args);
        }
        catch (ArgumentException e)
        {
            Console.WriteLine(e);
            Usage();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static void Usage()
    {
        var applicationName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
        Console.WriteLine($"Usage:    {applicationName} url-to-api [namespace [output-name]] [-d output-directory] [-p props-file-path]");
        Console.WriteLine($@"Examples: {applicationName} en.wikipedia.org LinqToWiki.Enwiki linqtowiki-enwiki -d C:\Temp -p props-defaults-sample.xml");
        Console.WriteLine($"          {applicationName} https://en.wikipedia.org/w/api.php");
    }

    private static void Run(string[] args)
    {
        Arguments arguments;
        try
        {
            arguments = Arguments.Parse(args);
        }
        catch (Exception e)
        {
            throw new ArgumentException($"Error parsing arguments: {e.Message}", e);
        }

        var urlString = arguments.Url;

        if (urlString == null)
        {
            throw new ArgumentException("Url to API has to be specified.");
        }

        if (!urlString.StartsWith("http"))
        {
            urlString = "http://" + urlString;
        }

        var url = new Uri(urlString, UriKind.Absolute);
        var baseUrl = url.GetLeftPart(UriPartial.Authority);
        var apiPath = url.AbsolutePath;

        if (apiPath == "/")
        {
            apiPath = "/w/api.php";
        }

        var wiki = new Wiki(baseUrl, apiPath, arguments.Namespace, arguments.PropsFile);
        wiki.AddAllModules();
        wiki.AddAllQueryModules();
        var result = wiki.Compile(arguments.OutputName, arguments.Directory);

        foreach (CompilerError error in result.Errors)
        {
            Console.WriteLine(error);
        }
    }
}