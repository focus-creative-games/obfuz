using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace dnlib.Protection {

	public class RandomEncryptionOptions {

		public int InstructionSeed { get; set; }

		public int MetadataSeed { get; set; }

		public byte[] EncKey { get; set; }

		public int StringEncCodeLength { get; set; } = 128;

		public int BlobEncCodeLength { get; set; } = 128;

		public int UserStringEncCodeLength { get; set; } = 128;

		public int LazyUserStringEncCodeLength { get; set; } = 16;

		public int TableEncCodeLength { get; set; } = 128;

		public int LazyTableEncCodeLength { get; set; } = 16;

		public int MethodBodyEncCodeLength { get; set; } = 16;
	}


	public class RandomEncryption : IEncryption {

		private readonly byte[] _encParam;

		private readonly EncryptionInstructionSet _instructionSet;

		private readonly EncryptionMethod _signatureEnc;

		private readonly EncryptionMethod _fileHeaderEnc;
		private readonly EncryptionMethod _stringEnc;
		private readonly EncryptionMethod _blobEnc;
		private readonly EncryptionMethod _userStringEnc;
		private readonly EncryptionMethod _lazyUserStringEnc;
		private readonly EncryptionMethod _tableEnc;
		private readonly EncryptionMethod _lazyTableEnc;
		private readonly EncryptionMethod _methodBodyEnc;

		public byte[] EncParam => _encParam;

		public Algorithm Algorithm => Algorithm.Custom;

		public int AlgoVersion => 0;

		public EncryptionInstructionSet InstructionSet => _instructionSet;

		public EncryptionMethod SignatureEnc => _signatureEnc;

		public EncryptionMethod FileHeaderEnc => _fileHeaderEnc;

		public EncryptionMethod StringEnc => _stringEnc;

		public EncryptionMethod BlobEnc => _blobEnc;

		public EncryptionMethod UserStringEnc => _userStringEnc;

		public EncryptionMethod LazyUserStringEnc => _lazyUserStringEnc;

		public EncryptionMethod TableEnc => _tableEnc;

		public EncryptionMethod LazyTableEnc => _lazyTableEnc;

		public EncryptionMethod MethodBodyEnc => _methodBodyEnc;

		public RandomEncryption(RandomEncryptionOptions opt) {
			_encParam = opt.EncKey;

			_instructionSet = new EncryptionInstructionSet(opt.InstructionSeed);

			_signatureEnc = new EncryptionMethod(_instructionSet, CreateSignatureEncOpCodes());

			int metadataSeed = opt.MetadataSeed;
			_fileHeaderEnc = new EncryptionMethod(_instructionSet, metadataSeed++, 0x100);
			_stringEnc = new EncryptionMethod(_instructionSet, metadataSeed++, opt.StringEncCodeLength);
			_blobEnc = new EncryptionMethod(_instructionSet, metadataSeed++, opt.BlobEncCodeLength);
			_userStringEnc = new EncryptionMethod(_instructionSet, metadataSeed++, opt.UserStringEncCodeLength);
			_lazyUserStringEnc = new EncryptionMethod(_instructionSet, metadataSeed++, opt.LazyUserStringEncCodeLength);
			_tableEnc = new EncryptionMethod(_instructionSet, metadataSeed++, opt.TableEncCodeLength);
			_lazyTableEnc = new EncryptionMethod(_instructionSet, metadataSeed++, opt.LazyTableEncCodeLength);
			_methodBodyEnc = new EncryptionMethod(_instructionSet, metadataSeed++, opt.MethodBodyEncCodeLength);
		}

		private byte[] CreateSignatureEncOpCodes() {
			int size = 256;
			var ops = new byte[size];
			for (int i = 0; i < ops.Length; i++) {
				ops[i] = (byte)i;
			}
			return ops;
		}
	}
}
