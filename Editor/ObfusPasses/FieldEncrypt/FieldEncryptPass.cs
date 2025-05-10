using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Obfuz.ObfusPasses.FieldEncrypt
{

    public class FieldEncryptPass : InstructionObfuscationPassBase
    {
        private readonly IEncryptPolicy _encryptionPolicy = new ConfigurableEncryptPolicy();
        private IFieldEncryptor _memoryEncryptor;

        public override void Start(ObfuscationPassContext ctx)
        {
            _memoryEncryptor = new DefaultFieldEncryptor(ctx.random, ctx.encryptor);

        }

        public override void Stop(ObfuscationPassContext ctx)
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return true;
        }

        private bool IsSupportedFieldType(TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();
            switch (type.ElementType)
            {
                case ElementType.I4:
                case ElementType.I8:
                case ElementType.U4:
                case ElementType.U8:
                case ElementType.R4:
                case ElementType.R8:
                return true;
                default: return false;
            }
        }

        protected override bool TryObfuscateInstruction(MethodDef callingMethod, Instruction inst, IList<Instruction> instructions, int instructionIndex, List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions)
        {
            Code code = inst.OpCode.Code;
            if (!(inst.Operand is IField field) || !field.IsField)
            {
                return false;
            }
            FieldDef fieldDef = field.ResolveFieldDefThrow();
            if (!IsSupportedFieldType(fieldDef.FieldSig.Type) || !_encryptionPolicy.NeedEncrypt(fieldDef))
            {
                return false;
            }
            var ctx = new MemoryEncryptionContext
            {
                module = callingMethod.Module,
                currentInstruction = inst,
            };
            switch (code)
            {
                case Code.Ldfld:
                {
                    _memoryEncryptor.Decrypt(callingMethod, fieldDef, outputInstructions, inst);
                    break;
                }
                case Code.Stfld:
                {
                    _memoryEncryptor.Encrypt(callingMethod, fieldDef, outputInstructions, inst);
                    break;
                }
                case Code.Ldsfld:
                {
                    _memoryEncryptor.Decrypt(callingMethod, fieldDef, outputInstructions, inst);
                    break;
                }
                case Code.Stsfld:
                {
                    _memoryEncryptor.Encrypt(callingMethod, fieldDef, outputInstructions, inst);
                    break;
                }
                case Code.Ldflda:
                case Code.Ldsflda:
                {
                    throw new System.Exception($"You shouldn't get reference to memory encryption field: {field}");
                }
                default: return false;
            }
            //Debug.Log($"memory encrypt field: {field}");
            return true;
        }
    }
}
