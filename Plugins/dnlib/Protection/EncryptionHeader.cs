using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.Protection {


	public enum Algorithm {
		None,
		Custom,
	}

	/// <summary>
	/// 
	/// </summary>
	public class EncryptionInfo {
		public const int KeyLength = 256;

		public Algorithm algorithm;
		public int version;
		public byte[] key; // 256 bytes
		public byte[] headerEncOpCodes;
		public byte[] stringEncOpCodes;
		public byte[] blobEncOpCodes;
		public byte[] userStringEncOpCodes;
		public byte[] lazyUserStringEncOpCodes;
		public byte[] tableEncOpCodes;
		public byte[] lazyTableEncOpCodes;
		public byte[] methodBoyEncOpCodes;


		private static byte[] originalSignature { get; } = Encoding.UTF8.GetBytes("Hello, HybridCLR");

		public byte[] encryptedSignature;

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public uint GetVirtualSize() => 4 
			+ 4
			+ KeyLength
			+ 4 + GetAlignedLength(headerEncOpCodes)
			+ 4 + GetAlignedLength(stringEncOpCodes)
			+ 4 + GetAlignedLength(blobEncOpCodes)
			+ 4 + GetAlignedLength(userStringEncOpCodes)
			+ 4 + GetAlignedLength(lazyUserStringEncOpCodes)
			+ 4 + GetAlignedLength(tableEncOpCodes)
			+ 4 + GetAlignedLength(lazyTableEncOpCodes)
			+ 4 + GetAlignedLength(methodBoyEncOpCodes)
			+ 16;


		public static uint GetAlignedLength(byte[] bytes) {
			return (uint)((bytes.Length + 3) & ~0x3);
		}

		public void WriteBytesAigned(DataWriter writer, byte[] bytes) {
			writer.WriteUInt32((uint)bytes.Length);
			writer.WriteBytes(bytes);
			int padding = (int)(4 - (writer.Position % 4));
			if (padding != 4) {
				writer.WriteBytes(new byte[padding]);
			}
		}

		public void WriteTo(DataWriter writer) {
			writer.WriteUInt32((uint)algorithm);
			writer.WriteUInt32((uint)version);
			Debug.Assert(key.Length == KeyLength);
			writer.WriteBytes(key);

			WriteBytesAigned(writer, headerEncOpCodes);
			WriteBytesAigned(writer, stringEncOpCodes);
			WriteBytesAigned(writer, blobEncOpCodes);
			WriteBytesAigned(writer, userStringEncOpCodes);
			WriteBytesAigned(writer, lazyUserStringEncOpCodes);
			WriteBytesAigned(writer, tableEncOpCodes);
			WriteBytesAigned(writer, lazyTableEncOpCodes);
			WriteBytesAigned(writer, methodBoyEncOpCodes);

			encryptedSignature = CreateEncryptedSignature();
			Debug.Assert(encryptedSignature.Length == 16);
			writer.WriteBytes(encryptedSignature);
		}

		private byte[] CreateEncryptedSignature() {
			byte[] encSig = new byte[originalSignature.Length];
			Array.Copy(originalSignature, encSig, originalSignature.Length);
			EncryptionContext.Encryption.SignatureEnc.Encrypt(encSig, 0, (uint)encSig.Length, key);
			return encSig;
		}
		}

	/// <summary>
	/// 
	/// </summary>
	public interface IBytesEncryptor {

		Algorithm Algorithm { get; }

		Int32 AlgoVersion { get; }

		byte[] Encrypt(byte[] content, byte[] encryptionParam);

		byte[] EncryptString(string content, byte[] encryptionParam);
		byte[] EncryptUserString(string content, byte[] encryptionParam);

		byte[] EncryptBlob(byte[] content, byte[] encryptionParam);
	}



	/// <summary>
	/// format:
	/// file magic numer :=  'CDPH'
	/// encryption_info := |algorithm : uint32_t | version: uint32_t | param: uint8_t[32]| signature: uint8_t[128]|
	/// cli_header := | rva: uint32_t | size: uint32_t | entryPointToken: uint32_t |
	/// section_count : int32_t
	/// section_descs := | Section { raw_offset: uint32_t | raw_size : uint32_t | rva_start: uint32_t | virtual_size: uint32_t } | ...
	/// secion_datas := | uint8_t[length] | ...
	/// </summary>
	public class EncryptionHeader : IChunk {



		public ImageCor20Header imageCor20Header;

		// =================


		public string fileMagic = "CDPH";

		public int formatVersion;

		public EncryptionInfo encryptionInfo;

		public uint entryPointToken;
		public RVA metadataRva;
		public uint metadataSize;

		public List<PESection> sections;


		private FileOffset _fileOffset;
		

		/// <summary>
		/// 
		/// </summary>
		public FileOffset FileOffset => _fileOffset;

		private RVA _rva;
		public RVA RVA => _rva;

		public uint CalculateAlignment() => 4;

		private uint _length;
		public uint GetFileLength() => _length;
		public uint GetVirtualSize() => _length;
		public void SetOffset(FileOffset offset, RVA rva) {
			_fileOffset = offset;
			_rva = rva;

			// compute length
			_length = (uint)Encoding.UTF8.GetByteCount(fileMagic)
				+ 4 // formatVersion
				+ encryptionInfo.GetVirtualSize() // encryptionInfo
				+ 12 // cli header
				+ 4
				+ 16 * (uint)sections.Count;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public void WriteTo(DataWriter writer) {
			writer.WriteBytes(Encoding.UTF8.GetBytes(fileMagic));
			writer.WriteInt32(formatVersion);
			encryptionInfo.WriteTo(writer);
			writer.WriteInt32((int)entryPointToken);
			writer.WriteUInt32((uint)metadataRva);
			writer.WriteUInt32(metadataSize);
			writer.WriteUInt32((uint)sections.Count);
			foreach(var section in sections) {
				writer.WriteUInt32((uint)section.FileOffset);
				writer.WriteUInt32(section.GetFileLength());
				writer.WriteUInt32((uint)section.RVA);
				writer.WriteUInt32(section.GetVirtualSize());
			}
		}
	}
}
