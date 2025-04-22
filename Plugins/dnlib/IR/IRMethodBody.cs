using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;


namespace dnlib.IR {

	public class IRMethodBody {

		public MethodDef MethodDef { get; }

		public List<IRExceptionHandler> ExceptionHandlers { get; }

		public List<IRBasicBlock> BasicBlocks { get; private set; }

		public VariableSet VariableSet { get; private set; }

		private CFG _cfg;

		private class LocalVariable {
			public LocalVariable prev;
			public VariableInfo variableInfo;
		}

		private List<LocalVariable> _localVariables;

		public IRMethodBody(MethodDef methodDef, List<IRExceptionHandler> exceptionHandlers, VariableSet variableSet, List<IRBasicBlock> basicBlocks) {
			MethodDef = methodDef;
			ExceptionHandlers = exceptionHandlers;
			VariableSet = variableSet;
			BasicBlocks = basicBlocks;
		}

		public void ApplyOptimizations() {
			RemoveUnusedBasicBlocks();
			RemoveAllUnusedVariales();
			RebuildInstructionIds();
			BuildLocalVariables();

			//_cfg = new CFG(BasicBlocks, ExceptionHandlers);
			//_cfg.ComputeControlFlows();

			//BuildVariableScopes();
		}


		private void RebuildInstructionIds() {
			int nextIndex = 0;
			foreach (var bb in BasicBlocks) {
				foreach (var inst in bb.Instructions) {
					inst.index = nextIndex++;
				}
			}
		}

		private HashSet<VariableInfo> ComputeUsedVariables() {
			var usedVars = new HashSet<VariableInfo>();
			foreach (var bb in BasicBlocks) {
				foreach (var inst in bb.Instructions) {
					foreach (var op in inst.args) {
						if (op is InstructionArgumentVariable iav) {
							usedVars.Add(iav.value);
						}
						else if (op is InstructionArgumentMultiVariable iamv) {
							foreach (var v in iamv.values) {
								usedVars.Add(v);
							}
						}
					}
				}
			}
			return usedVars;
		}


		private void RemoveAllUnusedVariales() {
			var usedVars = ComputeUsedVariables();
			VariableSet = VariableSet.Rebuild(usedVars);
		}

		private void WalkBasicBlock(IRBasicBlock bb, HashSet<IRBasicBlock> visited) {
			if (!visited.Add(bb)) {
				return;
			}
			foreach (var next in bb.OutboundBasicBlocks) {
				WalkBasicBlock(next, visited);
			}
		}

		private HashSet<IRBasicBlock> ComputeUsedBlocks() {
			var usedBlocks = new HashSet<IRBasicBlock>();
			WalkBasicBlock(BasicBlocks[0], usedBlocks);
			foreach (var ex in ExceptionHandlers) {
				if (ex.TryStart != null) {
					WalkBasicBlock(ex.TryStart, usedBlocks);
				}
				if (ex.HandlerStart != null) {
					WalkBasicBlock(ex.HandlerStart, usedBlocks);
				}
				if (ex.FilterStart != null) {
					WalkBasicBlock(ex.FilterStart, usedBlocks);
				}
			}
			return usedBlocks;
		}

		private void RemoveUnusedBasicBlocks() {
			var usedBlocks = ComputeUsedBlocks();
			var oldBasicBlocks = BasicBlocks;
			var newBasicBlocks = BasicBlocks.Where(usedBlocks.Contains).ToList();
			BasicBlocks = newBasicBlocks;
			int oldBasicBlocksCount = oldBasicBlocks.Count;
			int newBasicBlocksCount = BasicBlocks.Count;
			if (newBasicBlocksCount != oldBasicBlocksCount) {
				Console.WriteLine($"original BasicBlock count:{oldBasicBlocksCount}, trimed count:{newBasicBlocksCount}, Removed {oldBasicBlocksCount - newBasicBlocksCount} unused basic blocks");
			}
		}


		private void BuildLocalVariables() {
			_localVariables = VariableSet.Variables.Select(v => new LocalVariable { variableInfo = v }).ToList();

			LocalVariable prevLocal = null;

			foreach (var loc in _localVariables) {
				Debug.Assert(_localVariables[loc.variableInfo.id] == loc);
				if (loc.variableInfo.location == VariableLocation.Argument) {
					continue;
				}
				if (prevLocal != null) {
					loc.prev = prevLocal;
				}
				prevLocal = loc;
			}
		}

		private class VariableScope {

			//private readonly Dictionary<IRBasicBlock, IRInstruction> _lastBbUsedInsts = new Dictionary<IRBasicBlock, IRInstruction>();

			private readonly HashSet<StronglyConnectedComponents> _belongBbs = new HashSet<StronglyConnectedComponents>();

			public StronglyConnectedComponents PrevCommonAncestor { get; private set; }

			public StronglyConnectedComponents NextCommonAncestor { get; private set; }

			public void AddBlock(StronglyConnectedComponents bb) {
				//_lastBbUsedInsts[bb] = inst;
				_belongBbs.Add(bb);
			}

			public void SetupCommonAncestors(CFG cfg) {
				
				if (_belongBbs.Count == 1) {
					PrevCommonAncestor = NextCommonAncestor = _belongBbs.First();
					return;
				}


			}
		}

		private Dictionary<VariableInfo, VariableScope> CreateVariableScopes() {
			var varScopes = new Dictionary<VariableInfo, VariableScope>();
			foreach (var bb in this.BasicBlocks) {
				var scc = _cfg.GetScc(bb);
				foreach (var inst in bb.Instructions) {
					foreach (var op in inst.args) {
						if (op is InstructionArgumentVariable iav) {
							if (!varScopes.TryGetValue(iav.value, out var scope)) {
								scope = new VariableScope();
								varScopes[iav.value] = scope;
							}
							scope.AddBlock(scc);
						}
						else if (op is InstructionArgumentMultiVariable iamv) {
							foreach (var v in iamv.values) {
								if (!varScopes.TryGetValue(v, out var scope)) {
									scope = new VariableScope();
									varScopes[v] = scope;
								}
								scope.AddBlock(scc);
							}
						}
					}
				}
			}
			return varScopes;
		}


		private void BuildVariableScopes() {
			var varScopes = CreateVariableScopes();
			foreach (var scope in varScopes.Values) {
				scope.SetupCommonAncestors(_cfg);
			}
		}
	}
}
