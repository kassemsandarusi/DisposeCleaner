using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Build.Locator;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System;
using McMaster.Extensions.CommandLineUtils;
using System.IO;

namespace RemoveUsing
{
    [Command(Name = "usingremover", Description = "Tool for removing usings of a particular type from a solution.")]
    public class Program
    {
        [Argument(0, "Solution", "The path to the target solution.")]
        public string SolutionPath { get; set; }

        [Argument(1, "TypeName", "The type you are interested in undisposing.")]
        public string TypeName { get; set; } 

        public static async Task<int> Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();
            return await CommandLineApplication.ExecuteAsync<Program>(args);
        }

        private async Task<int> OnExecuteAsync(CommandLineApplication app) {
            if (string.IsNullOrEmpty(SolutionPath) || string.IsNullOrEmpty(TypeName)) {
                app.ShowHelp();
                return 1;
            }

            using (var msbuildWorkspace = MSBuildWorkspace.Create())
            {
                if (!File.Exists(SolutionPath)) {
                    Console.WriteLine($"No file found at {SolutionPath}.");
                    return 1;
                }

                var originalSolution = await msbuildWorkspace.OpenSolutionAsync(SolutionPath);
                var newSolution = originalSolution;

                var usingRemover = new UsingRemover();

                foreach (var projectId in originalSolution.ProjectIds)
                {
                    var project = newSolution.GetProject(projectId);
                    var newProject = project;
                    
                    foreach (var documentId in project.DocumentIds)
                    {
                        var document = newSolution.GetDocument(documentId);
                        Console.WriteLine($"Processing Document {document.Name}");

                        if (document.SourceCodeKind != SourceCodeKind.Regular)
                        {
                            continue;
                        }

                        var syntaxRoot = await document.GetSyntaxRootAsync();
                        var transformedSyntaxRoot = usingRemover.RemoveUsings(syntaxRoot, "InventoryService");

                        var newDocument = document.WithSyntaxRoot(transformedSyntaxRoot);
                        newSolution = newSolution.WithDocumentSyntaxRoot(document.Id, transformedSyntaxRoot);

                        // todo: create backup of old versions of files?
                    }

                    if (!msbuildWorkspace.TryApplyChanges(newSolution)) {
                        Console.WriteLine("Failed to make changes to solution");
                    }
                }
            }

            return 0;
        }
    }
}
