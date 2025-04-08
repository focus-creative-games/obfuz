using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;


namespace dnlib.IR {
	class MethodBasicBlocks {
		public List<BasicBlock> BasicBlocks { get; }

		public Dictionary<Instruction, BasicBlock> Inst2BasicBlock { get; }

		public MethodBasicBlocks(List<BasicBlock> basicBlocks) {
			this.BasicBlocks = basicBlocks;
			this.Inst2BasicBlock = new Dictionary<Instruction, BasicBlock>();
			foreach (var bb in basicBlocks) {
				var inst = bb.Instructions[0];
				Inst2BasicBlock[inst] = bb;
			}
		}

		public BasicBlock GetBasicBlockByInst(Instruction inst) {
			if (Inst2BasicBlock.TryGetValue(inst, out var bb)) {
				return bb;
			}

			return null;
		}

		public static MethodBasicBlocks SplitBasicBlocks(MethodDef methodDef) {
			var targetInsts = new HashSet<Instruction>();
			bool isEndOfBasicBlock = false;
			foreach (var instruction in methodDef.Body.Instructions) {
				if (isEndOfBasicBlock) {
					targetInsts.Add(instruction);
					isEndOfBasicBlock = false;
				}

				switch (instruction.OpCode.FlowControl) {
				case FlowControl.Branch:
				case FlowControl.Cond_Branch: {
					if (instruction.Operand is Instruction target) {
						targetInsts.Add(target);
						isEndOfBasicBlock = true;
					}
					else if (instruction.Operand is IList<Instruction> targets) {
						foreach (var targetInst in targets) {
							targetInsts.Add(targetInst);
						}
						isEndOfBasicBlock = true;
					}

					break;
				}
				case FlowControl.Return:
				case FlowControl.Throw: {
					isEndOfBasicBlock = true;
					break;
				}
				// case FlowControl.Call:
				// {
				//     targetInsts.Add(instruction);
				//     break;
				// }
				}
			}

			foreach (var exceptionHandler in methodDef.Body.ExceptionHandlers) {
				targetInsts.Add(exceptionHandler.HandlerStart);
				targetInsts.Add(exceptionHandler.HandlerEnd);
				if (exceptionHandler.FilterStart != null) {
					targetInsts.Add(exceptionHandler.FilterStart);
				}

				targetInsts.Add(exceptionHandler.TryStart);
				targetInsts.Add(exceptionHandler.TryEnd);
			}

			BasicBlock currentBasicBlock = null;
			var bbs = new List<BasicBlock>();
			foreach (var instruction in methodDef.Body.Instructions) {
				if (currentBasicBlock == null || targetInsts.Contains(instruction)) {
					currentBasicBlock = new BasicBlock();
					bbs.Add(currentBasicBlock);
				}
				currentBasicBlock.AddInstruction(instruction);
			}


			return new MethodBasicBlocks(bbs);
		}
	}
}
