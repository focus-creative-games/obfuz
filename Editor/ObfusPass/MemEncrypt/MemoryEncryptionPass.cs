using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz;
using Obfuz.MemEncrypt;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Obfuz.MemEncrypt
{

    public class MemoryEncryptionPass : MethodBodyObfuscationPassBase
    {
        private readonly IEncryptionPolicy _encryptionPolicy = new ConfigEncryptionPolicy();
        private readonly IMemoryEncryptor _memoryEncryptor = new DefaultMemoryEncryptor();

        public override void Start(ObfuscationPassContext ctx)
        {

        }

        public override void Stop(ObfuscationPassContext ctx)
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return true;
        }

        private FieldDef TryResolveFieldDef(IField field)
        {
            if (field is FieldDef fieldDef)
            {
                return fieldDef;
            }
            if (field is MemberRef memberRef)
            {
                return memberRef.ResolveFieldDef();
            }
            throw new System.Exception($"Cannot resolve field: {field}");
        }

        protected override bool TryObfuscateInstruction(MethodDef callingMethod, Instruction inst, IList<Instruction> instructions, int instructionIndex, List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions)
        {
            Code code = inst.OpCode.Code;
            if (!(inst.Operand is IField field))
            {
                return false;
            }
            FieldDef fieldDef = TryResolveFieldDef(field);
            if (fieldDef == null)
            {
                return false;
            }
            if (!_encryptionPolicy.NeedEncrypt(fieldDef))
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
                    _memoryEncryptor.Decrypt(fieldDef, outputInstructions, ctx);
                    break;
                }
                case Code.Stfld:
                {
                    _memoryEncryptor.Encrypt(fieldDef, outputInstructions, ctx);
                    break;
                }
                case Code.Ldsfld:
                {
                    _memoryEncryptor.Decrypt(fieldDef, outputInstructions, ctx);
                    break;
                }
                case Code.Stsfld:
                {
                    _memoryEncryptor.Encrypt(fieldDef, outputInstructions, ctx);
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
