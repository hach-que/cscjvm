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
        static Lazy<IList<IUnresolvedAssembly>> builtInLibs = new Lazy<IList<IUnresolvedAssembly>>(
            delegate {
            Assembly[] assemblies = {
                typeof(object).Assembly, // mscorlib
                //typeof(Uri).Assembly, // System.dll
                typeof(System.Linq.Enumerable).Assembly, // System.Core.dll
                //                  typeof(System.Xml.XmlDocument).Assembly, // System.Xml.dll
                //                  typeof(System.Drawing.Bitmap).Assembly, // System.Drawing.dll
                //                  typeof(Form).Assembly, // System.Windows.Forms.dll
                typeof(ICSharpCode.NRefactory.TypeSystem.IProjectContent).Assembly,
                typeof(java.io.PrintStream).Assembly
            };
            IUnresolvedAssembly[] projectContents = new IUnresolvedAssembly[assemblies.Length];
            Stopwatch total = Stopwatch.StartNew();
            Parallel.For(
                0, assemblies.Length,
                delegate (int i) {
                Stopwatch w = Stopwatch.StartNew();
                CecilLoader loader = new CecilLoader();
                projectContents[i] = loader.LoadAssemblyFile(assemblies[i].Location);
                Debug.WriteLine(Path.GetFileName(assemblies[i].Location) + ": " + w.Elapsed);
            });
            Debug.WriteLine("Total: " + total.Elapsed);
            return projectContents;
        });

        public static void Main(string[] args)
        {
            var link = new JavaLink();

            // Parse options.
            string jasminJar = null;
            string outputJar = null;
            List<string> files;
            var options = new OptionSet()
            {
                { "jasmin=", "Path to Jasmin JAR", x => jasminJar = x },
                { "output=", "The resulting JAR to create", x => outputJar = x }
            };
            try
            {
                files = options.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.Write("cscjvm: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `cscjvm --help' for more information.");
                return;
            }

            Console.WriteLine("C# to JVM Compiler");
            Console.WriteLine("Using Jasmin at: " + jasminJar);
            
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
            project = project.AddAssemblyReferences(builtInLibs.Value);
            foreach (var tree in trees)
            {
                project = project.AddOrUpdateFiles(tree.ToTypeSystem());
            }

            Console.WriteLine("Emitting Jasmin assembly...");
            var output = new DirectoryInfo(Environment.CurrentDirectory).CreateSubdirectory("output");
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
