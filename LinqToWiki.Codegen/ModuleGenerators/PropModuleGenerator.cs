using LinqToWiki.Codegen.ModuleInfo;
using LinqToWiki.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace LinqToWiki.Codegen.ModuleGenerators
{
    /// <summary>
    /// Generates code for <see cref="LinqToWiki.Internals.QueryType.Prop"/> query modules
    /// that return a list of results (almost all of them do).
    /// </summary>
    internal class PropModuleGenerator : QueryModuleGenerator
    {
        public PropModuleGenerator(Wiki wiki)
            : base(wiki)
        {
        }

        protected override string MethodClassName => Wiki.Names.Page;

        protected override IEnumerable<Tuple<string, string>> GetBaseParameters(Module module)
        {
            return new TupleList<string, string>();
        }

        protected override IList<StatementSyntax> GenerateMethodBody(
            ExpressionSyntax queryProcessor, ExpressionSyntax queryParameters, IList<StatementSyntax> statements)
        {
            return null;
        }
    }
}