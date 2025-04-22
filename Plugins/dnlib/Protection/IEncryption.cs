namespace dnlib.Protection {
	public interface IEncryption {

		byte[] EncParam { get; }

		Algorithm Algorithm { get; }

		int AlgoVersion { get; }

		EncryptionMethod SignatureEnc { get; }

		EncryptionMethod FileHeaderEnc { get; }

		EncryptionMethod StringEnc { get; }

		EncryptionMethod BlobEnc { get; }

		EncryptionMethod UserStringEnc { get; }

		EncryptionMethod LazyUserStringEnc { get; }

		EncryptionMethod TableEnc { get; }

		EncryptionMethod LazyTableEnc { get; }

		EncryptionMethod MethodBodyEnc { get; }
	}
}
