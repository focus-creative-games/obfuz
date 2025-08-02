using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.PolymorphicWriter.Utilities;
using Obfuz.Settings;
using Obfuz.Utils;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using FieldAttributes = dnlib.DotNet.FieldAttributes;
using KeyGenerator = Obfuz.Utils.KeyGenerator;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace Obfuz.ObfusPasses.Watermark
{
    public class WatermarkPass : ObfuscationPassBase
    {
        private readonly WatermarkSettingsFacade _watermarkSettings;

        public WatermarkPass(WatermarkSettingsFacade watermarkSettingsFacade)
        {
            this._watermarkSettings = watermarkSettingsFacade;
        }

        public override ObfuscationPassType Type => ObfuscationPassType.WaterMark;

        public override void Start()
        {
        }

        public override void Stop()
        {

        }

        public override void Process()
        {
            var ctx = ObfuscationPassContext.Current;
            foreach (ModuleDef mod in ctx.modulesToObfuscate)
            {
                AddWaterMarkToAssembly(mod, _watermarkSettings.text);
            }
        }

        private TypeDef GetDataHolderType(ModuleDef module, TypeDef declaringType, int size)
        {

            using (var scope = new DisableTypeDefFindCacheScope(module))
            {
                var dataHolderType = new TypeDefUser($"$Obfuz$WatermarkDataHolderSize{size}_{declaringType.NestedTypes.Count}", module.Import(typeof(System.ValueType)));
                dataHolderType.Attributes = TypeAttributes.NestedPrivate | TypeAttributes.Sealed;
                dataHolderType.Layout = TypeAttributes.ExplicitLayout;
                dataHolderType.PackingSize = 1;
                dataHolderType.ClassSize = (uint)size;
                dataHolderType.DeclaringType = declaringType;
                return dataHolderType;
            }
        }

        private void AddWaterMarkToAssembly(ModuleDef module, string waterMarkText)
        {
            string finalWatermarkText = $"{waterMarkText} [{module.Name}]";
            TypeDef moduleType = module.FindNormal("<PrivateImplementationDetails>");
            if (moduleType == null)
            {
                //throw new Exception($"Module '{module.Name}' does not contain a '<PrivateImplementationDetails>' type.");
                moduleType = new TypeDefUser("<PrivateImplementationDetails>", module.Import(typeof(object)));
                moduleType.Attributes = TypeAttributes.NotPublic | TypeAttributes.Sealed;
                moduleType.CustomAttributes.Add(new CustomAttribute(module.Import(module.Import(typeof(CompilerGeneratedAttribute)).ResolveTypeDefThrow().FindDefaultConstructor())));
                module.Types.Add(moduleType);
            }

            var ctx = ObfuscationPassContext.Current;
            EncryptionScopeInfo encryptionScope = ctx.moduleEntityManager.EncryptionScopeProvider.GetScope(module);
            var random = encryptionScope.localRandomCreator(0);
            byte[] watermarkBytes = KeyGenerator.GenerateKey(finalWatermarkText, _watermarkSettings.signatureLength);
            for (int subIndex = 0; subIndex < watermarkBytes.Length;)
            {
                int subSegmentLength = Math.Min(random.NextInt(16, 32) & ~3, watermarkBytes.Length - subIndex);
                int paddingLength = random.NextInt(8, 32) & ~3;
                int totalLength = subSegmentLength + paddingLength;
                TypeDef dataHolderType = GetDataHolderType(module, moduleType, totalLength);

                byte[] subSegment = new byte[totalLength];
                Buffer.BlockCopy(watermarkBytes, subIndex, subSegment, 0, subSegmentLength);

                for (int i = subSegmentLength; i < totalLength; i++)
                {
                    subSegment[i] = (byte)random.NextInt(0, 256);
                }

                subIndex += subSegmentLength;
                var field = new FieldDefUser($"$Obfuz$WatermarkDataHolderField{moduleType.Fields.Count}", 
                    new FieldSig(dataHolderType.ToTypeSig()),
                    FieldAttributes.Assembly | FieldAttributes.InitOnly | FieldAttributes.Static | FieldAttributes.HasFieldRVA);
                field.DeclaringType = moduleType;
                field.InitialValue = subSegment;
            }

            var moduleTypeFields = moduleType.Fields.ToList();
            RandomUtil.ShuffleList(moduleTypeFields, random);
            moduleType.Fields.Clear();
            foreach (var field in moduleTypeFields)
            {
                moduleType.Fields.Add(field);
            }
        }

        
    }
}
