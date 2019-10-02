using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace RemoveUsing.Tests
{
    public class RemoveUsingTests
    {
        private SyntaxTree GetDocumentSyntaxTree(string document) {
            var csharpParseOptions = new CSharpParseOptions()
                .WithKind(SourceCodeKind.Regular)
                .WithLanguageVersion(LanguageVersion.Latest);

            return CSharpSyntaxTree.ParseText(document, csharpParseOptions);
        }

        private string ReadResource(string resourceName) {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        [Theory]
        [InlineData("InventoryService", "GetWorkItems", "RemoveUsing.Tests.TestCaseDocuments.NoBlock.cs","RemoveUsing.Tests.TestCaseDocuments.NoBlockTransformed.cs")]
        [InlineData("TestTarget", "TestMethod", "RemoveUsing.Tests.TestCaseDocuments.Block.cs","RemoveUsing.Tests.TestCaseDocuments.BlockTransformed.cs")]
        [InlineData("TestTarget", "UsingWithBraceOnSameLine", "RemoveUsing.Tests.TestCaseDocuments.ChildOfUsing.cs","RemoveUsing.Tests.TestCaseDocuments.ChildOfUsingTransformed.cs")]
        [InlineData("TestTarget", "UsingWithBraceOnNextLine", "RemoveUsing.Tests.TestCaseDocuments.ChildOfUsing.cs","RemoveUsing.Tests.TestCaseDocuments.ChildOfUsingTransformed.cs")]
        [InlineData("TestTarget", "UsingWithExtraLevelOfIndention", "RemoveUsing.Tests.TestCaseDocuments.ChildOfUsing.cs","RemoveUsing.Tests.TestCaseDocuments.ChildOfUsingTransformed.cs")]
        public void RemoveUsing_CorrectRewrites(string targetType, string testMethod, string preTransformManifest, string postTransformManifest) {
            var usingRemover = new UsingRemover(null);

            var originalDocument = ReadResource(preTransformManifest);
            var expectedTransformation = ReadResource(postTransformManifest);
            var originalSyntaxTree = GetDocumentSyntaxTree(originalDocument);
            var expectedSyntaxTree = GetDocumentSyntaxTree(expectedTransformation);

            var originalMethod = originalSyntaxTree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Single(x => x.Identifier.ValueText == testMethod);
            var transformedMethod = expectedSyntaxTree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Single(x => x.Identifier.ValueText == testMethod);

            var actualTransformation = usingRemover.RemoveUsings(originalMethod, targetType).GetText().ToString();

            Assert.Equal(transformedMethod.GetText().ToString(), actualTransformation);
        }
    }
}
