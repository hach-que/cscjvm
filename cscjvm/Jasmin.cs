using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Ionic.Zip;

namespace cscjvm
{
    public class Jasmin
    {
        private string m_JasminJar;

        public Jasmin(string jasminJar)
        {
            this.m_JasminJar = jasminJar;
        }

        public void CompileJar(string @base, IEnumerable<string> sources, Stream output, string entryPoint)
        {
            using (var zip = new ZipFile())
            {
                var temp = this.GetTemporaryDirectory();
                var args = "-jar \"" +
                    this.m_JasminJar + "\" " +
                    "-d \"" + temp.FullName + "\"";
                foreach (var source in sources)
                    args += " \"" + Path.Combine(@base, source) + "\"";
                Console.WriteLine(args);
                var process = Process.Start("java", args);
                process.WaitForExit();

                foreach (var source in sources)
                {
                    var classFile = source.Substring(0, source.Length - 2) + ".class";
                    var normalizedClassFile = classFile.Replace('\\', '/');
                    var directoryInArchive = normalizedClassFile.Substring(0, normalizedClassFile.LastIndexOf("/"));
                    var pathToOriginal = Path.Combine(temp.FullName, classFile);
                    Console.WriteLine(pathToOriginal + " as " + classFile);
                    zip.AddFile(pathToOriginal, directoryInArchive);
                }
                
                // Create Manifest file.
                var manifestPath = Path.Combine(temp.FullName, "MANIFEST.MF");
                var manifest = new StreamWriter(manifestPath);
                manifest.WriteLine("Manifest-Version: 1.0");
                manifest.WriteLine("Created-By: cscjvm");
                if (entryPoint != null)
                    manifest.WriteLine("Main-Class: " + entryPoint);
                manifest.Close();
                zip.AddFile(manifestPath, "META-INF");

                zip.Save(output);
                File.Delete(manifestPath);
            }
        }

        private DirectoryInfo GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            return Directory.CreateDirectory(tempDirectory);
        }
    }
}

