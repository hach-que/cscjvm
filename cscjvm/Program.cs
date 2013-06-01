using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using NDesk.Options;

namespace cscjvm
{
    class MainClass
    {
        private static IEnumerable<IUnresolvedAssembly> GetProjectReferences(List<string> explicitReferences)
        {
            var loader = new CecilLoader();
            yield return loader.LoadAssemblyFile(typeof(object).Assembly.Location);
            foreach (var reference in explicitReferences)
                yield return loader.LoadAssemblyFile(reference);
        }

        public static void Main(string[] args)
        {
            // Parse options.
            string jasminJar = null;
            string outputJar = null;
            bool noConfig;
            string target;
            string debugLevel;
            bool debug;
            bool optimize;
            string define;
            List<string> reference = new List<string>();
            string warnLevel;
            List<string> files;
            var options = new OptionSet()
            {
                { "jasmin=", "Path to Jasmin JAR", x => jasminJar = x },
                { "out=", "The resulting JAR to create", x => outputJar = x },
                { "noconfig", "", x => noConfig = true },
                { "target=", "", x => target = x },
                { "debug", "", x => debugLevel = x },
                { "debug+", "", x => debug = true },
                { "optimize+", "", x => optimize = true },
                { "debug-", "", x => debug = false },
                { "optimize-", "", x => optimize = false },
                { "define", "", x => define = x },
                { "reference:", "", reference.Add },
                { "warn", "", x => warnLevel = x },
            };
            try
            {
                files = options.Parse(args);
                if (files.Count == 1 && files[0][0] == '@')
                {
                    // Read command line from file.
                    using (var reader = new StreamReader(files[0].Substring(1)))
                    {
                        //Console.WriteLine(reader.ReadToEnd());
                        files = options.Parse(reader.ReadToEnd().Split(' '));
                    }
                }
            }
            catch (OptionException ex)
            {
                Console.Write("cscjvm: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `cscjvm --help' for more information.");
                return;
            }

            if (string.IsNullOrWhiteSpace(jasminJar))
            {
                Console.WriteLine("Defaulting Jasmin JAR path to JASMIN_JAR environment variable.");
                jasminJar = Environment.GetEnvironmentVariable("JASMIN_JAR");
                if (string.IsNullOrWhiteSpace(jasminJar))
                {
                    Console.WriteLine("Jasmin JAR path must be set.");
                    return;
                }
            }

            Console.WriteLine("C# to JVM Compiler");
            Console.WriteLine("Using Jasmin at: " + jasminJar);

            foreach (var arg in args)
            {
                Console.WriteLine("argument: " + arg);
            }
            
            IProjectContent project = new CSharpProjectContent();

            var trees = new List<SyntaxTree>();

            Console.WriteLine("Parsing C# files...");
            var parser = new CSharpParser();
            foreach (var filename in files)
            {
                using (var reader = new StreamReader(filename))
                {
                    trees.Add(parser.Parse(reader.ReadToEnd(), filename));
                }
            }

            Console.WriteLine("Creating references...");
            project = project.AddAssemblyReferences(GetProjectReferences(reference));
            foreach (var tree in trees)
            {
                project = project.AddOrUpdateFiles(tree.ToTypeSystem());
            }

            Console.WriteLine("Emitting Jasmin assembly...");
            var output = new DirectoryInfo(Environment.CurrentDirectory).CreateSubdirectory("obj");
            var sources = new List<string>();
            string entryPoint = null;
            foreach (var tree in trees)
            {
                var unresolvedFile = tree.ToTypeSystem();
                ICompilation compilation = project.CreateCompilation();
                CSharpAstResolver resolver = new CSharpAstResolver(compilation, tree, unresolvedFile);

                var typeVisitor = new JavaTypeVisitor(resolver, output);
                tree.AcceptVisitor(typeVisitor);
                sources.AddRange(typeVisitor.CreatedFiles);
                if (typeVisitor.EntryPoint != null)
                    entryPoint = typeVisitor.EntryPoint;
            }

            if (entryPoint == null)
            {
                Console.WriteLine("No class with [EntryPoint] attribute.  This JAR will be a library.");
            }

            Console.WriteLine("Compiling JAR...");
            var jasmin = new Jasmin(jasminJar);
            using (var writer = new StreamWriter(outputJar))
            {
                jasmin.CompileJar(output.FullName, sources, writer.BaseStream, entryPoint);
            }
            Console.WriteLine("Wrote JAR to " + outputJar);
        }
    }
}
