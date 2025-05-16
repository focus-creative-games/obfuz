using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Editor;
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
        public readonly int index;

        public ProxyCallMethodData(MethodDef proxyMethod, int encryptOps, int salt, int encryptedIndex, int index)
        {
            this.proxyMethod = proxyMethod;
            this.encryptOps = encryptOps;
            this.salt = salt;
            this.encryptedIndex = encryptedIndex;
            this.index = index;
        }
    }

    class ModuleCallProxyAllocator : IGroupByModuleEntity
    {
        private ModuleDef _module;
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private readonly int _encryptionLevel;

        private EncryptionScopeInfo _encryptionScope;
        private bool _done;

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

        public ModuleCallProxyAllocator(EncryptionScopeProvider encryptionScopeProvider, int encryptionLevel)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
            _encryptionLevel = encryptionLevel;
        }

        public void Init(ModuleDef mod)
        {
            _module = mod;
            _encryptionScope = _encryptionScopeProvider.GetScope(mod);
        }

        private TypeDef CreateProxyTypeDef()
        {
            var typeDef = new TypeDefUser($"{ConstValues.ObfuzMetadataNamePrefix}ProxyCall", _module.CorLibTypes.Object.ToTypeDefOrRef());
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
            MethodDef methodDef = new MethodDefUser($"{ConstValues.ObfuzMetadataNamePrefix}ProxyCall$Dispatch${_proxyTypeDef.Methods.Count}", methodSig,
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

        private int GenerateSalt(IRandom random)
        {
            return random.NextInt();
        }

        private int GenerateEncryptOps(IRandom random)
        {
            return EncryptionUtil.GenerateEncryptionOpCodes(random, _encryptionScope.encryptor, _encryptionLevel);
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

        private IRandom CreateRandomForMethod(IMethod method, bool callVir)
        {
            int seed = MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method);
            return _encryptionScope.localRandomCreator(seed);
        }

        public ProxyCallMethodData Allocate(IMethod method, bool callVir)
        {
            if (_done)
            {
                throw new Exception("can't Allocate after done");
            }
            var key = new MethodKey(method, callVir);
            if (!_methodProxys.TryGetValue(key, out var proxyInfo))
            {
                var methodDispatcher = GetDispatchMethod(method);

                int index = methodDispatcher.methods.Count;
                IRandom localRandom = CreateRandomForMethod(method, callVir);
                int encryptOps = GenerateEncryptOps(localRandom);
                int salt = GenerateSalt(localRandom);
                int encryptedIndex = _encryptionScope.encryptor.Encrypt(index, encryptOps, salt);
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
            return new ProxyCallMethodData(proxyInfo.proxyMethod, proxyInfo.encryptedOps, proxyInfo.salt, proxyInfo.encryptedIndex, proxyInfo.index);
        }

        public void Done()
        {
            if (_done)
            {
                throw new Exception("Already done");
            }
            _done = true;

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
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private GroupByModuleEntityManager _moduleEntityManager;
        private readonly int _encryptionLevel;

        public CallProxyAllocator(EncryptionScopeProvider encryptionScopeProvider, GroupByModuleEntityManager moduleEntityManager, int encryptionLevel)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
            _moduleEntityManager = moduleEntityManager;
            _encryptionLevel = encryptionLevel;
        }

        private ModuleCallProxyAllocator GetModuleAllocator(ModuleDef mod)
        {
            return _moduleEntityManager.GetEntity<ModuleCallProxyAllocator>(mod, () => new ModuleCallProxyAllocator(_encryptionScopeProvider, _encryptionLevel));
        }

        public ProxyCallMethodData Allocate(ModuleDef mod, IMethod method, bool callVir)
        {
            ModuleCallProxyAllocator allocator = GetModuleAllocator(mod);
            return allocator.Allocate(method, callVir);
        }

        public void Done()
        {
            foreach (var allocator in _moduleEntityManager.GetEntities<ModuleCallProxyAllocator>())
            {
                allocator.Done();
            }
        }
    }
}
