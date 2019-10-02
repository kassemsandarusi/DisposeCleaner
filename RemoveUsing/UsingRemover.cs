using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RemoveUsing
{
    public class UsingRemover
    {
        /* Syntax Cases:
         * Case #1:
            using()
                statement;
         * Case #2:
            using() {
                statement;
                ...
            }
         * Case #3:
            using() <--- me
            using() {
            }
         * Case #4:
            using()
            using() <--- me {
            }
         */

        public enum BracePlacement {
            NewLine,
            SameLine,
        }

        public class UsingRemoverOptions {

            /// <summary>
            /// Character to use for indentation.
            /// </summary>
            /// <value></value>
            public string Indentation { get; set; }

            public BracePlacement BracePlacement { get; set; }

            public static UsingRemoverOptions Defaults = new UsingRemoverOptions {
                Indentation = "    ",
                BracePlacement = BracePlacement.NewLine
            };
        }

        public UsingRemover(UsingRemoverOptions options = null) {
            Options = options ?? UsingRemoverOptions.Defaults;
        }

        private UsingRemoverOptions Options { get; }

        public SyntaxNode RemoveUsings(SyntaxNode syntaxRoot, string targetType)
        {
            var badNode = syntaxRoot.DescendantNodes()
                .OfType<UsingStatementSyntax>()
                .FirstOrDefault(x => x.Declaration?.Type is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == targetType);

            if (badNode == null)
                return syntaxRoot;

            var unusedVersion = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(badNode.Declaration.Type)
                                        .WithVariables(badNode.Declaration.Variables));
            
            // let's keep the block if there is another using.
            if (badNode.Parent is UsingStatementSyntax parentUsing)
            {
                // assume child is block.
                var childBlock = badNode.Statement as BlockSyntax;

                if (Options.BracePlacement == BracePlacement.NewLine) {

                }

                var childBlockTrivia = parentUsing.GetLeadingTrivia();
                var childBlockStatementTrivia = parentUsing.GetLeadingTrivia().Append(SyntaxFactory.Whitespace(Options.Indentation));

                unusedVersion = unusedVersion.WithLeadingTrivia(childBlockStatementTrivia).WithTrailingTrivia(badNode.GetTrailingTrivia());

                var statements = new SyntaxList<StatementSyntax>(unusedVersion);
                statements = statements.AddRange(childBlock.Statements.Select(x => x.WithLeadingTrivia(childBlockStatementTrivia)));
 
                var newChildBlock = childBlock
                                        .WithStatements(statements);
                                        
                if (Options.BracePlacement == BracePlacement.NewLine) {
                    newChildBlock = newChildBlock.WithLeadingTrivia(childBlockTrivia);
                }

                var newParent = parentUsing
                                    .WithTrailingTrivia(badNode.GetTrailingTrivia())
                                    .WithCloseParenToken(badNode.CloseParenToken)
                                    .WithStatement(newChildBlock);

                syntaxRoot = syntaxRoot.ReplaceNode(parentUsing, newParent);
            }
            else
            {
                var leadingTrivia = badNode.GetLeadingTrivia();
                var trailingTrivia = badNode.GetTrailingTrivia();

                unusedVersion = unusedVersion.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);
                var statements = new SyntaxList<StatementSyntax>(unusedVersion);

                switch (badNode.Statement) {
                    // we remove the block and just take the children.
                    case BlockSyntax block:

                        foreach (var statement in block.Statements)
                        {
                            statements = statements.Add(statement.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia));
                        }

                        break;
                    case UsingStatementSyntax usingStatement:
                        break;

                    default:
                        statements = statements.Add(badNode.Statement.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia));
                        break;
                }

                syntaxRoot = syntaxRoot.ReplaceNode(badNode, statements);
            }

            return syntaxRoot;
        }
    }
}
