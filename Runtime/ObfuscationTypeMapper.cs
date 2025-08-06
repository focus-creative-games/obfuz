using System;
using System.Collections.Generic;
using System.Reflection;

namespace Obfuz
{
    public static class ObfuscationTypeMapper
    {
        private static readonly Dictionary<Type, string> _type2OriginalFullName = new Dictionary<Type, string>();
        private static readonly Dictionary<Assembly, Dictionary<string, Type>> _originalFullName2Types = new Dictionary<Assembly, Dictionary<string, Type>>();

        public static void RegisterType<T>(string originalFullName)
        {
            RegisterType(typeof(T), originalFullName);
        }

        public static void RegisterType(Type type, string originalFullName)
        {
            if (_type2OriginalFullName.ContainsKey(type))
            {
                throw new ArgumentException($"Type '{type.FullName}' is already registered with original name '{_type2OriginalFullName[type]}'.");
            }
            _type2OriginalFullName.Add(type, originalFullName);
            Assembly assembly = type.Assembly;
            if (!_originalFullName2Types.TryGetValue(assembly, out var originalFullName2Types))
            {
                originalFullName2Types = new Dictionary<string, Type>();
                _originalFullName2Types[assembly] = originalFullName2Types;
            }
            if (originalFullName2Types.ContainsKey(originalFullName))
            {
                throw new ArgumentException($"Original full name '{originalFullName}' is already registered with type '{originalFullName2Types[originalFullName].FullName}'.");
            }
            originalFullName2Types.Add(originalFullName, type);
        }

        public static string GetOriginalTypeFullName(Type type)
        {
            return _type2OriginalFullName.TryGetValue(type, out string originalFullName)
                ? originalFullName
                : throw new KeyNotFoundException($"Type '{type.FullName}' not found in the obfuscation mapping.");
        }

        public static string GetOriginalTypeFullNameOrCurrent(Type type)
        {
            if (_type2OriginalFullName.TryGetValue(type, out string originalFullName))
            {
                return originalFullName;
            }
            return type.FullName;
        }

        public static Type GetTypeByOriginalFullName(Assembly assembly, string originalFullName)
        {
            if (_originalFullName2Types.TryGetValue(assembly, out var n2t))
            {
                if (n2t.TryGetValue(originalFullName, out Type type))
                {
                    return type;
                }
            }
            return null;
        }

        public static void Clear()
        {
            _type2OriginalFullName.Clear();
            _originalFullName2Types.Clear();
        }
    }

}
