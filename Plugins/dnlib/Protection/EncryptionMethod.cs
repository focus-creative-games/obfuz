using System;
using System.Collections.Generic;
using System.Linq;

namespace dnlib.Protection {
	public class EncryptionMethod {

		public class InstructionInfo {
			public readonly int opCode;

			public readonly IEncryptionInstruction oper;

			public InstructionInfo(int index, IEncryptionInstruction oper) {
				this.opCode = index;
				this.oper = oper;
			}
		}

		public List<InstructionInfo> Insts { get; } = new List<InstructionInfo>();

		public byte[] GetDecryptionOpCode() {
			return Insts.Select(e => (byte)e.opCode).Reverse().ToArray();
		}

		public EncryptionMethod(EncryptionInstructionSet re, int seed, int instCount) {
			var r = new MyRandom(seed);
			int instSetCount = re.Count;
			for (int i = 0; i < instCount; i++) {
				int index = r.Next(instSetCount);
				Insts.Add(new InstructionInfo(index, re.GetInstruction(index)));
			}
		}

		public EncryptionMethod(EncryptionInstructionSet re, byte[] opCodes) {
			foreach (byte op in opCodes) {
				Insts.Add(new InstructionInfo(op, re.GetInstruction(op)));
			}
		}

		public void Encrypt(byte[] content, uint start, uint length, byte[] encryptionParam) {
			for (int i = 0; i < Insts.Count; i++) {
				Insts[i].oper.Encrypt(content, start, length, encryptionParam);
			}
		}

		public void EncryptBySegment(byte[] content, uint start, uint length, byte[] encryptionParam, uint segmentSize) {
			for (uint i = 0; i < length; i += segmentSize) {
				uint len = Math.Min(segmentSize, length - i);
				foreach (var inst in Insts) {
					inst.oper.Encrypt(content, start + i, len, encryptionParam);
				}
			}
		}
	}
}
