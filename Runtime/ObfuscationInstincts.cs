namespace Obfuz
{
    public static class ObfuscationInstincts
    {
        /// <summary>
        /// Returns the original full name before obfuscated of the type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string FullNameOf<T>()
        {
            return typeof(T).FullName;
        }

        /// <summary>
        /// Returns the original name before obfuscated of the type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string NameOf<T>()
        {
            return typeof(T).Name;
        }

        /// <summary>
        /// register original type name to type mapping.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void RegisterReflectionType<T>()
        {
            ObfuscationTypeMapper.RegisterType<T>(typeof(T).FullName);
        }
    }
}
