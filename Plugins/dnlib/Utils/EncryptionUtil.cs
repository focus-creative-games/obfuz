using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dnlib.DotNet.Writer;
using dnlib.Protection;

namespace dnlib.Utils {
	internal static class EncryptionUtil {

		public static void WriteWithEncIfNeed(this DataWriter writer, Action<DataWriter> writeAction, Func<IEncryption, EncryptionMethod> encGetter, uint segmentSize) {
			IEncryption enc = EncryptionContext.Encryption;
			EncryptionMethod method = enc != null ? encGetter(enc) : null;
			if (method == null) {
				writeAction(writer);
			} else {
				var ms = new MemoryStream();
				var dw = new DataWriter(ms);
				writeAction(dw);
				ms.Flush();
				var content = ms.ToArray();
				method.EncryptBySegment(content, 0, (uint)content.Length, enc.EncParam, segmentSize);
				writer.WriteBytes(content);
			}
		}

		public static void WriteWithNotSegmentEncIfNeed(this DataWriter writer, Action<DataWriter> writeAction, Func<IEncryption, EncryptionMethod> encGetter) {
			IEncryption enc = EncryptionContext.Encryption;
			EncryptionMethod method = enc != null ? encGetter(enc) : null;
			if (method == null) {
				writeAction(writer);
			}
			else {
				var ms = new MemoryStream();
				var dw = new DataWriter(ms);
				writeAction(dw);
				ms.Flush();
				var content = ms.ToArray();
				method.Encrypt(content, 0, (uint)content.Length, enc.EncParam);
				writer.WriteBytes(content);
			}
		}
	}
}
