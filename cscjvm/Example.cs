namespace cscjvm.Example
{
    using java.lang;

    [EntryPoint]
    class Program
    {
        // Still need to call this lowercase main so Java
        // can start it.  The compiler might provide a
        // bootstrap to call a proper Main method in the
        // future.
        public static void main(string[] args)
        {
            var hello = "Hello, World!";
            var a = 3;
            var b = 6;
            var c = a + b;
            System.@out.println(hello);
            System.@out.println(String.valueOf(c));
        }
    }
}