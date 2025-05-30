namespace Obfuz.Utils
{
    public abstract class AssemblyResolverBase : IAssemblyResolver
    {
        public abstract string ResolveAssembly(string assemblyName);
    }
}
