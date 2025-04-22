using System;
using System.Collections.Generic;
using System.Text;

namespace dnlib.Protection {
	internal class EncryptionContext {

		public const int SmallSegmentSize = 0x10;

		public const int BigSegmentSize = 0x100;

		public static IEncryption Encryption { get; set; }
	}
}
