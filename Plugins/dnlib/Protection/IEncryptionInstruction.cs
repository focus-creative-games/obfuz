namespace dnlib.Protection {
	public interface IEncryptionInstruction {
		void Encrypt(byte[] content, uint start, uint length, byte[] encryptionParam);

		string GenerateDecryptExpression(string dataVarName, string dataLengthVarName, string keyVarName);
	}
}
