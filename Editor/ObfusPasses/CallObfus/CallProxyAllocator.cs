using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
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
        public readonly int encryptOps;
        public readonly int salt;
        public readonly int encryptedIndex;

        public ProxyCallMethodData(MethodDef proxyMethod, int encryptOps, int salt, int encryptedIndex)
        {
            this.proxyMethod = proxyMethod;
            this.encryptOps = encryptOps;
            this.salt = salt;
            this.encryptedIndex = encryptedIndex;
        }
    }

    class ModuleCallProxyAllocator : IModuleEmitManager
    {
        private ModuleDef _module;
        private readonly IRandom _random;
        private readonly IEncryptor _encryptor;

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

            public int index;
            public int encryptedOps;
            public int salt;
            public int encryptedIndex;
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
            public List<CallInfo> methods = new List<CallInfo>();
        }

        private readonly Dictionary<MethodSig, List<DispatchMethodInfo>> _dispatchMethods = new Dictionary<MethodSig, List<DispatchMethodInfo>>(SignatureEqualityComparer.Instance);


        private TypeDef _proxyTypeDef;

        public ModuleCallProxyAllocator(IRandom random, IEncryptor encryptor)
        {
            _random = random;
            _encryptor = encryptor;
        }

        public void Init(ModuleDef mod)
        {
            _module = mod;
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
                    methodSig.Params.Insert(0, _module.CorLibTypes.IntPtr);
                    break;
                }
            }
            // extra param for index
            methodSig.Params.Add(_module.CorLibTypes.Int32);
            return MethodSig.CreateStatic(methodSig.RetType, methodSig.Params.ToArray());
        }

        private int GenerateSalt()
        {
            return _random.NextInt();
        }

        private int GenerateEncryptOps()
        {
            return _random.NextInt();
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

                int index = methodDispatcher.methods.Count;
                int encryptOps = GenerateEncryptOps();
                int salt = GenerateSalt();
                int encryptedIndex = _encryptor.Encrypt(index, encryptOps, salt);
                proxyInfo = new MethodProxyInfo()
                {
                    proxyMethod = methodDispatcher.methodDef,
                    index = index,
                    encryptedOps = encryptOps,
                    salt = salt,
                    encryptedIndex = encryptedIndex,
                };
                methodDispatcher.methods.Add(new CallInfo { method = method, callVir = callVir});
                _methodProxys.Add(key, proxyInfo);
            }
            return new ProxyCallMethodData(proxyInfo.proxyMethod, proxyInfo.encryptedOps, proxyInfo.salt, proxyInfo.encryptedIndex);
        }

        public void Done()
        {
            foreach (DispatchMethodInfo dispatchMethod in _dispatchMethods.Values.SelectMany(ms => ms))
            {
                var methodDef = dispatchMethod.methodDef;
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

    public class CallProxyAllocator
    {
        private readonly IRandom _random;
        private readonly IEncryptor _encryptor;

        public CallProxyAllocator(IRandom random, IEncryptor encryptor)
        {
            _random = random;
            _encryptor = encryptor;
        }

        private ModuleCallProxyAllocator GetModuleAllocator(ModuleDef mod)
        {
            return EmitManager.Ins.GetEmitManager<ModuleCallProxyAllocator>(mod, () => new ModuleCallProxyAllocator(_random, _encryptor));
        }

        public ProxyCallMethodData Allocate(ModuleDef mod, IMethod method, bool callVir)
        {
            ModuleCallProxyAllocator allocator = GetModuleAllocator(mod);
            return allocator.Allocate(method, callVir);
        }

        public void Done()
        {
            foreach (var allocator in EmitManager.Ins.GetEmitManagers<ModuleCallProxyAllocator>())
            {
                allocator.Done();
            }
        }
    }
}
