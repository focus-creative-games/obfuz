using dnlib.DotNet;
using dnlib.DotNet.Emit;
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

            public void Encrypt(FieldDef field, List<Instruction> outputInstructions, MemoryEncryptionContext ctx)
            {
                ElementType type = field.FieldType.RemovePinnedAndModifiers().ElementType;
                if (type == ElementType.R4)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, s_castFloatAsInt));
                    type = ElementType.I4;
                }
                else if (type == ElementType.R8)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, s_castDoubleAsLong));
                    type = ElementType.I8;
                }

                if (type == ElementType.I4)
                {
                    outputInstructions.Add(Instruction.CreateLdcI4(100));
                    outputInstructions.Add(Instruction.Create(OpCodes.Add));
                }
                else if (type == ElementType.I8)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Ldc_I8, 100L));
                    outputInstructions.Add(Instruction.Create(OpCodes.Add));
                }
                else
                {
                    throw new NotSupportedException($"Unsupported type {type} for MemoryEncryptor");
                }
                if (type == ElementType.R4)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, s_castIntAsFloat));
                    type = ElementType.I4;
                }
                else if (type == ElementType.R8)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, s_castLongAsDouble));
                    type = ElementType.I8;
                }
                outputInstructions.Add(ctx.currentInstruction.Clone());
            }

            public void Decrypt(FieldDef field, List<Instruction> outputInstructions, MemoryEncryptionContext ctx)
            {
                outputInstructions.Add(ctx.currentInstruction.Clone());
                ElementType type = field.FieldType.RemovePinnedAndModifiers().ElementType;
                if (type == ElementType.R4)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, s_castFloatAsInt));
                    type = ElementType.I4;
                }
                else if (type == ElementType.R8)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, s_castDoubleAsLong));
                    type = ElementType.I8;
                }

                if (type == ElementType.I4)
                {
                    outputInstructions.Add(Instruction.CreateLdcI4(100));
                    outputInstructions.Add(Instruction.Create(OpCodes.Sub));
                }
                else if (type == ElementType.I8)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Ldc_I8, 100L));
                    outputInstructions.Add(Instruction.Create(OpCodes.Sub));
                }
                else
                {
                    throw new NotSupportedException($"Unsupported type {type} for MemoryEncryptor");
                }
                if (type == ElementType.R4)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, s_castIntAsFloat));
                    type = ElementType.I4;
                }
                else if (type == ElementType.R8)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, s_castLongAsDouble));
                    type = ElementType.I8;
                }
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
