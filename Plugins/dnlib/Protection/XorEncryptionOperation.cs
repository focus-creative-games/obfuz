namespace dnlib.Protection {
	public class XorEncryptionOperation : IEncryptionInstruction {

		private readonly uint _xIndex;
		private readonly uint _kIndex;
		private readonly uint _c;

		public XorEncryptionOperation(uint xIndex, uint kIndex, uint c) {
			_xIndex = xIndex;
			_kIndex = kIndex % EncryptionInfo.KeyLength;
			_c = c & 0xFF;
		}

		public void Encrypt(byte[] content, uint start, uint length, byte[] encryptionParam) {
			uint xIndex = (_xIndex % length) + start;
			content[xIndex] = (byte)(content[xIndex] ^ encryptionParam[_kIndex] ^ _c);
		}

		public string GenerateDecryptExpression(string dataVarName, string dataLengthVarName, string keyVarName) {
			return $"{{ uint32_t xIndex = {_xIndex} % {dataLengthVarName}; {dataVarName}[xIndex] = (byte)((uint32_t){dataVarName}[xIndex] ^ (uint32_t){keyVarName}[{_kIndex}] ^ {_c}u); }}";
		}
	}
}
