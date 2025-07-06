using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LinqToWiki.Codegen
{
    /// <summary>
    /// Contains LinqToWiki-specific Roslyn extension methods.
    /// </summary>
    internal static class LinqToWikiRoslynExtensions
    {
        /// <summary>
        /// Returns the given class with a private parameterless constructor added.
        /// </summary>
        public static ClassDeclarationSyntax AddPrivateConstructor(this ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.AddMembers(
                SyntaxEx.ConstructorDeclaration(
                    new[] { SyntaxKind.PrivateKeyword }, classDeclaration.Identifier.ValueText));
        }
    }
}