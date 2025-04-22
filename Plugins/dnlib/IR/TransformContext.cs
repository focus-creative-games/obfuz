using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.Utils;
using ExceptionHandler = dnlib.DotNet.Emit.ExceptionHandler;


namespace dnlib.IR {

	public partial class TransformContext {

		private readonly MethodDef _methodDef;
		private readonly VariableSet _vs;
		private readonly EvalStack _es;
		private readonly ParameterList _parameters;
		private readonly LocalList _locals;
		private readonly ICorLibTypes _corLibTypes;

		public TransformContext(MethodDef methodDef) {
			_methodDef = methodDef;
			_vs = new VariableSet(_methodDef.Module);
			_es = new EvalStack();
			_parameters = methodDef.Parameters;
			_locals = methodDef.Body.Variables;
			_corLibTypes = methodDef.Module.CorLibTypes;

			_vs.InitParams(_parameters);
			_vs.InitLocals(_locals);
		}

		public VariableInfo GetParam(Instruction inst) {
			return _vs.GetParam(inst.GetParameterIndex());
		}

		public VariableInfo GetLocal(Instruction inst) {
			return _vs.GetLocal(inst.GetLocal(_locals).Index);
		}

		private void AddInstruction(IRInstruction inst) {
			//_methodDef.Body.Instructions.Add(inst);
			_curIrbb.AddInstruction(inst);
		}

