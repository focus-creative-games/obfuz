using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Obfuz.MemEncrypt
{
    public class DefaultMemoryEncryptor : MemoryEncryptorBase
    {

        private class ModuleDefaultMemoryEncryptor
        {
            private readonly ModuleDef _module;

            public ModuleDefaultMemoryEncryptor(ModuleDef module)
            {
                _module = module;
                InitMetadatas(module);
            }

            private static IMethod s_castIntAsFloat;
            private static IMethod s_castLongAsDouble;
            private static IMethod s_castFloatAsInt;
            private static IMethod s_castDoubleAsLong;

            private void InitMetadatas(ModuleDef mod)
            {
                if (s_castFloatAsInt != null)
                {
                    return;
                }
                var constUtilityType = typeof(ConstUtility);

                s_castIntAsFloat = mod.Import(constUtilityType.GetMethod("CastIntAsFloat"));
                Assert.IsNotNull(s_castIntAsFloat, "CastIntAsFloat not found");
                s_castLongAsDouble = mod.Import(constUtilityType.GetMethod("CastLongAsDouble"));
                Assert.IsNotNull(s_castLongAsDouble, "CastLongAsDouble not found");
                s_castFloatAsInt = mod.Import(constUtilityType.GetMethod("CastFloatAsInt"));
                Assert.IsNotNull(s_castFloatAsInt, "CastFloatAsInt not found");
                s_castDoubleAsLong = mod.Import(constUtilityType.GetMethod("CastDoubleAsLong"));
                Assert.IsNotNull(s_castDoubleAsLong, "CastDoubleAsLong not found");
            }


            private VariableEncryption CreateEncryption(ElementType type)
            {
                IRandom random = new RandomWithKey(new byte[16], 1234);
                switch (type)
                {
                    case ElementType.I4:
                    return new VariableEncryption(DataNodeType.Int32, random);
                    case ElementType.I8:
                    return new VariableEncryption(DataNodeType.Int64, random);
                    case ElementType.R4:
                    return new VariableEncryption(DataNodeType.Float32, random);
                    case ElementType.R8:
                    return new VariableEncryption(DataNodeType.Float64, random);
                    default: throw new Exception($"Unsupported type {type} for MemoryEncryptor");
                }
            }

            public void Encrypt(FieldDef field, List<Instruction> outputInstructions, MemoryEncryptionContext ctx)
            {

                ElementType type = field.FieldType.RemovePinnedAndModifiers().ElementType;
                var encryption = CreateEncryption(type);

                encryption.EmitTransform(outputInstructions, new EncryptionCompileContext { module = _module });

                outputInstructions.Add(ctx.currentInstruction.Clone());
            }

            public void Decrypt(FieldDef field, List<Instruction> outputInstructions, MemoryEncryptionContext ctx)
            {
                outputInstructions.Add(ctx.currentInstruction.Clone());
                ElementType type = field.FieldType.RemovePinnedAndModifiers().ElementType;
                var encryption = CreateEncryption(type);

                encryption.EmitRevertTransform(outputInstructions, new EncryptionCompileContext { module = _module });
            }
        }

        private readonly Dictionary<ModuleDef, ModuleDefaultMemoryEncryptor> _moduleEncryptors = new Dictionary<ModuleDef, ModuleDefaultMemoryEncryptor>();

        private ModuleDefaultMemoryEncryptor GetModuleEncryptor(ModuleDef module)
        {
            if (!_moduleEncryptors.TryGetValue(module, out var encryptor))
            {
                encryptor = new ModuleDefaultMemoryEncryptor(module);
                _moduleEncryptors.Add(module, encryptor);
            }
            return encryptor;
        }

        public override void Encrypt(FieldDef field, List<Instruction> outputInstructions, MemoryEncryptionContext ctx)
        {
            GetModuleEncryptor(ctx.module).Encrypt(field, outputInstructions, ctx);
        }

        public override void Decrypt(FieldDef field, List<Instruction> outputInstructions, MemoryEncryptionContext ctx)
        {
            GetModuleEncryptor(ctx.module).Decrypt(field, outputInstructions, ctx);
        }
    }
}
