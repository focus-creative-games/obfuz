using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.DotNet.Emit;


namespace dnlib.IR {
	public class VariableSet {
		private readonly ModuleDef _module;
		private readonly ICorLibTypes _corLibTypes;

		private readonly List<VariableInfo> _variables = new List<VariableInfo>();
		private int _nextId;
		private readonly List<VariableInfo> _arguments = new List<VariableInfo>();
		private readonly List<VariableInfo> _locals = new List<VariableInfo>();

		public VariableSet(ModuleDef module) {
			_module = module;
			_corLibTypes = module.CorLibTypes;
		}

		int GetNextId() {
			return _nextId++;
		}

		public IList<VariableInfo> Variables => _variables;

		public IList<VariableInfo> Arguments => _arguments;

		public IList<VariableInfo> Locals => _locals;

		public void InitParams(IEnumerable<Parameter> ps) {
			foreach (var p in ps) {
				var v = new VariableInfo {
					location = VariableLocation.Argument,
					id = GetNextId(),
					paramOrLocalIndex = p.Index,
					variableName = p.Name,
					paramOrLocalOrConst = p,
					type = p.Type,
					expandToInt = false,
				};
				Debug.Assert(p.Index == _arguments.Count);
				_arguments.Add(v);
				Debug.Assert(v.id == _variables.Count);
				_variables.Add(v);
			}
		}

		public void InitLocals(IEnumerable<Local> ls) {
			foreach (var l in ls) {
				var v = new VariableInfo {
					location = VariableLocation.Local,
					id = GetNextId(),
					paramOrLocalIndex = l.Index,
					variableName = l.Name,
					paramOrLocalOrConst = l,
					type = l.Type,
					expandToInt = false,
				};
				Debug.Assert(l.Index == _locals.Count);
				_locals.Add(v);
				Debug.Assert(v.id == _variables.Count);
				_variables.Add(v);
			}
		}



		private bool TryConvertToExpandType(TypeSig type, out TypeSig expandType) {
			type = type.RemovePinnedAndModifiers();
			expandType = type;
			switch (type.ElementType) {
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2: expandType = type.Module.CorLibTypes.Int32; return true;
			case ElementType.MVar:
			case ElementType.Var: return false;
			default: return true;
			}

		}

		public VariableInfo CreateTempVar(TypeSig type, bool expandToInt = true) {
			TypeSig expandType = type;
			if (expandToInt) {
				if (TryConvertToExpandType(type, out expandType)) {
					expandToInt = false;
				}
			}

			var v = new VariableInfo {
				location = VariableLocation.Temp,
				id = GetNextId(),
				paramOrLocalIndex = -1,
				variableName = null,
				paramOrLocalOrConst = null,
				type = expandType,
				expandToInt = expandToInt,
			};
			Debug.Assert(v.id == _variables.Count);
			_variables.Add(v);
			return v;
		}

		private TypeSig GetTypeSigByConstType(ConstType type) {
			switch (type) {
			case ConstType.Int32: return _corLibTypes.Int32;
			case ConstType.Int64: return _corLibTypes.Int64;
			case ConstType.Float: return _corLibTypes.Single;
			case ConstType.Double: return _corLibTypes.Double;
			case ConstType.String: return _corLibTypes.String;
			case ConstType.Null: return _corLibTypes.UIntPtr;
			case ConstType.RuntimeHandle: return _corLibTypes.UIntPtr;
			default: throw new NotSupportedException();
			}
		}

		public VariableInfo CreateConstVar(TypedConst value) {
			var v = new VariableInfo {
				location = VariableLocation.Temp,
				id = GetNextId(),
				paramOrLocalIndex = -1,
				variableName = null,
				paramOrLocalOrConst = null,
				type = GetTypeSigByConstType(value.type),
				expandToInt = false,
			};
			Debug.Assert(v.id == _variables.Count);
			_variables.Add(v);
			return v;
		}

		public VariableInfo GetParam(int index) {
			return _arguments[index];
		}

		public VariableInfo GetLocal(int index) {
			return _locals[index];
		}

		public VariableInfo GetVariable(int id) {
			return _variables[id];
		}

		public VariableSet Rebuild(HashSet<VariableInfo> usedVars) {
			var newVarSet = new VariableSet(_module);
			
			var newVars = newVarSet._variables;
			newVars.AddRange(usedVars);
			newVars.Sort((a, b) => a.id.CompareTo(b.id));

			_nextId = newVars.Count;

			// rebuid id
			for (int i = 0; i < newVars.Count; i++) {
				newVars[i].id = i;
			}
			Console.WriteLine($"original varaible count:{_variables.Count} => trimed count:{newVars.Count}, reduce count:{_variables.Count - newVars.Count}");

			return newVarSet;
		}
	}
}
