using LinqToWiki.Internals;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace LinqToWiki.Download
{
    /// <summary>
    /// Downloads the results of a query from the wiki website.
    /// </summary>
    public class Downloader
    {
        static Downloader()
        {
            ServicePointManager.Expect100Continue = false;
            UseMaxlag = true;
        }

        /// <summary>
        /// Whether to set the <c>maxlag</c> parameter to limit queries in times of high load.
        /// See http://www.mediawiki.org/wiki/Manual:Maxlag_parameter.
        /// </summary>
        public static bool UseMaxlag { get; set; }

        /// <summary>
        /// Whether each request should be logged to the console.
        /// </summary>
        public static bool LogDownloading { get; set; }

        /// <summary>
        /// The value of the <c>User-Agent</c> header of requests.
        /// </summary>
        public string UserAgent => $"{m_wiki.UserAgent} LinqToWiki";

        private readonly WikiInfo m_wiki;
        private readonly CookieContainer m_cookies = new CookieContainer();

        public Downloader(WikiInfo wiki)
        {
            m_wiki = wiki;
        }

        /// <summary>
        /// Downloads the results of query defined by <paramref name="parameters"/>.
        /// </summary>
        public XDocument Download(IEnumerable<HttpQueryParameterBase> parameters)
        {
            parameters = parameters.ToArray();

            if (LogDownloading)
            {
                LogRequest(parameters);
            }

            parameters = new[] { new HttpQueryParameter("format", "xml") }.Concat(parameters);

            if (UseMaxlag)
            {
                parameters = parameters.Concat(new[] { new HttpQueryParameter("maxlag", "5") });
            }

            var client = new RestClient(new RestClientOptions
            {
                BaseUrl = new Uri(m_wiki.ApiUrl.AbsoluteUri + "?rawcontinue"),
                CookieContainer = m_cookies,
                UserAgent = UserAgent
            });
            var request = new RestRequest(string.Empty, Method.Post);

            WriteParameters(parameters, request);

            var response = client.Execute(request);

            return XDocument.Parse(response.Content ?? "");
        }

        /// <summary>
        /// Logs the request to the console.
        /// </summary>
        private void LogRequest(IEnumerable<HttpQueryParameterBase> parameters)
        {
            Console.WriteLine($"{m_wiki.ApiUrl}?{string.Join("&", parameters)}");
        }

        /// <summary>
        /// Writes parameters to a request.
        /// </summary>
        private static void WriteParameters(IEnumerable<HttpQueryParameterBase> parameters, RestRequest request)
        {
            foreach (var parameter in parameters)
            {
                switch (parameter)
                {
                    case HttpQueryParameter normalParameter:
                        request.AddParameter(normalParameter.Name, normalParameter.Value);
                        continue;
                    case HttpQueryFileParameter fileParameter:
                        request.AddFile(fileParameter.Name, () => fileParameter.File, "noname");
                        break;
                }
            }
        }
    }
}