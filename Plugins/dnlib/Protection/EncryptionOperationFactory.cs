namespace dnlib.Protection {
	public class EncryptionOperationFactory {

		public static IEncryptionInstruction CreateEncryptor(int seed) {
			var r = new MyRandom(seed);
			EncryptionOperationType type = (EncryptionOperationType)r.Next((int)EncryptionOperationType.MaxTypeValue);
			var sr = new MyRandom(r.Next());
			switch (type) {
			case EncryptionOperationType.Xor: {
				return new XorEncryptionOperation(sr.NextU(), sr.NextU(), sr.NextU());
			}
			case EncryptionOperationType.Add: {
				return new AddEncryptionOperation(sr.NextU(), sr.NextU(), sr.NextU());
			}
			case EncryptionOperationType.Permute: {
				return new PermuteEncryptionOperation(sr.NextU(), sr.NextU(), sr.NextU());
			}
			case EncryptionOperationType.Permute2: {
				return new Permute2EncryptionOperation(sr.NextU(), sr.NextU(), sr.NextU());
			}
			case EncryptionOperationType.RotateLeftShift: {
				return new RotateLeftShiftEncryptionOperation(sr.NextU(), sr.NextU(), sr.NextU());
			}
			}
			return null;
		}
	}
}
