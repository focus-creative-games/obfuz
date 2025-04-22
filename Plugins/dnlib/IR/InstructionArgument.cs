namespace dnlib.IR {
	public class InstructionArgument {
		public ArgumentFamily family;
		public ArgumentFlag flag;
		//public object value;


		public static InstructionArgument CreateVariable(VariableInfo varInfo, ArgumentFlag flag = ArgumentFlag.None) {
			return new InstructionArgumentVariable {
				family = ArgumentFamily.VariableId,
				flag = flag,
				value = varInfo,
			};
		}

		public static InstructionArgument CreateMultiVariable(params VariableInfo[] varInfos) {
			return new InstructionArgumentMultiVariable {
				family = ArgumentFamily.MultiVariable,
				flag = ArgumentFlag.None,
				values = varInfos,
			};
		}

		public static InstructionArgumentConstant CreateConst(TypedConst value) {
			return new InstructionArgumentConstant {
				family = ArgumentFamily.Constant,
				flag = ArgumentFlag.None,
				value = value,
			};
		}

		//public static InstructionArgumentTarget CreateBranchTarget(IRBasicBlock target) {
		//	return new InstructionArgumentTarget {
		//		family = ArgumentFamily.BranchOffset,
		//		flag = ArgumentFlag.None,
		//		target = target,
		//	};
		//}

		//public static InstructionArgumentSwitch CreateSwitch(IRBasicBlock[] cases) {
		//	return new InstructionArgumentSwitch {
		//		family = ArgumentFamily.Switch,
		//		flag = ArgumentFlag.None,
		//		cases = cases,
		//	};
		//}
	}

	public class InstructionArgumentVariable : InstructionArgument {
		public VariableInfo value;
	}
	public class InstructionArgumentMultiVariable : InstructionArgument {
		public VariableInfo[] values;
	}

	public class InstructionArgumentConstant : InstructionArgument {
		public TypedConst value;
	}

	//public class InstructionArgumentTarget : InstructionArgument {
	//	public IRBasicBlock target;
	//}

	//public class InstructionArgumentSwitch : InstructionArgument {
	//	public IRBasicBlock[] cases;
	//}
}
