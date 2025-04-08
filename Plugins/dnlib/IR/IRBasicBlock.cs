using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;


namespace dnlib.IR {
	public class IRBasicBlock {
		public IRBasicBlock nextIrbb;

		public BasicBlock ilbb;

		private readonly List<IRInstruction> _insts = new List<IRInstruction>();

		private readonly List<VariableInfo> _inboundVars = new List<VariableInfo>();

		private readonly List<VariableInfo> _outboundVars = new List<VariableInfo>();

		private readonly List<IRBasicBlock> _inboundBbs = new List<IRBasicBlock>();

		private readonly List<IRBasicBlock> _outboundBbs = new List<IRBasicBlock>();

		public List<IRInstruction> Instructions => _insts;

		public IList<VariableInfo> InboundVariables => _inboundVars;

		public IList<VariableInfo> OutboundVariables => _outboundVars;

		public IList<IRBasicBlock> InboundBasicBlocks => _inboundBbs;

		public IList<IRBasicBlock> OutboundBasicBlocks => _outboundBbs;

		public int IROffset { get; set; } = -1;

		public void AddInstruction(IRInstruction inst) {
			VerifyInstruction(inst);
			_insts.Add(inst);
		}

		private void VerifyInstruction(IRInstruction inst) {
			var meta = InstructionMeta.Get(inst.opcode);

			if (meta.args.Count != inst.args.Count) {
				Debug.Fail($"Instruction {inst.opcode} has {inst.args.Count} arguments, but {meta.args.Count} are expected.");
			}

			for (int i = 0; i < inst.args.Count; i++) {
				var arg = inst.args[i];
				var argMeta = meta.args[i];
				if ((argMeta.flag & ArgumentMetaFlag.Constant) != 0) {
					if (!(arg is InstructionArgumentConstant)) {
						Debug.Fail($"Argument {i} of instruction {inst.opcode} should be constant.");
					}
				}
				else if ((argMeta.flag & ArgumentMetaFlag.Variadic) != 0) {
					if (!(arg is InstructionArgumentMultiVariable)) {
						Debug.Fail($"Argument {i} of instruction {inst.opcode} should be variable.");
					}
				} else {
					if (arg != null && !(arg is InstructionArgumentVariable)) {
						Debug.Fail($"Argument {i} of instruction {inst.opcode} should be variable.");
					}
				}
			}

			if ((meta.flag & (InstructionFlag.InlineToken | InstructionFlag.InlineOffset)) != 0) {
				Debug.Assert(inst.inlineOperand != null, "inline operand should be null");
			}
		}

		//public void PushEvalStack(VariableInfo v) {
		//	_inboundVars.Add(v);
		//}

		//public void PushEvalStack(IList<VariableInfo> vs) {
		//	_inboundVars.AddRange(vs);
		//}

		public void SetInboundVariable(VariableInfo variable) {
			SetInboundVariables(new VariableInfo[] { variable });
		}

		public void SetInboundVariables(IList<VariableInfo> vs) {

			if (_inboundVars.Count == 0) {
				_inboundVars.AddRange(vs);
			}
			else {
				Debug.Assert(_inboundVars.Count == vs.Count);
			}
		}

		public void SetOutboundVariables(IList<VariableInfo> vs) {
			_outboundVars.Clear();
			_outboundVars.AddRange(vs);
		}

		public void AddOutboundBasicBlock(IRBasicBlock target) {
			if (target != this && !_outboundBbs.Contains(target)) {
				_outboundBbs.Add(target);
				target._inboundBbs.Add(this);
			}
		}
	}
}
