using System.IO;

namespace Obfuz.Utils
{
    public class PathAssemblyResolver : AssemblyResolverBase
    {
        private readonly string[] _searchPaths;

        public PathAssemblyResolver(params string[] searchPaths)
        {
            _searchPaths = searchPaths;
        }

        public override string ResolveAssembly(string assemblyName)
        {
            foreach (var path in _searchPaths)
            {
                string assPath = Path.Combine(path, assemblyName + ".dll");
                if (File.Exists(assPath))
                {
                    //Debug.Log($"resolve {assemblyName} at {assPath}");
                    return assPath;
                }
            }
            return null;
        }
    }
}
