using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CustomORM.Converter.Extensions;

public static class CodeCompilerExtensions
{
    public static CSharpCompilation CompileAssembly(this string sourceCode)
    {
        // https://softwareparticles.com/how-to-dynamically-execute-code-in-net/
        var codeString = SourceText.From(sourceCode);
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);

        var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

        var rootPath = Path.GetDirectoryName(typeof(object).Assembly.Location) + Path.DirectorySeparatorChar;

        var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(KeyAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(rootPath, "System.dll")),
                MetadataReference.CreateFromFile(Path.Combine(rootPath, "netstandard.dll")),
                MetadataReference.CreateFromFile(Path.Combine(rootPath, "System.Runtime.dll"))
            };

        Assembly
            .GetEntryAssembly()?
            .GetReferencedAssemblies()
            .ToList()
            .ForEach(a => references.Add(MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

        return CSharpCompilation.Create
            (
                $"{Guid.NewGuid()}.dll",
                new[] { parsedSyntaxTree },
                references: references,
                options: new CSharpCompilationOptions
                (
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
                )
            );
    }

    public static void LogDiagnostics(this IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            var error = new CompilerError(
                diagnostic.Location.SourceTree?.FilePath,
                line: diagnostic.Location.GetLineSpan().StartLinePosition.Line,
                column: diagnostic.Location.GetLineSpan().StartLinePosition.Character,
                errorNumber: diagnostic.Id,
                errorText: diagnostic.GetMessage());

            Console.WriteLine($"Line : {error.Line} Column : {error.Column} {error}");
        }
    }
}