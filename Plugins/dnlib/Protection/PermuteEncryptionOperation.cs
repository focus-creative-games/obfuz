namespace dnlib.Protection {
	public class PermuteEncryptionOperation : IEncryptionInstruction {

		private readonly uint _xIndex1;
		private readonly uint _kIndex;
		private readonly uint _c;

		public PermuteEncryptionOperation(uint xIndex1, uint kIndex, uint c) {
			_xIndex1 = xIndex1;
			_kIndex = kIndex % EncryptionInfo.KeyLength;
			_c = c;
		}

		public void Encrypt(byte[] content, uint start, uint length, byte[] encryptionParam) {
			uint xIndex1 = _xIndex1 % length + start;
			uint xIndex2 = (encryptionParam[_kIndex] + _c) % length + start;
			byte temp = content[xIndex1];
			content[xIndex1] = content[xIndex2];
			content[xIndex2] = temp;
		}

		public string GenerateDecryptExpression(string dataVarName, string dataLengthVarName, string keyVarName) {
			return $"{{ uint32_t xIndex1 = {_xIndex1} % {dataLengthVarName}; uint32_t xIndex2 = ((uint32_t){keyVarName}[{_kIndex}] + {_c}u) % {dataLengthVarName}; byte temp = {dataVarName}[xIndex1]; {dataVarName}[xIndex1] = {dataVarName}[xIndex2]; {dataVarName}[xIndex2] = temp; }}";
		}
	}
}
