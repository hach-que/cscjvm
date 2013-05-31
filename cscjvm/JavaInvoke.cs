using ICSharpCode.NRefactory.TypeSystem;

namespace cscjvm
{
    public static class JavaInvoke
    {
        public static string DetermineInvocationMethod(IMethod method)
        {
            if (method.IsStatic)
                return "invokestatic";
            if (method.IsVirtual || method.IsSealed || method.IsOverride ||
                method.IsOverridable || method.IsConstructor)
                return "invokevirtual";
            return "invokenonvirtual";
        }
    }
}

