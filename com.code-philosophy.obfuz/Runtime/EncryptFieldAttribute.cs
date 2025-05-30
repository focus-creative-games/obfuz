using System;

namespace Obfuz
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class EncryptFieldAttribute : Attribute
    {
    }
}
