using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;

namespace Obfuz.Emit
{
    class ScopeLocalVariables : IDisposable
    {
        private readonly LocalVariableAllocator _localVariableAllocator;

        private readonly List<Local> _allocatedVars = new List<Local>();


        public ScopeLocalVariables(LocalVariableAllocator localVariableAllocator)
        {
            _localVariableAllocator = localVariableAllocator;
        }

        public Local AllocateLocal(TypeSig type)
        {
            var local = _localVariableAllocator.AllocateLocal(type);
            _allocatedVars.Add(local);
            return local;
        }

        public void Dispose()
        {
            foreach (var local in _allocatedVars)
            {
                _localVariableAllocator.ReturnLocal(local);
            }
        }
    }

    class LocalVariableAllocator
    {
        private readonly MethodDef _method;
        private readonly List<Local> _freeLocals = new List<Local>();

        public LocalVariableAllocator(MethodDef method)
        {
            _method = method;
        }

        public Local AllocateLocal(TypeSig type)
        {
            foreach (var local in _freeLocals)
            {
                if (TypeEqualityComparer.Instance.Equals(local.Type, type))
                {
                    _freeLocals.Remove(local);
                    return local;
                }
            }
            var newLocal = new Local(type);
            // _freeLocals.Add(newLocal);
            _method.Body.Variables.Add(newLocal);
            return newLocal;
        }

        public void ReturnLocal(Local local)
        {
            _freeLocals.Add(local);
        }
    }
}
