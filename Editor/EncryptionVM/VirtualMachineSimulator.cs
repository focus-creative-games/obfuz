using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Obfuz.EncryptionVM
{

    public class VirtualMachineSimulator : EncryptorBase
    {
        private readonly EncryptionInstructionWithOpCode[] _opCodes;
        private readonly int[] _secretKey;

        public VirtualMachineSimulator(VirtualMachine vm, byte[] byteSecretKey)
        {
            _opCodes = vm.opCodes;
            _secretKey = KeyGenerator.ConvertToIntKey(byteSecretKey);

            VerifyInstructions();
        }

        private void VerifyInstructions()
        {
            int value = 0x11223344;
            for (int i = 0; i < _opCodes.Length; i++)
            {
                int encryptedValue = _opCodes[i].Encrypt(value, _secretKey, i);
                int decryptedValue = _opCodes[i].Decrypt(encryptedValue, _secretKey, i);
                Assert.AreEqual(value, decryptedValue);
            }
        }

        private List<ushort> DecodeOps(int ops)
        {
            var codes = new List<ushort>();
            while (ops > 0)
            {
                var code = (ushort)(ops % _opCodes.Length);
                codes.Add(code);
                ops >>= 16;
            }
            return codes;
        }

        public override int Encrypt(int value, int ops, int salt)
        {
            var codes = DecodeOps(ops);
            foreach (var code in codes)
            {
                var opCode = _opCodes[code];
                value = opCode.Encrypt(value, _secretKey, salt);
            }
            return value;
        }

        public override int Decrypt(int value, int ops, int salt)
        {
            var codes = DecodeOps(ops);
            for (int i = codes.Count - 1; i >= 0; i--)
            {
                var opCode = _opCodes[codes[i]];
                value = opCode.Decrypt(value, _secretKey, salt);
            }
            return value;
        }
    }
}
