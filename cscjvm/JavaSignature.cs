using System.Text;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace cscjvm
{
    public static class JavaSignature
    {
        public static string CreateMethodSignature(IMethod method)
        {
            var builder = new StringBuilder();
            builder.Append(method.DeclaringType.FullName.ToJavaNamespace());
            builder.Append("/");
            builder.Append(method.Name);
            builder.Append("(");
            foreach (var parameter in method.Parameters)
            {
                builder.Append(CreateTypeSignature(parameter.Type));
            }
            builder.Append(")");
            builder.Append(CreateTypeSignature(method.ReturnType));
            return builder.ToString();
        }

        public static string CreateMethodSignature(MethodDeclaration method, ITypeResolveContext resolver, bool fullyQualified = true)
        {
            var builder = new StringBuilder();
            if (fullyQualified)
            {
                builder.Append(CreateTypeSignature(method.GetParent<TypeDeclaration>()));
                builder.Append("/");
            }
            builder.Append(method.Name);
            builder.Append("(");
            foreach (var parameter in method.Parameters)
            {
                builder.Append(CreateTypeSignature(parameter.Type.ToTypeReference().Resolve(resolver)));
            }
            builder.Append(")");
            builder.Append(CreateTypeSignature(method.ReturnType.ToTypeReference().Resolve(resolver)));
            return builder.ToString();
        }

        public static string CreateTypeSignature(IType type)
        {
            if (type.Kind == TypeKind.Array)
                return "[" + CreateTypeSignature((type as TypeWithElementType).ElementType);
            if (type.FullName == typeof(string).FullName)
                return "Ljava/lang/String;";
            if (type.FullName == typeof(bool).FullName)
                return "Z";
            if (type.FullName == typeof(byte).FullName)
                return "B";
            if (type.FullName == typeof(char).FullName)
                return "C";
            if (type.FullName == typeof(short).FullName)
                return "S";
            if (type.FullName == typeof(int).FullName)
                return "I";
            if (type.FullName == typeof(long).FullName)
                return "J";
            if (type.FullName == typeof(float).FullName)
                return "F";
            if (type.FullName == typeof(double).FullName)
                return "D";
            if (type.FullName == typeof(void).FullName)
                return "V";
            return "L" + type.FullName.ToJavaNamespace() + ";";
        }

        public static string CreateTypeSignature(TypeDeclaration type)
        {
            var @namespace = type.GetParent<NamespaceDeclaration>();
            var builder = new StringBuilder();
            builder.Append(@namespace.FullName.ToJavaNamespace());
            builder.Append("/");
            builder.Append(type.Name);
            return builder.ToString();
        }
    }
}

