using System;
using System.Collections.Generic;
using System.Text;

namespace dnlib.Protection {
	//public class EncryptionVMInterpreterGenerator {

	//	private readonly EncryptionInstructionSet _encryptionInstructionSet;

	//	public EncryptionVMInterpreterGenerator(EncryptionInstructionSet encryptionInstructionSet) {
	//		_encryptionInstructionSet = encryptionInstructionSet;
	//	}

	//	public void Generate(string templateFile, string outputFile) {
	//		var sb = new StringBuilder();

	//		int opCode = 0;
	//		foreach (var inst in _encryptionInstructionSet.opers) {
	//			sb.Append($"\t\t\tcase {opCode}:").Append(inst.GenerateDecryptExpression("data", "dataLength", "key")).Append("break;").AppendLine();
	//			++opCode;
	//		}

	//		var template = System.IO.File.ReadAllText(templateFile);
	//		var frr = new FileRegionReplace(template);
	//		frr.Replace("INSTRUCTIONS", sb.ToString());
	//		frr.Commit(outputFile);
	//	}
	//}
}
