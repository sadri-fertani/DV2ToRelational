using CustomORM.Converter.Extensions;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Text;

namespace CustomORM.Converter;

internal static class Program
{
    internal static void Main(string[] args)
    {
        if (args.Length < 2 || args.Length > 3)
            throw new ArgumentException("Check args");

        string folderSource = args[0];
        string folderTarget = args[1];
        string namespaceTarget = args.Length < 3 ? string.Empty : args[2];

        // infos
        Console.WriteLine("Inputs informations");
        Console.WriteLine("-------------------");
        Console.WriteLine($"Source folder : {folderSource}");
        Console.WriteLine($"Target folder : {folderTarget}");
        Console.WriteLine($"Namespace : {namespaceTarget}");
        Console.WriteLine();

        Task.Delay(500).Wait();

        Console.Write("Check source folder : ");
        Task.Delay(1500).Wait();
        if (!Directory.Exists(folderSource))
        {
            Console.WriteLine("KO");
            Console.Error.WriteLine("Folder source not found");
            Environment.Exit(-1);
        }
        else
            Console.WriteLine("OK");

        Console.Write("Check target folder : ");
        Task.Delay(1500).Wait();
        if (!Directory.Exists(folderTarget))
        {
            Console.WriteLine("KO");
            Console.Error.WriteLine("Folder target not found");
            Environment.Exit(-1);
        }
        else
            Console.WriteLine("OK");

        // clean target folder
        Console.Write($"Clean target folder : ");
        foreach (FileInfo file in new DirectoryInfo(folderTarget).GetFiles()) 
            file.Delete();
        Task.Delay(1500).Wait();
        Console.WriteLine($"OK");

        // Find hubs
        Console.Write("Check hubs : ");
        Task.Delay(1000).Wait();
        var dv2FilesHubs = Directory.GetFiles(folderSource, "H*", SearchOption.TopDirectoryOnly);
        var relationalEntities = new Dictionary<string, string>();

        if (dv2FilesHubs.Any())
        {
            // hubs found
            Console.WriteLine("OK");

            // Generate content of entity class (in memory)
            foreach (var dv2HubFile in dv2FilesHubs)
            {
                var hubName = Path.GetFileNameWithoutExtension(dv2HubFile);

                // Find View for current hub
                var dv2ViewFile = Path.Combine(folderSource, $"V{hubName[1..]}.cs");
                Console.Write($"Check file V{hubName[1..]}.cs : ");
                Task.Delay(500).Wait();
                if (Path.Exists(dv2ViewFile))
                    Console.WriteLine("OK");
                else
                {
                    Console.WriteLine("KO");
                    Environment.Exit(-1);
                }

                // Get class content
                Console.Write($"Code for class {hubName[1..]} generation : ");
                Task.Delay(750).Wait();
                try
                {
                    relationalEntities.Add(hubName[1..], CodeGeneratorExtensions.GenerateCodeClassEntity(dv2HubFile, dv2ViewFile, hubName[1..]).ToString());
                    Console.WriteLine("OK");
                }
                catch(Exception ex)
                {
                    Console.WriteLine("KO");
                    Console.Error.WriteLine(ex);
                }
            }

            StringBuilder allClass = new StringBuilder();

            allClass
                .AddUsingReferences()
                .AddNamespace(namespaceTarget);

            foreach (var entity in relationalEntities)
                allClass.AppendLine(entity.Value);

            Console.Write($"Compile files : ");
            Task.Delay(2500).Wait();
            // check entity class (in memory)
            using (var peStream = new MemoryStream())
            {
                var result = allClass.ToString()
                    .CompileAssembly()
                    .Emit(peStream);

                if (!result.Success)
                {
                    Console.WriteLine("KO");

                    var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
                    failures.LogDiagnostics();
                }
                else
                {
                    Console.WriteLine("OK");

                    peStream.Seek(0, SeekOrigin.Begin);
                }
            }

            // Save entity class
            foreach (var entity in relationalEntities)
            {
                var currentClass = new StringBuilder();

                currentClass
                    .AddUsingReferences()
                    .AddNamespace(namespaceTarget);

                currentClass
                    .AppendLine(entity.Value);

                // save file (new class <=> relational entity)
                Console.Write($"File {entity.Key}.cs generation : ");
                Task.Delay(750).Wait();
                try
                {
                    File.WriteAllText(Path.Combine(folderTarget, $"{entity.Key}.cs"), currentClass.ToString());
                    Console.WriteLine("OK");
                }
                catch(Exception ex)
                {
                    Console.WriteLine("KO");
                    Console.Error.WriteLine(ex);
                }
            }
        }
        else
            Console.WriteLine("KO");

        Console.WriteLine();
        Console.WriteLine("End");
        Console.WriteLine("Press key to close");
        Console.ReadLine();
    }
}
