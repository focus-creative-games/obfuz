using System;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.Utils
{
    public static class ReflectionUtil
    {
        public static List<Type> FindTypesInCurrentAppDomain(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName))
                .Where(type => type != null)
                .ToList();
        }

        public static Type FindUniqueTypeInCurrentAppDomain(string fullName)
        {
            var foundTypes = FindTypesInCurrentAppDomain(fullName);
            if (foundTypes.Count == 0)
            {
                throw new Exception($"class {fullName} not found in any assembly!");
            }
            if (foundTypes.Count > 1)
            {
                throw new Exception($"class {fullName} found in multiple assemblies! Please retain only one!");
            }
            return foundTypes[0];
        }
    }
}
