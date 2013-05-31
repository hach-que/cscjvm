using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;

namespace cscjvm
{
    public class JavaTypeVisitor : DepthFirstAstVisitor
    {
        private CSharpAstResolver m_Resolver;
        private DirectoryInfo m_Output;
        private Dictionary<TypeDeclaration, StreamWriter> m_TypeWriters = new Dictionary<TypeDeclaration, StreamWriter>();
        private string m_CurrentNamespace = "";

        public string EntryPoint
        {
            get; 
            private set;
        }

        public List<string> CreatedFiles
        {
            get;
            private set;
        }

        public JavaTypeVisitor(CSharpAstResolver resolver, DirectoryInfo output)
        {
            this.m_Resolver = resolver;
            this.m_Output = output;
            this.CreatedFiles = new List<string>();
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
        {
            var old = this.m_CurrentNamespace;
            if (this.m_CurrentNamespace == "")
                this.m_CurrentNamespace = namespaceDeclaration.Name;
            else
                this.m_CurrentNamespace += "." + namespaceDeclaration.Name;

            base.VisitNamespaceDeclaration(namespaceDeclaration);

            this.m_CurrentNamespace = old;
        }

        public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
        {
            this.m_TypeWriters.Add(typeDeclaration, this.GetWriterForType(typeDeclaration));

            // Write out the type information.
            var writer = this.m_TypeWriters[typeDeclaration];
            var @namespace = this.m_CurrentNamespace == "" ? "" :
                this.m_CurrentNamespace.Replace(".", "/") + "/";
            var modifiers = this.GetJavaModifiersAsList(typeDeclaration.Modifiers);
            var javaModifiers = this.ConvertModifierListToJavaModifiers(modifiers);
            writer.WriteLine(".class " + javaModifiers + " " + @namespace + typeDeclaration.Name);
            writer.WriteLine(".super java/lang/Object");

            // Determine if this is an entry point class.
            if (typeDeclaration.Attributes.Count > 0)
            {
                foreach (var attribute in from attributeSection in typeDeclaration.Attributes
                         from attribute in attributeSection.Attributes
                         select attribute)
                {
                    var type = attribute.Type;
                    var resolvedType = this.m_Resolver.Resolve(type) as TypeResolveResult;
                    if (resolvedType.Type.FullName == typeof(EntryPointAttribute).FullName)
                    {
                        this.EntryPoint = @namespace + typeDeclaration.Name;
                        break;
                    }
                }
            }

            // Default constructor.
            writer.WriteLine(".method public <init>()V");
            writer.WriteLine("aload_0");
            writer.WriteLine("invokenonvirtual java/lang/Object/<init>()V");
            writer.WriteLine("return");
            writer.WriteLine(".end method");

            base.VisitTypeDeclaration(typeDeclaration);

            writer.Close();
            this.m_TypeWriters.Remove(typeDeclaration);
        }

        public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            var typeDeclaration = methodDeclaration.GetParent<TypeDeclaration>();
            var writer = this.m_TypeWriters[typeDeclaration];

            var modifiers = this.GetJavaModifiersAsList(methodDeclaration.Modifiers);
            var javaModifiers = this.ConvertModifierListToJavaModifiers(modifiers);
            writer.WriteLine(".method " + javaModifiers + " " + 
                             JavaSignature.CreateMethodSignature(methodDeclaration, this.m_Resolver.TypeResolveContext, false));

            methodDeclaration.AcceptVisitor(new JavaMethodVisitor(this.m_Resolver, writer));

            writer.WriteLine(".end method");
        }

        private List<Modifiers> GetJavaModifiersAsList(Modifiers modifiers)
        {
            var modifierList = new List<Modifiers>();
            if ((modifiers & Modifiers.Abstract) == Modifiers.Abstract)
                modifierList.Add(Modifiers.Abstract);
            if ((modifiers & Modifiers.Async) == Modifiers.Async)
                modifierList.Add(Modifiers.Async);
            if ((modifiers & Modifiers.Const) == Modifiers.Const)
                modifierList.Add(Modifiers.Const);
            if ((modifiers & Modifiers.Extern) == Modifiers.Extern)
                modifierList.Add(Modifiers.Extern);
            if ((modifiers & Modifiers.Internal) == Modifiers.Internal)
                modifierList.Add(Modifiers.Internal);
            if ((modifiers & Modifiers.New) == Modifiers.New)
                modifierList.Add(Modifiers.New);
            if ((modifiers & Modifiers.Override) == Modifiers.Override)
                modifierList.Add(Modifiers.Override);
            if ((modifiers & Modifiers.Partial) == Modifiers.Partial)
                modifierList.Add(Modifiers.Partial);
            if ((modifiers & Modifiers.Private) == Modifiers.Private)
                modifierList.Add(Modifiers.Private);
            if ((modifiers & Modifiers.Protected) == Modifiers.Protected)
                modifierList.Add(Modifiers.Protected);
            if ((modifiers & Modifiers.Public) == Modifiers.Public)
                modifierList.Add(Modifiers.Public);
            if ((modifiers & Modifiers.Readonly) == Modifiers.Readonly)
                modifierList.Add(Modifiers.Readonly);
            if ((modifiers & Modifiers.Sealed) == Modifiers.Sealed)
                modifierList.Add(Modifiers.Sealed);
            if ((modifiers & Modifiers.Static) == Modifiers.Static)
                modifierList.Add(Modifiers.Static);
            if ((modifiers & Modifiers.Unsafe) == Modifiers.Unsafe)
                modifierList.Add(Modifiers.Unsafe);
            if ((modifiers & Modifiers.Virtual) == Modifiers.Virtual)
                modifierList.Add(Modifiers.Virtual);
            if ((modifiers & Modifiers.Volatile) == Modifiers.Volatile)
                modifierList.Add(Modifiers.Volatile);
            return modifierList;
        }

        private string ConvertModifierListToJavaModifiers(List<Modifiers> modifierList)
        {
            var result = "";
            foreach (var modifier in modifierList)
            {
                result += " ";
                switch (modifier)
                {
                    case Modifiers.Public:
                        result += "public";
                        break;
                    case Modifiers.Private:
                        result += "private";
                        break;
                    case Modifiers.Protected:
                        result += "protected";
                        break;
                    case Modifiers.Static:
                        result += "static";
                        break;
                }
                result = result.Trim();
            }

            // Check to see if a method should be made final.
            if (modifierList.Contains(Modifiers.Virtual))
                result = (result + " final").Trim();
            return result;
        }

        private StreamWriter GetWriterForType(TypeDeclaration typeDeclaration)
        {
            var namespaces = this.m_CurrentNamespace.Split('.');
            var current = this.m_Output;
            if (namespaces.Any(x => !string.IsNullOrWhiteSpace(x)))
            {
                foreach (var @namespace in namespaces)
                {
                    var existing = current.GetDirectories().FirstOrDefault<DirectoryInfo>(x => x.Name == @namespace);
                    if (existing != null)
                        current = existing;
                    else
                        current = current.CreateSubdirectory(@namespace);
                }
            }
            this.CreatedFiles.Add(Path.Combine(current.FullName.Substring(this.m_Output.FullName.Length + 1), typeDeclaration.Name + ".j"));
            return new StreamWriter(Path.Combine(current.FullName, typeDeclaration.Name + ".j"));
        }

        
    }
}

