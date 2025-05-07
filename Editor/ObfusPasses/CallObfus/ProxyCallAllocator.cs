using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MethodImplAttributes = dnlib.DotNet.MethodImplAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace Obfuz.ObfusPasses.CallObfus
{
    public struct ProxyCallMethodData
    {
        public readonly MethodDef proxyMethod;
        public readonly int secret;

        public ProxyCallMethodData(MethodDef proxyMethod, int secret)
        {
            this.proxyMethod = proxyMethod;
            this.secret = secret;
        }
    }

    class ModuleDynamicProxyMethodAllocator
    {
        private readonly ModuleDef _module;
        private readonly IRandom _random;

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

        class MethodProxyInfo
        {
            public MethodDef proxyMethod;
            public int secret;
        }

        private readonly Dictionary<MethodKey, MethodProxyInfo> _methodProxys = new Dictionary<MethodKey, MethodProxyInfo>();


        const int maxProxyMethodPerDispatchMethod = 1000;

        class CallInfo
        {
            public IMethod method;
            public bool callVir;
        }

        class DispatchMethodInfo
        {
            public MethodDef methodDef;
            public int secret;
            public List<CallInfo> methods = new List<CallInfo>();
        }

        private readonly Dictionary<MethodSig, List<DispatchMethodInfo>> _dispatchMethods = new Dictionary<MethodSig, List<DispatchMethodInfo>>(SignatureEqualityComparer.Instance);


        private TypeDef _proxyTypeDef;

        public ModuleDynamicProxyMethodAllocator(ModuleDef module, IRandom random)
        {
            _module = module;
            _random = random;
        }

        private TypeDef CreateProxyTypeDef()
        {
            var typeDef = new TypeDefUser("$Obfuz$ProxyCall", _module.CorLibTypes.Object.ToTypeDefOrRef());
            typeDef.Attributes = TypeAttributes.NotPublic | TypeAttributes.Sealed;
            _module.EnableTypeDefFindCache = false;
            _module.Types.Add(typeDef);
            _module.EnableTypeDefFindCache = true;
            return typeDef;
        }

        private MethodDef CreateDispatchMethodInfo(MethodSig methodSig)
        {
            if (_proxyTypeDef == null)
            {
                _proxyTypeDef = CreateProxyTypeDef();
            }
            MethodDef methodDef = new MethodDefUser($"$Obfuz$ProxyCall$Dispatch${_proxyTypeDef.Methods.Count}", methodSig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Static | MethodAttributes.Private);
            methodDef.DeclaringType = _proxyTypeDef;
            return methodDef;
        }

        private MethodSig CreateDispatchMethodSig(IMethod method)
        {
            MethodSig methodSig = MetaUtil.ToSharedMethodSig(_module.CorLibTypes, MetaUtil.GetInflatedMethodSig(method));
            //MethodSig methodSig = MetaUtil.GetInflatedMethodSig(method).Clone();
            //methodSig.Params
            switch (MetaUtil.GetThisArgType(method))
            {
                case ThisArgType.Class:
                {
                    methodSig.Params.Insert(0, _module.CorLibTypes.Object);
                    break;
                }
                case ThisArgType.ValueType:
                {
                    methodSig.Params.Insert(0, _module.CorLibTypes.UIntPtr);
                    break;
                }
            }
            // extra param for secret
            methodSig.Params.Add(_module.CorLibTypes.Int32);
            return MethodSig.CreateStatic(methodSig.RetType, methodSig.Params.ToArray());
        }

        private DispatchMethodInfo GetDispatchMethod(IMethod method)
        {
            MethodSig methodSig = CreateDispatchMethodSig(method);
            if (!_dispatchMethods.TryGetValue(methodSig, out var dispatchMethods))
            {
                dispatchMethods = new List<DispatchMethodInfo>();
                _dispatchMethods.Add(methodSig, dispatchMethods);
            }
            if (dispatchMethods.Count == 0 || dispatchMethods.Last().methods.Count >= maxProxyMethodPerDispatchMethod)
            {
                var newDispatchMethodInfo = new DispatchMethodInfo
                {
                    methodDef = CreateDispatchMethodInfo(methodSig),
                    secret = 0,
                };
                dispatchMethods.Add(newDispatchMethodInfo);
            }
            return dispatchMethods.Last();
        }

        public ProxyCallMethodData Allocate(IMethod method, bool callVir)
        {
            var key = new MethodKey(method, callVir);
            if (!_methodProxys.TryGetValue(key, out var proxyInfo))
            {
                var methodDispatcher = GetDispatchMethod(method);
                proxyInfo = new MethodProxyInfo()
                {
                    proxyMethod = methodDispatcher.methodDef,
                    secret = methodDispatcher.methods.Count,
                };
                methodDispatcher.methods.Add(new CallInfo { method = method, callVir = callVir});
                _methodProxys.Add(key, proxyInfo);
            }
            return new ProxyCallMethodData(proxyInfo.proxyMethod, proxyInfo.secret);
        }

        public void Done()
        {
            foreach (DispatchMethodInfo dispatchMethod in _dispatchMethods.Values.SelectMany(ms => ms))
            {
                var methodDef = dispatchMethod.methodDef;
                var secret = dispatchMethod.secret;
                var methodSig = methodDef.MethodSig;


                var body = new CilBody();
                methodDef.Body = body;
                var ins = body.Instructions;

                foreach (Parameter param in methodDef.Parameters)
                {
                    ins.Add(Instruction.Create(OpCodes.Ldarg, param));
                }

                var switchCases = new List<Instruction>();
                var switchInst = Instruction.Create(OpCodes.Switch, switchCases);
                ins.Add(switchInst);
                var ret = Instruction.Create(OpCodes.Ret);
                foreach (CallInfo ci in dispatchMethod.methods)
                {
                    var callTargetMethod = Instruction.Create(ci.callVir ? OpCodes.Callvirt : OpCodes.Call, ci.method);
                    switchCases.Add(callTargetMethod);
                    ins.Add(callTargetMethod);
                    ins.Add(Instruction.Create(OpCodes.Br, ret));
                }
                ins.Add(ret);
            }
        }
    }

    public class ProxyCallAllocator
    {
        private readonly IRandom _random;

        private readonly Dictionary<ModuleDef, ModuleDynamicProxyMethodAllocator> _moduleAllocators = new Dictionary<ModuleDef, ModuleDynamicProxyMethodAllocator>();

        public ProxyCallAllocator(IRandom random)
        {
            _random = random;
        }

        public ProxyCallMethodData Allocate(ModuleDef mod, IMethod method, bool callVir)
        {
            if (!_moduleAllocators.TryGetValue(mod, out var allocator))
            {
                allocator = new ModuleDynamicProxyMethodAllocator(mod, _random);
                _moduleAllocators.Add(mod, allocator);
            }
            return allocator.Allocate(method, callVir);
        }

        public void Done()
        {
            foreach (var allocator in _moduleAllocators.Values)
            {
                allocator.Done();
            }
            _moduleAllocators.Clear();
        }
    }
}
