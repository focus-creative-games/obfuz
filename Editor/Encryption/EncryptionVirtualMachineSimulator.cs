using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Obfuz.Encryption
{

    public class EncryptionVirtualMachineSimulator : EncryptorBase
    {
        private readonly EncryptOpCode[] _opCodes;
        private readonly int[] _secretKey;

        public EncryptionVirtualMachineSimulator(EncryptOpCode[] opCodes, int[] secretKey)
        {
            _opCodes = opCodes;
            // should be power of 2
            Assert.AreEqual(0, opCodes.Length ^ (opCodes.Length - 1));
            _secretKey = secretKey;
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
