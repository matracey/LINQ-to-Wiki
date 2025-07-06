﻿using LinqToWiki.Codegen.ModuleGenerators;
using LinqToWiki.Codegen.ModuleInfo;
using LinqToWiki.Collections;
using LinqToWiki.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LinqToWiki.Codegen
{
    /// <summary>
    /// Holds all information about the code being generated for a wiki.
    /// </summary>
    public class Wiki
    {
        /// <summary>
        /// Names of basic generated types.
        /// </summary>
        internal static class Names
        {
            public const string Wiki = "Wiki";
            public const string WikiInfo = "WikiInfo";
            public const string QueryAction = "QueryAction";
            public const string Enums = "Enums";
            public const string Page = "Page";
            public const string PageResult = "PageResult";
        }

        private const string Extension = ".cs";
        private const string PagesSourcePageSizePropertyName = "PagesSourcePageSize";
        private const string TypeParameterName = "TData";
        private readonly ModulesSource m_modulesSource;

        private int m_modulesFinished;

        /// <summary>
        /// The namespace of generated types that are not entities.
        /// </summary>
        internal string Namespace { get; private set; }

        /// <summary>
        /// The namespace of generated entities: types used for the select, where and orderby clauses
        /// of query modules and as result of non-query modules.
        /// </summary>
        internal string EntitiesNamespace { get; private set; }

        /// <summary>
        /// List of all source code files, along with their names.
        /// </summary>
        internal TupleList<string, CompilationUnitSyntax> Files { get; private set; }

        /// <summary>
        /// <see cref="TypeManager"/> for this wiki.
        /// </summary>
        internal TypeManager TypeManager { get; private set; }

        /// <summary>
        /// Base parameters common to non-prop query modules.
        /// </summary>
        internal static readonly TupleList<string, string> QueryBaseParameters =
            new TupleList<string, string> { { "action", "query" } };

        public Wiki(string baseUri, string apiPath, string ns = null, string propsFilePath = null)
        {
            Files = new TupleList<string, CompilationUnitSyntax>();
            TypeManager = new TypeManager(this);

            Namespace = ns ?? "LinqToWiki.Generated";
            EntitiesNamespace = Namespace + ".Entities";

            m_modulesSource = new ModulesSource(new WikiInfo("LinqToWiki.Codegen.App", baseUri, apiPath), propsFilePath);

            CreatePageClass();
            CreateWikiClass(baseUri, apiPath);
            CreateQueryActionClass();
            CreateEnumsFile();
        }

        /// <summary>
        /// Creates the <c>QueryAction</c> class that is used to access non-prop query modules.
        /// </summary>
        private void CreateQueryActionClass()
        {
            var wikiField = SyntaxEx.FieldDeclaration(
                new[] { SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword }, Names.WikiInfo, "m_wiki");

            var wikiParameter = SyntaxEx.Parameter(Names.WikiInfo, "wiki");

            var ctor = SyntaxEx.ConstructorDeclaration(
                new[] { SyntaxKind.InternalKeyword }, Names.QueryAction,
                new[] { wikiParameter },
                new StatementSyntax[] { SyntaxEx.Assignment(wikiField, wikiParameter) });

            var queryActionClass = SyntaxEx.ClassDeclaration(Names.QueryAction, wikiField, ctor);

            Files.Add(
                Names.QueryAction, SyntaxEx.CompilationUnit(
                    SyntaxEx.NamespaceDeclaration(Namespace, queryActionClass),
                    "System", "System.Collections.Generic", "LinqToWiki", "LinqToWiki.Collections",
                    "LinqToWiki.Parameters", "LinqToWiki.Internals", EntitiesNamespace));
        }

        /// <summary>
        /// Creates the <c>Page</c> class that used to access prop query modules.
        /// </summary>
        private void CreatePageClass()
        {
            var pageClass = SyntaxEx.ClassDeclaration(SyntaxKind.AbstractKeyword, Names.Page)
                .AddPrivateConstructor();

            Files.Add(
                Names.Page,
                SyntaxEx.CompilationUnit(
                    SyntaxEx.NamespaceDeclaration(Namespace, pageClass),
                    "System", "System.Collections.Generic", "LinqToWiki", "LinqToWiki.Collections",
                    "LinqToWiki.Internals", EntitiesNamespace));
        }

        /// <summary>
        /// Creates the <c>Wiki</c> class, that is used as an entry point for the whole API.
        /// It can be used to access non-query modules directly and non-prop query modules
        /// indirectly though its <c>Query</c> property.
        /// </summary>
        private void CreateWikiClass(string baseUri, string apiPath)
        {
            var wikiField = SyntaxEx.FieldDeclaration(
                new[] { SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword }, Names.WikiInfo, "m_wiki");

            var queryProperty = SyntaxEx.AutoPropertyDeclaration(
                new[] { SyntaxKind.PublicKeyword }, Names.QueryAction, "Query", SyntaxKind.PrivateKeyword);

            var wikiFieldPagesSourcePageSizeProperty = SyntaxEx.MemberAccess(wikiField, PagesSourcePageSizePropertyName);
            var pagesSourcePageSizeProperty = SyntaxEx.PropertyDeclaration(
                new[] { SyntaxKind.PublicKeyword }, SyntaxFactory.ParseTypeName("int"), PagesSourcePageSizePropertyName,
                getStatements: new[] { SyntaxEx.Return(wikiFieldPagesSourcePageSizeProperty) },
                setStatements:
                    new[] { SyntaxEx.Assignment(wikiFieldPagesSourcePageSizeProperty, SyntaxFactory.IdentifierName("value")) });

            var userAgentParameter = SyntaxEx.Parameter("string", "userAgent");
            var baseUriParameter = SyntaxEx.Parameter("string", "baseUri", SyntaxEx.NullLiteral());
            var apiPathParameter = SyntaxEx.Parameter("string", "apiPath", SyntaxEx.NullLiteral());

            var wikiAssignment = SyntaxEx.Assignment(
                wikiField,
                SyntaxEx.ObjectCreation(
                    Names.WikiInfo,
                    (NamedNode)userAgentParameter,
                    SyntaxEx.Coalesce((NamedNode)baseUriParameter, SyntaxEx.Literal(baseUri)),
                    SyntaxEx.Coalesce((NamedNode)apiPathParameter, SyntaxEx.Literal(apiPath))));

            var queryAssignment = SyntaxEx.Assignment(
                queryProperty, SyntaxEx.ObjectCreation(Names.QueryAction, (NamedNode)wikiField));

            var ctor = SyntaxEx.ConstructorDeclaration(
                new[] { SyntaxKind.PublicKeyword }, Names.Wiki,
                new[] { userAgentParameter, baseUriParameter, apiPathParameter },
                new StatementSyntax[] { wikiAssignment, queryAssignment });

            var members = new List<MemberDeclarationSyntax>
            {
                wikiField,
                queryProperty,
                pagesSourcePageSizeProperty,
                ctor
            };

            members.AddRange(CreatePageSourceMethods(wikiField));

            var wikiClass = SyntaxEx.ClassDeclaration(Names.Wiki, members);

            Files.Add(
                Names.Wiki,
                SyntaxEx.CompilationUnit(
                    SyntaxEx.NamespaceDeclaration(Namespace, wikiClass),
                    "System", "System.Collections.Generic", "LinqToWiki", "LinqToWiki.Collections",
                    "LinqToWiki.Internals", "LinqToWiki.Parameters", EntitiesNamespace));
        }

        /// <summary>
        /// Creates methods to create <see cref="ListSourceBase{TPage}"/> page sources
        /// for the <c>Wiki</c> class.
        /// </summary>
        private static IEnumerable<MethodDeclarationSyntax> CreatePageSourceMethods(FieldDeclarationSyntax wikiField)
        {
            var pageSources =
                new[]
                {
                    new { type = "string", name = "titles", sourceType = typeof(TitlesSource<>) },
                    new { type = "long", name = "pageIds", sourceType = typeof(PageIdsSource<>) },
                    new { type = "long", name = "revIds", sourceType = typeof(RevIdsSource<>) }
                };

            foreach (var pageSource in pageSources)
            {
                var sourceTypeName = pageSource.sourceType.Name;
                sourceTypeName = sourceTypeName.Substring(0, sourceTypeName.IndexOf('`'));

                var parameterVersions =
                    new[]
                    {
                        SyntaxEx.Parameter(SyntaxEx.GenericName("IEnumerable", pageSource.type), pageSource.name),
                        SyntaxEx.Parameter(
                            pageSource.type + "[]", pageSource.name, modifiers: new[] { SyntaxKind.ParamsKeyword })
                    };

                foreach (var parameter in parameterVersions)
                {
                    var returnStatement = SyntaxEx.Return(
                        SyntaxEx.ObjectCreation(
                            SyntaxEx.GenericName(sourceTypeName, Names.Page),
                            (NamedNode)wikiField, (NamedNode)parameter));

                    yield return SyntaxEx.MethodDeclaration(
                        new[] { SyntaxKind.PublicKeyword }, SyntaxEx.GenericName("PagesSource", Names.Page),
                        "Create" + sourceTypeName, new[] { parameter }, returnStatement);
                }
            }
        }

        /// <summary>
        /// Creates file that holds all enums.
        /// </summary>
        private void CreateEnumsFile()
        {
            Files.Add(
                Names.Enums,
                SyntaxEx.CompilationUnit(
                    SyntaxEx.NamespaceDeclaration(Namespace), "LinqToWiki.Internals"));
        }

        /// <summary>
        /// Creates the <c>PageResult</c> class that can be used as a named (non-anonymous) type
        /// for the result of PageSource queries.
        /// </summary>
        private void CreatePageResultClass()
        {
            var infoResultClassName = Files["info"].SingleDescendant<ClassDeclarationSyntax>().Identifier.ValueText;
            var dataPropertyType = SyntaxEx.GenericName("IEnumerable", TypeParameterName);

            var infoProperty = SyntaxEx.AutoPropertyDeclaration(
                new[] { SyntaxKind.PublicKeyword }, infoResultClassName, "Info", SyntaxKind.PrivateKeyword);

            var dataProperty = SyntaxEx.AutoPropertyDeclaration(
                new[] { SyntaxKind.PublicKeyword }, dataPropertyType, "Data", SyntaxKind.PrivateKeyword);

            var infoParameter = SyntaxEx.Parameter(infoResultClassName, "info");
            var dataParameter = SyntaxEx.Parameter(dataPropertyType, "data");

            var ctorBody =
                new StatementSyntax[]
                {
                    SyntaxEx.Assignment(infoProperty, infoParameter),
                    SyntaxEx.Assignment(dataProperty, dataParameter)
                };

            var ctor = SyntaxEx.ConstructorDeclaration(
                new[] { SyntaxKind.PublicKeyword }, Names.PageResult, new[] { infoParameter, dataParameter }, ctorBody);

            var pageResultClass = SyntaxEx.ClassDeclaration(
                Names.PageResult, new[] { SyntaxEx.TypeParameter(TypeParameterName) }, null,
                new MemberDeclarationSyntax[] { infoProperty, dataProperty, ctor });

            var pageResultType = SyntaxEx.GenericName(Names.PageResult, TypeParameterName);

            var createMethodBody = SyntaxEx.Return(
                SyntaxEx.ObjectCreation(pageResultType, (NamedNode)infoParameter, (NamedNode)dataParameter));

            var createMethod = SyntaxEx.MethodDeclaration(
                new[] { SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword }, pageResultType, "Create",
                new[] { SyntaxEx.TypeParameter(TypeParameterName) }, new[] { infoParameter, dataParameter },
                createMethodBody);

            var pageResultHelperClass = SyntaxEx.ClassDeclaration(SyntaxKind.StaticKeyword, Names.PageResult, createMethod);

            Files.Add(
                Names.PageResult,
                SyntaxEx.CompilationUnit(
                    SyntaxEx.NamespaceDeclaration(Namespace, pageResultClass, pageResultHelperClass),
                    "System.Collections.Generic", EntitiesNamespace));
        }

        /// <summary>
        /// Adds the code for given query modules.
        /// </summary>
        public void AddQueryModules(IEnumerable<string> moduleNames)
        {
            var modules = m_modulesSource.GetQueryModules(moduleNames);

            foreach (var module in modules)
            {
                if (module.QueryType == QueryType.List || module.QueryType == QueryType.Meta)
                {
                    if (module.ListResult)
                    {
                        new QueryModuleGenerator(this).Generate(module);
                    }
                    else
                    {
                        new SingleQueryModuleGenerator(this).Generate(module);
                    }
                }
                else
                {
                    // unknown result properties: not supported
                    if (module.PropertyGroups == null)
                    {
                        continue;
                    }

                    if (module.Name == "info")
                    {
                        new InfoModuleGenerator(this).Generate(module);
                        CreatePageResultClass();
                    }
                    else if (!module.ListResult)
                    {
                        new SinglePropModuleGenerator(this).Generate(module);
                    }
                    else
                    {
                        // this is not actually a query module
                        if (module.Name == "stashimageinfo")
                        {
                            continue;
                        }

                        new PropModuleGenerator(this).Generate(module);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the code for given non-query modules.
        /// </summary>
        public void AddModules(IEnumerable<string> moduleNames)
        {
            foreach (var module in m_modulesSource.GetModules(moduleNames))
            {
                new ModuleGenerator(this).Generate(module);
            }
        }

        /// <summary>
        /// Adds the code for all query modules.
        /// </summary>
        public void AddAllQueryModules()
        {
            AddQueryModules(m_modulesSource.GetAllQueryModuleNames());
        }

        /// <summary>
        /// Adds the code for all non-query modules.
        /// </summary>
        public void AddAllModules()
        {
            var moduleNames = m_modulesSource.GetAllModuleNames();
            AddModules(moduleNames);
        }

        /// <summary>
        /// Compiles the generated code into a DLL.
        /// </summary>
        public CompilerResults Compile(string name, string path = "")
        {
            if (m_modulesFinished == 0)
            {
                throw new InvalidOperationException("No modules were successfully finished, nothing to compile.");
            }

            var compiler = new CSharpCodeProvider();

            var files = WriteToFiles(Path.Combine(Path.GetTempPath(), "LinqToWiki"));

            return compiler.CompileAssemblyFromFile(
                new CompilerParameters(
                    new[]
                    {
                        typeof(Enumerable).Assembly.Location,
                        typeof(System.Xml.Linq.XElement).Assembly.Location,
                        typeof(System.Xml.IXmlLineInfo).Assembly.Location,
                        typeof(WikiInfo).Assembly.Location
                    }, Path.Combine(path, name + ".dll"))
                {
                    TreatWarningsAsErrors = true,
                    CompilerOptions = $"/doc:{Path.Combine(path, name + ".xml")} /nowarn:1591 /debug",
                    IncludeDebugInformation = false
                },
                files.ToArray());
        }

        /// <summary>
        /// Writes the generated code into a folder.
        /// </summary>
        public IEnumerable<string> WriteToFiles(string directoryPath)
        {
            if (m_modulesFinished == 0)
            {
                throw new InvalidOperationException("No modules were successfully finished, nothing to write out.");
            }

            var result = new List<string>();

            Directory.CreateDirectory(directoryPath);
            foreach (var file in Files)
            {
                var path = Path.Combine(directoryPath, file.Item1 + Extension);
                File.WriteAllText(
                    path, Formatter.Format(file.Item2, new AdhocWorkspace()).ToString());
                result.Add(path);
            }

            return result;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            var first = true;

            foreach (var file in Files)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.AppendLine();
                }

                builder.AppendLine(file.Item1 + Extension + ':');
                builder.AppendLine(Formatter.Format(file.Item2, new AdhocWorkspace()).ToString());
            }

            return builder.ToString();
        }

        /// <summary>
        /// Marks module as finished.
        /// </summary>
        public void ModuleFinished()
        {
            m_modulesFinished++;
        }
    }
}