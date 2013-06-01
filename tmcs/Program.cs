using System;
using Mono.Cecil;

namespace tmcs
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var assembly = AssemblyDefinition.ReadAssembly(args[0]);
            foreach (var module in assembly.Modules)
            {
                foreach (var type in module.Types)
                {
                    // Use ITypeVisitor here.
                }
            }
        }
    }
}
