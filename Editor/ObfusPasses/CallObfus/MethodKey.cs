using dnlib.DotNet;
using Obfuz.Utils;
using System;

namespace Obfuz.ObfusPasses.CallObfus
{
    class MethodKey : IEquatable<MethodKey>
    {
        public readonly IMethod _method;
        public readonly bool _callVir;
        private readonly int _hashCode;

        public MethodKey(IMethod method, bool callVir)
        {
            _method = method;
            _callVir = callVir;
            _hashCode = HashUtil.CombineHash(MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method), callVir ? 1 : 0);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public bool Equals(MethodKey other)
        {
            return MethodEqualityComparer.CompareDeclaringTypes.Equals(_method, other._method) && _callVir == other._callVir;
        }
    }
}
