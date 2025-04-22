namespace dnlib.Protection {
	public class RotateLeftShiftEncryptionOperation : IEncryptionInstruction {

		private readonly uint _xIndex;
		private readonly uint _kIndex;
		private readonly uint _c;

		public RotateLeftShiftEncryptionOperation(uint xIndex, uint kIndex, uint c) {
			_xIndex = xIndex;
			_kIndex = kIndex % EncryptionInfo.KeyLength;
			_c = c;
		}

		public void Encrypt(byte[] content, uint start, uint length, byte[] encryptionParam) {
			uint xIndex = _xIndex % length + start;
			int shift = (int)((encryptionParam[_kIndex] + _c) % 8);
			uint v = content[xIndex];
			content[xIndex] = (byte)((v << shift) | (v >> (8 - shift)));
		}

		public string GenerateDecryptExpression(string dataVarName, string dataLengthVarName, string keyVarName) {
			return $"{{ uint32_t xIndex = {_xIndex} % {dataLengthVarName}; uint32_t shift = (uint32_t)({keyVarName}[{_kIndex}] + {_c}u) % 8; {dataVarName}[xIndex] = (byte)(((uint32_t){dataVarName}[xIndex] >> shift) | ((uint32_t){dataVarName}[xIndex] << (8 - shift))); }}";
		}
	}
}
