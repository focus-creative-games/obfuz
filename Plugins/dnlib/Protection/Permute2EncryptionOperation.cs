namespace dnlib.Protection {
	public class Permute2EncryptionOperation : IEncryptionInstruction {

		private readonly uint _xIndex1;
		private readonly uint _kIndex;
		private readonly uint _c;

		public Permute2EncryptionOperation(uint xIndex1, uint kIndex, uint c) {
			_xIndex1 = xIndex1;
			_kIndex = kIndex % EncryptionInfo.KeyLength;
			_c = c;
		}

		public void Encrypt(byte[] content, uint start, uint length, byte[] encryptionParam) {
			uint xIndex1 = _xIndex1 % length + start;
			uint xIndex2 = (encryptionParam[_kIndex] + _c) % length + start;
			byte a = (byte)(content[xIndex1] + content[xIndex2]);
			byte b = (byte)(content[xIndex2] + 1);
			content[xIndex1] = a;
			content[xIndex2] = b;
		}

		public string GenerateDecryptExpression(string dataVarName, string dataLengthVarName, string keyVarName) {
			return $"{{ uint32_t xIndex1 = {_xIndex1} % {dataLengthVarName}; uint32_t xIndex2 = ((uint32_t){keyVarName}[{_kIndex}] + {_c}u) % {dataLengthVarName}; byte a = {dataVarName}[xIndex1]; byte b = {dataVarName}[xIndex2] - 1; {dataVarName}[xIndex1] = a - b; {dataVarName}[xIndex2] = b; }}";
		}
	}
}
