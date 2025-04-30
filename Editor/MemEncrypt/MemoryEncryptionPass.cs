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

        public override void Start(ObfuscatorContext ctx)
        {

        }

        public override void Stop(ObfuscatorContext ctx)
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
            switch (code)
            {
                case Code.Ldfld:
                {
                    break;
                }
                case Code.Stfld:
                {
                    break;
                }
                case Code.Ldsfld:
                {
                    break;
                }
                case Code.Stsfld:
                {
                    break;
                }
                case Code.Ldflda:
                case Code.Ldsflda:
                {
                    throw new System.Exception($"You shouldn't get reference to memory encryption field: {field}");
                }
                default: return false;
            }
            Debug.Log($"memory encrypt field: {field}");
            outputInstructions.Add(inst);
            return true;
        }
    }
}
