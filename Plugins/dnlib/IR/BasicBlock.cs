using System;
using System.Collections.Generic;
using System.Text;
using dnlib.DotNet.Emit;

namespace dnlib.IR {
	public class BasicBlock {
		private List<Instruction> _insts = new List<Instruction>();

		private List<VariableInfo> _goinVars = new List<VariableInfo>();
		private List<VariableInfo> _gooutVars = new List<VariableInfo>();


		public IList<Instruction> Instructions => _insts;

		public void AddInstruction(Instruction inst) {
			_insts.Add(inst);
		}
	}
}
