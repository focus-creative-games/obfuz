using System.Collections.Generic;


namespace dnlib.IR {

	public enum PrefixCode {
		None,
		Constrained,
		No,
		ReadOnly,
		Tail,
		Unaligned,
		Volatile,
	}

	public struct PrefixData {
		public PrefixCode code;
		public object data;
	}


	public class IRInstruction {
		public IRFamily family;
		public IROpCode opcode;
		public List<InstructionArgument> args;
		public PrefixData? prefixData;
		public object inlineOperand;
		public int index;

		public static IRInstruction Create(IRFamily family, IROpCode opcode, params InstructionArgument[] args) {
			return new IRInstruction {
				family = family,
				opcode = opcode,
				args = new List<InstructionArgument>(args),
			};
		}

		public static IRInstruction Create(IRFamily family, IROpCode opcode, IRBasicBlock target, params InstructionArgument[] args) {
			return new IRInstruction {
				family = family,
				opcode = opcode,
				args = new List<InstructionArgument>(args),
				inlineOperand = target,
			};
		}

		public static IRInstruction Create(IRFamily family, IROpCode opcode, IRBasicBlock[] targets, params InstructionArgument[] args) {
			return new IRInstruction {
				family = family,
				opcode = opcode,
				args = new List<InstructionArgument>(args),
				inlineOperand = targets,
			};
		}

		public static IRInstruction Create(IRFamily family, IROpCode opcode, PrefixData? prefixData, params InstructionArgument[] args) {
			return new IRInstruction {
				family = family,
				opcode = opcode,
				prefixData = prefixData,
				args = new List<InstructionArgument>(args),
			};
		}
	}
}
