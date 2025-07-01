using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NUnit.Framework;
using Obfuz.Data;
using Obfuz.Emit;
using Obfuz.Settings;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.ObfusPasses.CallObfus
{

    struct DelegateProxyMethodData
    {
        public readonly FieldDef delegateInstanceField;
        public readonly MethodDef delegateInvokeMethod;

        public DelegateProxyMethodData(FieldDef delegateInstanceField, MethodDef delegateInvokeMethod)
        {
            this.delegateInstanceField = delegateInstanceField;
            this.delegateInvokeMethod = delegateInvokeMethod;
        }
    }

    class ModuleDelegateProxyAllocator : IGroupByModuleEntity
    {
        private readonly GroupByModuleEntityManager _moduleEntityManager;
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private readonly RvaDataAllocator _rvaDataAllocator;
        private readonly CallObfuscationSettingsFacade _settings;
        private readonly CachedDictionary<MethodSig, TypeDef> _delegateTypes;
        private readonly HashSet<string> _allocatedDelegateNames = new HashSet<string>();

        private ModuleDef _module;

        private TypeDef _delegateInstanceHolderType;
        private bool _done;

        class CallInfo
        {
            public string key1;
            public int key2;
            public IMethod method;
            public bool callVir;

            public int index;
            public TypeDef delegateType;
            public FieldDef delegateInstanceField;
            public MethodDef delegateInvokeMethod;
            public MethodDef proxyMethod;
        }
        private readonly Dictionary<MethodKey, CallInfo> _callMethods = new Dictionary<MethodKey, CallInfo>();

        public ModuleDelegateProxyAllocator(GroupByModuleEntityManager moduleEntityManager, EncryptionScopeProvider encryptionScopeProvider, RvaDataAllocator rvaDataAllocator, CallObfuscationSettingsFacade settings)
        {
            _moduleEntityManager = moduleEntityManager;
            _encryptionScopeProvider = encryptionScopeProvider;
            _rvaDataAllocator = rvaDataAllocator;
            _settings = settings;
            _delegateTypes = new CachedDictionary<MethodSig, TypeDef>(SignatureEqualityComparer.Instance, CreateDelegateForSignature);
        }

        public void Init(ModuleDef mod)
        {
            _module = mod;

            _delegateInstanceHolderType = CreateDelegateInstanceHolderTypeDef();
        }

        private string AllocateDelegateTypeName(MethodSig delegateInvokeSig)
        {
            uint hashCode = (uint)SignatureEqualityComparer.Instance.GetHashCode(delegateInvokeSig);
            string typeName = $"$Obfuz$Delegate_{hashCode}";
            if (_allocatedDelegateNames.Add(typeName))
            {
                return typeName;
            }
            for (int i = 0; ;i++)
            {
                typeName = $"$Obfuz$Delegate_{hashCode}_{i}";
                if (_allocatedDelegateNames.Add(typeName))
                {
                    return typeName;
                }
            }
        }

        private TypeDef CreateDelegateForSignature(MethodSig delegateInvokeSig)
        {
            using (var scope = new DisableTypeDefFindCacheScope(_module))
            {

                string typeName = AllocateDelegateTypeName(delegateInvokeSig);
                _module.Import(typeof(MulticastDelegate));

                TypeDef delegateType = new TypeDefUser("", typeName, _module.CorLibTypes.GetTypeRef("System", "MulticastDelegate"));
                delegateType.Attributes = TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public;
                _module.Types.Add(delegateType);

                MethodDef ctor = new MethodDefUser(
                    ".ctor",
                    MethodSig.CreateInstance(_module.CorLibTypes.Void, _module.CorLibTypes.Object, _module.CorLibTypes.IntPtr),
                    MethodImplAttributes.Runtime,
                    MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Public
                );
                ctor.DeclaringType = delegateType;


                MethodDef invokeMethod = new MethodDefUser(
                    "Invoke",
                    MethodSig.CreateInstance(delegateInvokeSig.RetType, delegateInvokeSig.Params.ToArray()),
                    MethodImplAttributes.Runtime,
                    MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual
                );
                invokeMethod.DeclaringType = delegateType;
                return delegateType;
            }
        }

        private TypeDef CreateDelegateInstanceHolderTypeDef()
        {
            using (var scope = new DisableTypeDefFindCacheScope(_module))
            {
                string typeName = "$Obfuz$DelegateInstanceHolder";
                TypeDef holderType = new TypeDefUser("", typeName, _module.CorLibTypes.Object.ToTypeDefOrRef());
                holderType.Attributes = TypeAttributes.Class | TypeAttributes.Public;
                _module.Types.Add(holderType);
                return holderType;
            }
        }

        private string AllocateFieldName(IMethod method, bool callVir)
        {
            uint hashCode = (uint)MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method);
            string typeName = $"$Obfuz$Delegate$Field_{hashCode}_{callVir}";
            if (_allocatedDelegateNames.Add(typeName))
            {
                return typeName;
            }
            for (int i = 0; ; i++)
            {
                typeName = $"$Obfuz$Delegate$Field_{hashCode}_{callVir}_{i}";
                if (_allocatedDelegateNames.Add(typeName))
                {
                    return typeName;
                }
            }
        }

        private MethodDef CreateProxyMethod(string name, IMethod calledMethod, bool callVir, MethodSig delegateInvokeSig)
        {
            var proxyMethod = new MethodDefUser(name, delegateInvokeSig, MethodImplAttributes.Managed, MethodAttributes.Public | MethodAttributes.Static);
            var body = new CilBody();
            proxyMethod.Body = body;
            var ins = body.Instructions;

            foreach (Parameter param in proxyMethod.Parameters)
            {
                ins.Add(Instruction.Create(OpCodes.Ldarg, param));
            }

            ins.Add(Instruction.Create(callVir ? OpCodes.Callvirt : OpCodes.Call, calledMethod));
            ins.Add(Instruction.Create(OpCodes.Ret));
            return proxyMethod;
        }

        public DelegateProxyMethodData Allocate(IMethod method, bool callVir, MethodSig delegateInvokeSig)
        {
            var key = new MethodKey(method, callVir);
            if (!_callMethods.TryGetValue(key, out var callInfo))
            {
                TypeDef delegateType = _delegateTypes.GetValue(delegateInvokeSig);
                MethodDef delegateInvokeMethod = delegateType.FindMethod("Invoke");
                string fieldName = AllocateFieldName(method, callVir);
                FieldDef delegateInstanceField = new FieldDefUser(fieldName, new FieldSig(delegateType.ToTypeSig()), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly);
                string key1 = $"{method.FullName}_{callVir}";
                callInfo = new CallInfo
                {
                    key1 = key1,
                    key2 = HashUtil.ComputePrimitiveOrStringOrBytesHashCode(key1) * 33445566,
                    method = method,
                    callVir = callVir,
                    delegateType = delegateType,
                    delegateInstanceField = delegateInstanceField,
                    delegateInvokeMethod = delegateInvokeMethod,
                    proxyMethod = CreateProxyMethod($"{fieldName}$Proxy", method, callVir, delegateInvokeSig),
                };
                _callMethods.Add(key, callInfo);
            }
            return new DelegateProxyMethodData(callInfo.delegateInstanceField, callInfo.delegateInvokeMethod);
        }

        public void Done()
        {
            if (_done)
            {
                throw new Exception("Already done");
            }
            _done = true;

            // for stable order, we sort methods by name
            List<CallInfo> callMethodList = _callMethods.Values.ToList();
            callMethodList.Sort((a, b) => a.key1.CompareTo(b.key1));

            var cctor = new MethodDefUser(".cctor",
                MethodSig.CreateStatic(_module.CorLibTypes.Void),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Private);
            cctor.DeclaringType = _delegateInstanceHolderType;
            //_rvaTypeDef.Methods.Add(cctor);
            var body = new CilBody();
            cctor.Body = body;
            var ins = body.Instructions;

            // var arr = new array[];
            // var d = new delegate;
            // arr[index] = d;
            int index = 0;
            ins.Add(Instruction.CreateLdcI4(callMethodList.Count));
            ins.Add(Instruction.Create(OpCodes.Newarr, _module.CorLibTypes.Object));
            foreach (CallInfo ci in callMethodList)
            {
                ci.index = index;
                _delegateInstanceHolderType.Methods.Add(ci.proxyMethod);
                ins.Add(Instruction.Create(OpCodes.Dup));
                ins.Add(Instruction.CreateLdcI4(index));
                ins.Add(Instruction.Create(OpCodes.Ldnull));
                ins.Add(Instruction.Create(OpCodes.Ldftn, ci.proxyMethod));
                MethodDef ctor = ci.delegateType.FindMethod(".ctor");
                Assert.NotNull(ctor, $"Delegate type {ci.delegateType.FullName} does not have a constructor.");
                ins.Add(Instruction.Create(OpCodes.Newobj, ctor));
                ins.Add(Instruction.Create(OpCodes.Stelem_Ref));
                ++index;
            }



            List<CallInfo> callMethodList2 = callMethodList.ToList();
            callMethodList2.Sort((a, b) => a.key2.CompareTo(b.key2));

            EncryptionScopeInfo encryptionScope = _encryptionScopeProvider.GetScope(_module);
            DefaultMetadataImporter importer = _moduleEntityManager.GetDefaultModuleMetadataImporter(_module, _encryptionScopeProvider);
            foreach (CallInfo ci in callMethodList2)
            {
                _delegateInstanceHolderType.Fields.Add(ci.delegateInstanceField);


                ins.Add(Instruction.Create(OpCodes.Dup));

                IRandom localRandom = encryptionScope.localRandomCreator(HashUtil.ComputePrimitiveOrStringOrBytesHashCode(ci.key1));
                int ops = EncryptionUtil.GenerateEncryptionOpCodes(localRandom, encryptionScope.encryptor, 4);
                int salt = localRandom.NextInt();

                int encryptedValue = encryptionScope.encryptor.Encrypt(ci.index, ops, salt);
                RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                ins.Add(Instruction.CreateLdcI4(ops));
                ins.Add(Instruction.CreateLdcI4(salt));
                ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaInt));
                ins.Add(Instruction.Create(OpCodes.Ldelem_Ref));
                ins.Add(Instruction.Create(OpCodes.Stsfld, ci.delegateInstanceField));
            }

            ins.Add(Instruction.Create(OpCodes.Pop));
            ins.Add(Instruction.Create(OpCodes.Ret));
        }
    }

    class DelegateProxyAllocator
    {
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private readonly GroupByModuleEntityManager _moduleEntityManager;
        private readonly CallObfuscationSettingsFacade _settings;
        private readonly RvaDataAllocator _rvaDataAllocator;

        public DelegateProxyAllocator(EncryptionScopeProvider encryptionScopeProvider, GroupByModuleEntityManager moduleEntityManager, RvaDataAllocator rvaDataAllocator, CallObfuscationSettingsFacade settings)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
            _moduleEntityManager = moduleEntityManager;
            _rvaDataAllocator = rvaDataAllocator;
            _settings = settings;
        }

        public ModuleDelegateProxyAllocator GetModuleAllocator(ModuleDef mod)
        {
            return _moduleEntityManager.GetEntity<ModuleDelegateProxyAllocator>(mod, () => new ModuleDelegateProxyAllocator(_moduleEntityManager, _encryptionScopeProvider, _rvaDataAllocator, _settings));
        }

        public void Done()
        {
            foreach (var allocator in _moduleEntityManager.GetEntities<ModuleDelegateProxyAllocator>())
            {
                allocator.Done();
            }
        }
    }
}