		private void AddLoad(VariableInfo src) {
			var dst = _vs.CreateTempVar(src.type);
			_es.Push(dst);
			var ir = IRInstruction.Create(IRFamily.LoadOrSet, IROpCode.LoadOrSet, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
			AddInstruction(ir);
		}

		private void AddLoadAddress(VariableInfo src) {
			var dst = _vs.CreateTempVar(src.type);
			_es.Push(dst);
			var ir = IRInstruction.Create(IRFamily.LoadAddress, IROpCode.LoadAddress, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
			AddInstruction(ir);
		}

		private void AddSet(VariableInfo dst, VariableInfo src) {
			var ir = IRInstruction.Create(IRFamily.LoadOrSet, IROpCode.LoadOrSet, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
			AddInstruction(ir);
		}

		private void AddSetFromTop(VariableInfo dst) {
			AddSet(dst, _es.Pop());
		}

		private void AddLoadConst(TypedConst src) {
			var dst = _vs.CreateConstVar(src);
			_es.Push(dst);
			var ir = IRInstruction.Create(IRFamily.LoadConstant, IROpCode.LoadConstant, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateConst(src));
			AddInstruction(ir);
		}

		//private void AddBranch(IROpCode opCode, int target, params VariableInfo[] parameters) {
		//	_es.Pop(parameters.Length);
		//	var ir = IRInstruction.Create(IRFamily.Branch, opCode, InstructionArgument.CreateBranchTarget(target), InstructionArgument.CreateMultiVariable(parameters));
		//	AddInstruction(ir);
		//}

		private void AddBranch(Instruction il, int paramCount) {
			IRBasicBlock target = GetIRBasicBlock((Instruction)il.Operand);
			_curIrbb.AddOutboundBasicBlock(target);
			VariableInfo[] parameters = _es.PopWithValue(paramCount);
			var ir = IRInstruction.Create(IRFamily.Branch, GetBranchIRCodeByILCode(il.OpCode.Code), target, InstructionArgument.CreateMultiVariable(parameters));
			AddInstruction(ir);
			_es.SaveInbound(target);
		}

		private void AddSwitch(Instruction il) {
			var cases = (Instruction[])il.Operand;
			var index = _es.Pop();
			var targets = cases.Select(c => GetIRBasicBlock(c)).ToArray();
			foreach (var target in targets) {
				_curIrbb.AddOutboundBasicBlock(target);
				_es.SaveInbound(target);
			}
			var ir = IRInstruction.Create(IRFamily.Branch, IROpCode.Switch, targets, InstructionArgument.CreateVariable(index));
			AddInstruction(ir);
		}

		private IRBasicBlock GetIRBasicBlock(Instruction inst) {
			BasicBlock bb = _bbs.GetBasicBlockByInst(inst);
			return _il2irBb[bb];
		}


		struct MethodReturnTypeAndParams {
			public TypeSig returnType;
			public List<TypeSig> parameterTypes;
		}

		struct GenericInst {
			public IList<TypeSig> typeGenArgs;
			public IList<TypeSig> methodGenArgs;
		}

		private GenericInst ResolveGenericContenxt(IMethod method) {
			IList<TypeSig> typeGenArgs = null;
			IList<TypeSig> methodGenArgs = null;
			if (method is MemberRef memberRef) {
				var parent = memberRef.Class;
				if (parent is TypeSpec) {
					if (((TypeSpec)parent).TypeSig is GenericInstSig sig)
						typeGenArgs = sig.GenericArguments;
				}
			}
			else if (method is MethodSpec methodSpec) {
				if (methodSpec.Method is MemberRef mr) {
					var parent = mr.Class;
					if (parent is TypeSpec) {
						if (((TypeSpec)parent).TypeSig is GenericInstSig sig)
							typeGenArgs = sig.GenericArguments;
					}
				}
				methodGenArgs = methodSpec.GenericInstMethodSig?.GenericArguments;
			}
			return new GenericInst {
				typeGenArgs = typeGenArgs,
				methodGenArgs = methodGenArgs,
			};
		}

		private GenericInst ResolveGenericContenxt(IField field) {
			IList<TypeSig> typeGenArgs = null;
			if (field is MemberRef memberRef) {
				var parent = memberRef.Class;
				if (parent is TypeSpec) {
					if (((TypeSpec)parent).TypeSig is GenericInstSig sig)
						typeGenArgs = sig.GenericArguments;
				}
			}
			return new GenericInst {
				typeGenArgs = typeGenArgs,
				methodGenArgs = null,
			};
		}

		MethodReturnTypeAndParams MakeMethodFinalSignature(MethodSig methodSig, IList<TypeSig> typeGenArgs, IList<TypeSig> methodGenArgs) {
			var ga = new GenericArguments();
			if (typeGenArgs is not null)
				ga.PushTypeArgs(typeGenArgs);
			if (methodGenArgs is not null)
				ga.PushMethodArgs(methodGenArgs);
			var returnType = ga.Resolve(methodSig.RetType);
			var parameterTypes = methodSig.Params.Select(p => ga.Resolve(p)).ToList();
			if (methodSig.HasThis)
				parameterTypes.Insert(0, _corLibTypes.UInt32);
			return new MethodReturnTypeAndParams {
				returnType = returnType,
				parameterTypes = parameterTypes,
			};
		}

		TypeSig MakeFieldFinalType(TypeSig type, IList<TypeSig> typeGenArgs) {
			var ga = new GenericArguments();
			if (typeGenArgs is not null)
				ga.PushTypeArgs(typeGenArgs);
			var returnType = ga.Resolve(type);
			return returnType;
		}

		TypeSig GetFieldType(IField field) {
			var typeGenArgs = ResolveGenericContenxt(field).typeGenArgs;
			return MakeFieldFinalType(field.FieldSig.Type, typeGenArgs);
		}


		private void AddCall(IROpCode opCode, IMethod method, PrefixData? prefixData) {

			MethodSig methodSig = method.MethodSig;

			var mgc = ResolveGenericContenxt(method);
			var mrtp = MakeMethodFinalSignature(methodSig, mgc.typeGenArgs, mgc.methodGenArgs);

			


			AddCall(opCode, method, mrtp.returnType, mrtp.parameterTypes.ToArray(), prefixData);
			//int paramCount = method.MethodSig.Params.Count + (method.MethodSig.HasThis ? 1 : 0);
			//VariableInfo[] parameters = _es.PopWithValue(paramCount);
			//VariableInfo ret;
			//if (method.MethodSig.RetType.ElementType == ElementType.Void) {
			//	ret = null;
			//} else {
			//	ret = _vs.CreateTempVar(method.MethodSig.RetType);
			//	_es.Push(ret);
			//}
			//var ir = Instruction.Create(IRFamily.Call, IROpCode.Call, ret != null ? InstructionArgument.CreateVariable(ret) : null, InstructionArgument.CreateMultiVariable(parameters));
			//AddInstruction(ir);
		}

		private void AddCall(IROpCode opCode, object token, TypeSig returnType, TypeSig[] parameterTypes, PrefixData? prefixData) {
			int paramCount = parameterTypes?.Length ?? 0;
			VariableInfo[] parameters = _es.PopWithValue(paramCount);
			VariableInfo ret;
			if (returnType.ElementType == ElementType.Void) {
				ret = null;
			}
			else {
				ret = _vs.CreateTempVar(returnType);
				_es.Push(ret);
			}
			var ir = IRInstruction.Create(IRFamily.Call, opCode, prefixData, ret != null ? InstructionArgument.CreateVariable(ret) : null, InstructionArgument.CreateMultiVariable(parameters));
			ir.inlineOperand = token;
			AddInstruction(ir);
		}

		private void AddNewObj(IMethod methodInfo) {
			MethodSig method = methodInfo.MethodSig;
			int paramCount = method.Params.Count;
			VariableInfo[] parameters = _es.PopWithValue(paramCount);
			VariableInfo ret = _vs.CreateTempVar(method.RetType);
			_es.Push(ret);

			var ir = IRInstruction.Create(IRFamily.Call, IROpCode.NewObj, ret != null ? InstructionArgument.CreateVariable(ret) : null, InstructionArgument.CreateMultiVariable(parameters));
			ir.inlineOperand = methodInfo;
			AddInstruction(ir);
		}

		private TypeSig CalcEqualType(TypeSig type) {
			if (type.IsByRef) {
				return _corLibTypes.IntPtr;
			}
			switch (type.ElementType) {
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4: return _corLibTypes.Int32;
			case ElementType.I8:
			case ElementType.U8: return _corLibTypes.Int64;
			case ElementType.R4: return _corLibTypes.Single;
			case ElementType.R8: return _corLibTypes.Double;
			case ElementType.I:
			case ElementType.U:
			case ElementType.Ptr:
			case ElementType.FnPtr:
			case ElementType.Object:
			return _corLibTypes.IntPtr;
			default: return type;
			}
		}

		private TypeSig CalcConvertResultType(Code code) {
			switch (code) {
			case Code.Conv_I1:
			case Code.Conv_I2:
			case Code.Conv_I4:
			case Code.Conv_U1:
			case Code.Conv_U2:
			case Code.Conv_U4:
			case Code.Conv_Ovf_I1:
			case Code.Conv_Ovf_I2:
			case Code.Conv_Ovf_I4:
			case Code.Conv_Ovf_U1:
			case Code.Conv_Ovf_U2:
			case Code.Conv_Ovf_U4:
			case Code.Conv_Ovf_I1_Un:
			case Code.Conv_Ovf_I2_Un:
			case Code.Conv_Ovf_I4_Un:
			case Code.Conv_Ovf_U1_Un:
			case Code.Conv_Ovf_U2_Un:
			case Code.Conv_Ovf_U4_Un: return _corLibTypes.Int32;
			case Code.Conv_I8:
			case Code.Conv_U8:
			case Code.Conv_Ovf_I8:
			case Code.Conv_Ovf_U8:
			case Code.Conv_Ovf_I8_Un:
			case Code.Conv_Ovf_U8_Un: return _corLibTypes.Int64;
			case Code.Conv_I:
			case Code.Conv_U:
			case Code.Conv_Ovf_I:
			case Code.Conv_Ovf_U:
			case Code.Conv_Ovf_I_Un:
			case Code.Conv_Ovf_U_Un: return _corLibTypes.IntPtr;
			case Code.Conv_R4: return _corLibTypes.Single;
			case Code.Conv_R8: return _corLibTypes.Double;
			default: throw new NotSupportedException();
			}
		}

		private TypeSig CalcArithOpResultType(TypeSig op1, TypeSig op2) {

			// int32 + int32 = int32
			// int32 + native int => native int
			// x + int64 => int64
			// enum + x => enum
			var type1 = CalcEqualType(op1).ElementType;
			var type2 = CalcEqualType(op2).ElementType;

			if (type1 == ElementType.ValueType || type1 == ElementType.GenericInst) {
				switch (type2) {
				case ElementType.I4:
				case ElementType.I8:
				case ElementType.I: return op1;
				default: throw new NotSupportedException();
				}
			}
			else if (type2 == ElementType.ValueType || type2 == ElementType.GenericInst) {
				switch (type1) {
				case ElementType.I4:
				case ElementType.I8:
				case ElementType.I: return op2;
				default: throw new NotSupportedException();
				}
			}

			switch (type1) {
			case ElementType.I4: {
				switch (type2) {
				case ElementType.I4: return _corLibTypes.Int32;
				case ElementType.I8: return _corLibTypes.Int64;
				case ElementType.I: return _corLibTypes.IntPtr;
				default: throw new NotSupportedException();
				}
			}
			case ElementType.I8: {
				switch (type2) {
				case ElementType.I4: 
				case ElementType.I8:
				case ElementType.I: return _corLibTypes.Int64;
				default: throw new NotSupportedException();
				}
			}
			case ElementType.I: {
				switch (type2) {
				case ElementType.I4:
				case ElementType.I: return _corLibTypes.IntPtr;
				case ElementType.I8: return _corLibTypes.Int64;
				default: throw new NotSupportedException();
				}
			}
			case ElementType.R4: {
				switch (type2) {
				case ElementType.R4: return _corLibTypes.Single;
				case ElementType.R8: return _corLibTypes.Double;
				default: throw new NotSupportedException();
				}
			}
			case ElementType.R8: {
				switch (type2) {
				case ElementType.R4:
				case ElementType.R8: return _corLibTypes.Double;
				default: throw new NotSupportedException();
				}
			}
			default: throw new NotSupportedException();
			}
		}

		private IROpCode GetArithIROpcodeByILCode(Code code) {
			switch (code) {
			case Code.Add: return IROpCode.Add;
			case Code.Add_Ovf: return IROpCode.Add_Ovf;
			case Code.Add_Ovf_Un: return IROpCode.Add_Ovf_Un;
			case Code.Sub: return IROpCode.Sub;
			case Code.Sub_Ovf: return IROpCode.Sub_Ovf;
			case Code.Sub_Ovf_Un: return IROpCode.Sub_Ovf_Un;
			case Code.Mul: return IROpCode.Mul;
			case Code.Mul_Ovf: return IROpCode.Mul_Ovf;
			case Code.Mul_Ovf_Un: return IROpCode.Mul_Ovf_Un;
			case Code.Div: return IROpCode.Div;
			case Code.Div_Un: return IROpCode.Div_Un;
			case Code.Rem: return IROpCode.Rem;
			case Code.Rem_Un: return IROpCode.Rem_Un;
			case Code.And: return IROpCode.And;
			case Code.Or: return IROpCode.Or;
			case Code.Xor: return IROpCode.Xor;
			case Code.Shl: return IROpCode.Shl;
			case Code.Shr: return IROpCode.Shr;
			case Code.Shr_Un: return IROpCode.Shr_Un;
			default: throw new NotSupportedException();
			}
		}

		private IROpCode GetConvertIROpcodeByILCode(Code code) {
			switch (code) {
			case Code.Conv_I1: return IROpCode.Conv_I1;
			case Code.Conv_I2: return IROpCode.Conv_I2;
			case Code.Conv_I4: return IROpCode.Conv_I4;
			case Code.Conv_I8: return IROpCode.Conv_I8;
			case Code.Conv_U1: return IROpCode.Conv_U1;
			case Code.Conv_U2: return IROpCode.Conv_U2;
			case Code.Conv_U4: return IROpCode.Conv_U4;
			case Code.Conv_U8: return IROpCode.Conv_U8;
			case Code.Conv_I: return IROpCode.Conv_I;
			case Code.Conv_U: return IROpCode.Conv_U;
			case Code.Conv_R4: return IROpCode.Conv_R4;
			case Code.Conv_R8: return IROpCode.Conv_R8;
			case Code.Conv_Ovf_I1: return IROpCode.Conv_Ovf_I1;
			case Code.Conv_Ovf_I2: return IROpCode.Conv_Ovf_I2;
			case Code.Conv_Ovf_I4: return IROpCode.Conv_Ovf_I4;
			case Code.Conv_Ovf_I8: return IROpCode.Conv_Ovf_I8;
			case Code.Conv_Ovf_U1: return IROpCode.Conv_Ovf_U1;
			case Code.Conv_Ovf_U2: return IROpCode.Conv_Ovf_U2;
			case Code.Conv_Ovf_U4: return IROpCode.Conv_Ovf_U4;
			case Code.Conv_Ovf_U8: return IROpCode.Conv_Ovf_U8;
			case Code.Conv_Ovf_I: return IROpCode.Conv_Ovf_I;
			case Code.Conv_Ovf_U: return IROpCode.Conv_Ovf_U;
			case Code.Conv_Ovf_I1_Un: return IROpCode.Conv_Ovf_I1_Un;
			case Code.Conv_Ovf_I2_Un: return IROpCode.Conv_Ovf_I2_Un;
			case Code.Conv_Ovf_I4_Un: return IROpCode.Conv_Ovf_I4_Un;
			case Code.Conv_Ovf_I8_Un: return IROpCode.Conv_Ovf_I8_Un;
			case Code.Conv_Ovf_U1_Un: return IROpCode.Conv_Ovf_U1_Un;
			case Code.Conv_Ovf_U2_Un: return IROpCode.Conv_Ovf_U2_Un;
			case Code.Conv_Ovf_U4_Un: return IROpCode.Conv_Ovf_U4_Un;
			case Code.Conv_Ovf_U8_Un: return IROpCode.Conv_Ovf_U8_Un;
			case Code.Conv_Ovf_I_Un: return IROpCode.Conv_Ovf_I_Un;
			case Code.Conv_Ovf_U_Un: return IROpCode.Conv_Ovf_U_Un;
			default: throw new NotSupportedException();
			}
		}

		private IROpCode GetArrayIROpcodeByILCode(Code code) {
			switch (code) {
				case Code.Newarr: return IROpCode.Newarr;
				case Code.Ldlen: return IROpCode.LdLen;
				case Code.Ldelema: return IROpCode.Ldelema;
				case Code.Ldelem_I1: return IROpCode.Ldelem_I1;
				case Code.Ldelem_U1: return IROpCode.Ldelem_U1;
				case Code.Ldelem_I2: return IROpCode.Ldelem_I2;
				case Code.Ldelem_U2: return IROpCode.Ldelem_U2;
				case Code.Ldelem_I4: return IROpCode.Ldelem_I4;
				case Code.Ldelem_U4: return IROpCode.Ldelem_U4;
				case Code.Ldelem_I8: return IROpCode.Ldelem_I8;
				case Code.Ldelem_I: return IROpCode.Ldelem_I;
				case Code.Ldelem_R4: return IROpCode.Ldelem_R4;
				case Code.Ldelem_R8: return IROpCode.Ldelem_R8;
				case Code.Ldelem_Ref: return IROpCode.Ldelem_Ref;
				case Code.Stelem_I: return IROpCode.Stelem_I;
				case Code.Stelem_I1: return IROpCode.Stelem_I1;
				case Code.Stelem_I2: return IROpCode.Stelem_I2;
				case Code.Stelem_I4: return IROpCode.Stelem_I4;
				case Code.Stelem_I8: return IROpCode.Stelem_I8;
				case Code.Stelem_R4: return IROpCode.Stelem_R4;
				case Code.Stelem_R8: return IROpCode.Stelem_R8;
				case Code.Stelem_Ref: return IROpCode.Stelem_Ref;
				case Code.Ldelem: return IROpCode.Ldelem;
				case Code.Stelem: return IROpCode.Stelem;
				default: throw new NotSupportedException();
			}
		}

		private IROpCode GetBranchIRCodeByILCode(Code code) {
			switch (code) {
			case Code.Brfalse:
			case Code.Brfalse_S: return IROpCode.BranchFalse;
			case Code.Brtrue:
			case Code.Brtrue_S: return IROpCode.BranchTrue;
			case Code.Br:
			case Code.Br_S:	return IROpCode.UnconditionBranch;
			case Code.Beq:
			case Code.Beq_S: return IROpCode.Beq;
			case Code.Bge:
			case Code.Bge_S: return IROpCode.Bge;
			case Code.Bgt:
			case Code.Bgt_S: return IROpCode.Bgt;
			case Code.Ble:
			case Code.Ble_S: return IROpCode.Ble;
			case Code.Blt:
			case Code.Blt_S: return IROpCode.Blt;
			case Code.Bne_Un:
			case Code.Bne_Un_S: return IROpCode.Bne_Un;
			case Code.Bge_Un:
			case Code.Bge_Un_S: return IROpCode.Bge_Un;
			case Code.Bgt_Un:
			case Code.Bgt_Un_S: return IROpCode.Bgt_Un;
			case Code.Ble_Un:
			case Code.Ble_Un_S: return IROpCode.Ble_Un;
			case Code.Blt_Un:
			case Code.Blt_Un_S: return IROpCode.Blt_Un;
			default: throw new NotSupportedException($"{code}");
			}
		}

		private IROpCode GetCompareIRCodeByILCode(Code code) {
			switch (code) {
			case Code.Ceq: return IROpCode.Ceq;
				case Code.Cgt: return IROpCode.Cgt;
				case Code.Cgt_Un: return IROpCode.Cgt_Un;
				case Code.Clt: return IROpCode.Clt;
				case Code.Clt_Un: return IROpCode.Clt_Un;
				default: throw new NotSupportedException($"{code}");
			}
		}

		private IROpCode GetUnaryIRCodeByILCode(Code code) {
			switch (code) {
			case Code.Neg: return IROpCode.Neg;
			case Code.Not: return IROpCode.Not;
			default: throw new NotSupportedException($"{code}");
			}
		}

		private IROpCode GetCallIRCodeByILCode(Code code) {
			switch (code) {
			case Code.Call: return IROpCode.Call;
			case Code.Calli: return IROpCode.CallI;
			case Code.Callvirt: return IROpCode.CallVir;
			default: throw new NotSupportedException($"{code}");
			}
		}

		private IROpCode GetUnspecIRCodeByILCode(Code code) {
			switch (code) {
			case Code.Ldftn: return IROpCode.Ldftn;
			case Code.Ldvirtftn: return IROpCode.Ldvirtftn;
			default: throw new NotSupportedException($"{code}");
			}
		}


		private readonly Queue<BasicBlock> _pendingBBs = new Queue<BasicBlock>();


		private IRBasicBlock _curIrbb;

		private MethodBasicBlocks _bbs;

		private Dictionary<BasicBlock, IRBasicBlock> _il2irBb;

		private List<IRBasicBlock> _irbbs;


		private IRBasicBlock GetIRBasicBlockByIL(Instruction inst) {
			return inst != null ? _il2irBb[_bbs.GetBasicBlockByInst(inst)] : null;
		}


		private void SetupExceptionHandlerStart(Instruction handlerStartInst) {
			var bb = _bbs.GetBasicBlockByInst(handlerStartInst);
			var irbb = _il2irBb[bb];
			var exceptionObj = _vs.CreateTempVar(_corLibTypes.Object);
			irbb.SetInboundVariable(exceptionObj);
			irbb.AddInstruction(IRInstruction.Create(IRFamily.Exception, IROpCode.LoadExceptionObject, InstructionArgument.CreateVariable(exceptionObj)));
		}

		public IRMethodBody Transform() {

			_bbs = MethodBasicBlocks.SplitBasicBlocks(_methodDef);

			var visitedBBs = new HashSet<BasicBlock>();

			_il2irBb = new Dictionary<BasicBlock, IRBasicBlock>();
			_irbbs = new List<IRBasicBlock>();
			IRBasicBlock lastIrbb = null;
			foreach (var bb in _bbs.BasicBlocks) {

				IRBasicBlock irbb = new IRBasicBlock { ilbb = bb };
				if (lastIrbb != null) {
					lastIrbb.nextIrbb = irbb;
				}
				_irbbs.Add(irbb);
				_il2irBb[bb] = irbb;

				lastIrbb = irbb;
			}

			// while enter catch or filter block, push current exception object to exception object stack.
			// the first instruction push top exception object to the eval stack.
			foreach (var exceptionHandler in _methodDef.Body.ExceptionHandlers) {

				if (exceptionHandler.HandlerStart != null) {
					SetupExceptionHandlerStart(exceptionHandler.HandlerStart);
				}
				if (exceptionHandler.FilterStart != null) {
					SetupExceptionHandlerStart(exceptionHandler.FilterStart);
				}

			}

			_curIrbb = _irbbs[0];

			BasicBlock curBb;
			for (int nextBasicBlockIndex = 0; ; ) {
				while (true) {
					if (_pendingBBs.Count > 0) {
						curBb = _pendingBBs.Dequeue();
					}
					else {
						if (nextBasicBlockIndex >= _bbs.BasicBlocks.Count) {
							curBb = null;
							break;
						}
						curBb = _bbs.BasicBlocks[nextBasicBlockIndex++];
					}
					if (!visitedBBs.Contains(curBb)) {
						break;
					}
				}
				if (curBb == null) {
					break;
				}
				visitedBBs.Add(curBb);
				TransformBasicBlock(curBb, _il2irBb[curBb]);
			}

			var varMapper = new Dictionary<VariableInfo, VariableInfo>();
			MergeInboundOutboundVariables(_irbbs, varMapper);
			MapVariableToMergedVariable(_irbbs, varMapper);


			var irExs = new List<IRExceptionHandler>();
			foreach (ExceptionHandler exceptionHandler in _methodDef.Body.ExceptionHandlers) {
				var irex = new IRExceptionHandler {
					TryStart = GetIRBasicBlockByIL(exceptionHandler.TryStart),
					TryEnd = GetIRBasicBlockByIL(exceptionHandler.TryEnd),
					FilterStart = GetIRBasicBlockByIL(exceptionHandler.FilterStart),
					HandlerStart = GetIRBasicBlockByIL(exceptionHandler.HandlerStart),
					HandlerEnd = GetIRBasicBlockByIL(exceptionHandler.HandlerEnd),
					CatchType = exceptionHandler.CatchType,
					HandlerType = exceptionHandler.HandlerType,
				};
				irExs.Add(irex);
			}

			var irMethod = new IRMethodBody(_methodDef, irExs, _vs, _irbbs);
			irMethod.ApplyOptimizations();
			return irMethod;
		}


		private void MapVariableToMergedVariable(List<IRBasicBlock> bbs, Dictionary<VariableInfo, VariableInfo> variableMapper) {
			foreach (var bb in bbs) {
				foreach (var inst in bb.Instructions) {
					foreach (InstructionArgument arg in inst.args) {
						if (arg is InstructionArgumentVariable varArg) {
							if (variableMapper.TryGetValue(varArg.value, out var value)) {
								varArg.value = value;
							}
						}
					}
				}
			}
		}


		private class VariableGroup {
			public readonly List<VariableInfo> variables = new List<VariableInfo>();

			public void Add(VariableInfo variable) {
				variables.Add(variable);
			}

			public void Merge(VariableGroup group, Dictionary<VariableInfo, VariableGroup> variableGroups) {
				VariableGroup srcGroup;
				VariableGroup dstGroup;
				if (group.variables.Count < variables.Count) {
					srcGroup = group;
					dstGroup = this;
				} else {
					srcGroup = this;
					dstGroup = group;
				}

				foreach (var variable in srcGroup.variables) {
					dstGroup.variables.Add(variable);
					variableGroups[variable] = dstGroup;
				}
			}
		}

		private void MergeInboundOutboundVariables(List<IRBasicBlock> bbs, Dictionary<VariableInfo, VariableInfo> variableMapper) {
			var visited = new HashSet<IRBasicBlock>();

			var variableGroups = new Dictionary<VariableInfo, VariableGroup>();
			foreach (var bb in bbs) {
				MergeInboundOutboundVariables(bb, visited, variableGroups);
			}
			foreach (var e in variableGroups) {
				variableMapper.Add(e.Key, e.Value.variables[0]);
			}
		}

		private void MergeInboundOutboundVariables(IRBasicBlock bb, HashSet<IRBasicBlock> visited, Dictionary<VariableInfo, VariableGroup> variableGroups) {
			if (!visited.Add(bb)) {
				return;
			}

			var outboundVars = bb.OutboundVariables;
			foreach (var ov in outboundVars) {
				if (!variableGroups.ContainsKey(ov)) {
					var group = new VariableGroup();
					variableGroups.Add(ov, group);
					group.Add(ov);
				}
			}
			int outBoundVarCount = outboundVars.Count;
			foreach (var target in bb.OutboundBasicBlocks) {
				if (target.InboundVariables.Count != outBoundVarCount) {
					throw new Exception($"method:{_methodDef} InboundVariables.Count != outboundVars.Count");
				}
				for (int i = 0; i < outBoundVarCount; i++) {
					var ov = outboundVars[i];
					var iv = target.InboundVariables[i];
					var ovGroup = variableGroups[ov];
					if (!variableGroups.TryGetValue(iv, out var ivGroup)) {
						ovGroup.Add(iv);
					} else if (ivGroup != ovGroup) {
						ovGroup.Merge(ivGroup, variableGroups);
					}
				}
			}
			foreach (var target in bb.OutboundBasicBlocks) {
				MergeInboundOutboundVariables(target, visited, variableGroups);
			}
		}
	}
}
