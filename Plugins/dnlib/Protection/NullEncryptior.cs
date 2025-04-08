using System.Text;
using dnlib.Protection;

namespace dnlib.Protection {

	/// <summary>
	/// 
	/// </summary>
	public class NullEncryptior : IBytesEncryptor {

		/// <summary>
		/// 
		/// </summary>
		public Algorithm Algorithm => Algorithm.None;

		public int AlgoVersion => 1;

		public byte[] Encrypt(byte[] content, byte[] encryptionParam) {
			return content;
		}

		public byte[] EncryptBlob(byte[] content, byte[] encryptionParam) {
			return Encrypt(content, encryptionParam);
		}
		public byte[] EncryptString(string content, byte[] encryptionParam) {
			return Encrypt(Encoding.UTF8.GetBytes(content), encryptionParam);
		}

		public byte[] EncryptUserString(string content, byte[] encryptionParam) {
			return EncryptString(content, encryptionParam);
		}
	}
}
