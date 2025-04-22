using System.Collections.Generic;

namespace dnlib.Protection {
	public class EncryptionInstructionSet {

		private readonly int seed;

		public List<IEncryptionInstruction> Instructions { get; } = new List<IEncryptionInstruction>();

		public int Seed => seed;

		public int Count => Instructions.Count;

		public EncryptionInstructionSet(int seed) {
			this.seed = seed;

			var r = new MyRandom(seed);
			int count = 256;
			for (int i = 0; i < count; i++) {
				Instructions.Add(EncryptionOperationFactory.CreateEncryptor(r.Next()));
			}
		}

		public IEncryptionInstruction GetInstruction(int opCode) {
			return Instructions[opCode];
		}
	}
}
