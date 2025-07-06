﻿using LinqToWiki.Codegen.ModuleInfo;
using LinqToWiki.Collections;
using LinqToWiki.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToWiki.Codegen.ModuleGenerators
{
    /// Generates code for <see cref="LinqToWiki.Internals.QueryType.Meta"/> query modules,
    /// that return a single result (like <c>userinfo</c>).
    internal class SingleQueryModuleGenerator : ModuleGenerator
    {
        public SingleQueryModuleGenerator(Wiki wiki)
            : base(wiki)
        {
        }

        protected override void GenerateMethod(Module module)
        {
            GenerateMethod(
                module, module.Parameters.Where(p => p.Name != "prop"), ResultClassName, null, Wiki.Names.QueryAction,
                true, null);
        }

        protected override IEnumerable<Tuple<string, string>> GetBaseParameters(Module module)
        {
            return Wiki.QueryBaseParameters.Concat(
                new TupleList<string, string>
                {
                    { module.QueryType.ToString().ToLowerInvariant(), module.Name },
                    {
                        module.Prefix + "prop",
                        module.PropertyGroups.Select(g => g.Name).Where(n => n != string.Empty).ToQueryString()
                    }
                });
        }

        protected override ClassDeclarationSyntax GenerateResultClass(IEnumerable<PropertyGroup> propertyGroups)
        {
            var resultClass = GenerateClassForProperties(ResultClassName, propertyGroups.SelectMany(g => g.Properties));

            var parseMethodBody = resultClass.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Single(m => m.Identifier.ValueText == "Parse")
                .Body ?? throw new InvalidOperationException("method declaration cannot be null");

            var statements = parseMethodBody.Statements;

            var newStatement = SyntaxEx.Assignment(
                SyntaxFactory.IdentifierName("element"),
                SyntaxEx.Invocation(
                    SyntaxEx.MemberAccess(SyntaxEx.Invocation(SyntaxEx.MemberAccess("element", "Elements")), "Single")));

            var newStatements = new[] { newStatement }.Concat(statements);

            var newBody = SyntaxEx.Block(newStatements);

            return resultClass.ReplaceNode(parseMethodBody, newBody);
        }
    }
}