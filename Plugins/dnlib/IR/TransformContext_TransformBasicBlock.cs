using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnlib.IR {

	public partial class TransformContext {

		private void TransformBasicBlock(BasicBlock bb, IRBasicBlock irbb) {
			_curIrbb = irbb;
			_es.LoadInbound(irbb);
			PrefixData? prefixData = null;
			foreach (var il in bb.Instructions) {
				//Console.WriteLine($"IL: {il.OpCode} {il.Operand}");
				Code code = il.OpCode.Code;
				switch (code) {
				case Code.Nop: break;
				case Code.Break: break;
				case Code.Ldarg_0:
				case Code.Ldarg_1:
				case Code.Ldarg_2:
				case Code.Ldarg_3:
				case Code.Ldarg_S:
				case Code.Ldarg: {
					var src = GetParam(il);
					AddLoad(src);
					break;
				}
				case Code.Ldarga_S:
				case Code.Ldarga: {
					var src = GetParam(il);
					AddLoadAddress(src);
					break;
				}
				case Code.Starg_S:
				case Code.Starg: {
					var dst = GetParam(il);
					AddSetFromTop(dst);
					break;
				}
				case Code.Ldloc_0:
				case Code.Ldloc_1:
				case Code.Ldloc_2:
				case Code.Ldloc_3:
				case Code.Ldloc_S:
				case Code.Ldloc: {
					var src = GetLocal(il);
					AddLoad(src);
					break;
				}
				case Code.Ldloca:
				case Code.Ldloca_S: {
					AddLoadAddress(GetLocal(il));
					break;
				}
				case Code.Stloc_0:
				case Code.Stloc_1:
				case Code.Stloc_2:
				case Code.Stloc_3:
				case Code.Stloc_S:
				case Code.Stloc: {
					AddSetFromTop(GetLocal(il));
					break;
				}
				case Code.Ldnull: {
					var src = TypedConst.CreateNull();
					AddLoadConst(src);
					break;
				}
				case Code.Ldc_I4_M1:
				case Code.Ldc_I4_0:
				case Code.Ldc_I4_1:
				case Code.Ldc_I4_2:
				case Code.Ldc_I4_3:
				case Code.Ldc_I4_4:
				case Code.Ldc_I4_5:
				case Code.Ldc_I4_6:
				case Code.Ldc_I4_7:
				case Code.Ldc_I4_8:
				case Code.Ldc_I4_S:
				case Code.Ldc_I4: {
					var src = TypedConst.CreateInt(il.GetLdcI4Value());
					AddLoadConst(src);
					break;
				}
				case Code.Ldc_I8: {
					var src = TypedConst.CreateLong((long)il.Operand);
					AddLoadConst(src);
					break;
				}
				case Code.Ldc_R4: {
					var src = TypedConst.CreateFloat((float)il.Operand);
					AddLoadConst(src);
					break;
				}
				case Code.Ldc_R8: {
					var src = TypedConst.CreateDouble((double)il.Operand);
					AddLoadConst(src);
					break;
				}
				case Code.Ldstr: {
					var src = TypedConst.CreateString((string)il.Operand);
					AddLoadConst(src);
					break;
				}
				case Code.Dup: {
					var src = _es.GetTop();
					AddLoad(src);
					break;
				}
				case Code.Pop: {
					_es.Pop();
					break;
				}
				case Code.Ret: {
					VariableInfo ret = _methodDef.ReturnType.ElementType == ElementType.Void ? null : _es.Pop();
					var ir = IRInstruction.Create(IRFamily.Ret, IROpCode.Ret, ret != null ? InstructionArgument.CreateVariable(ret) : null);
					AddInstruction(ir);
					break;
				}
				case Code.Br_S:
				case Code.Br: {
					AddBranch(il, 0);
					break;
				}
				case Code.Brfalse_S:
				case Code.Brfalse:
				case Code.Brtrue_S:
				case Code.Brtrue: {
					AddBranch(il, 1);
					break;
				}
				case Code.Beq:
				case Code.Beq_S:
				case Code.Bge:
				case Code.Bge_S:
				case Code.Bgt:
				case Code.Bgt_S:
				case Code.Ble:
				case Code.Ble_S:
				case Code.Blt:
				case Code.Blt_S:
				case Code.Bne_Un:
				case Code.Bne_Un_S:
				case Code.Bge_Un:
				case Code.Bge_Un_S:
				case Code.Bgt_Un:
				case Code.Bgt_Un_S:
				case Code.Ble_Un:
				case Code.Ble_Un_S:
				case Code.Blt_Un:
				case Code.Blt_Un_S: {
					AddBranch(il, 2);
					break;
				}
				case Code.Switch: {
					AddSwitch(il);
					break;
				}
				case Code.Ldind_I1:
				case Code.Ldind_U1:
				case Code.Ldind_I2:
				case Code.Ldind_U2:
				case Code.Ldind_I4:
				case Code.Ldind_U4:
				case Code.Ldind_I8:
				case Code.Ldind_I:
				case Code.Ldind_R4:
				case Code.Ldind_R8:
				case Code.Ldind_Ref: {
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(src.type);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.LoadIndirect, IROpCode.LoadIndirect, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					AddInstruction(ir);
					break;
				}
				case Code.Stind_I1:
				case Code.Stind_I2:
				case Code.Stind_I4:
				case Code.Stind_I8:
				case Code.Stind_I:
				case Code.Stind_R4:
				case Code.Stind_R8:
				case Code.Stind_Ref: {
					var src = _es.Pop();
					var dst = _es.Pop();
					var ir = IRInstruction.Create(IRFamily.StoreIndirect, IROpCode.StoreIndirect, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					AddInstruction(ir);
					break;
				}
				case Code.Add:
				case Code.Add_Ovf:
				case Code.Add_Ovf_Un:
				case Code.Sub:
				case Code.Sub_Ovf:
				case Code.Sub_Ovf_Un:
				case Code.Mul:
				case Code.Mul_Ovf:
				case Code.Mul_Ovf_Un:
				case Code.Div:
				case Code.Div_Un:
				case Code.Rem:
				case Code.Rem_Un:
				case Code.And:
				case Code.Or:
				case Code.Xor: {
					var op2 = _es.Pop();
					var op1 = _es.Pop();
					var dst = _vs.CreateTempVar(CalcArithOpResultType(op1.type, op2.type));
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.BinOp, GetArithIROpcodeByILCode(code), InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(op1), InstructionArgument.CreateVariable(op2));
					AddInstruction(ir);
					break;
				}
				case Code.Shl:
				case Code.Shr:
				case Code.Shr_Un: {
					var op2 = _es.Pop();
					var op1 = _es.Pop();
					var dst = _vs.CreateTempVar(op1.type);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.BinOp, GetArithIROpcodeByILCode(code), InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(op1), InstructionArgument.CreateVariable(op2));
					AddInstruction(ir);
					break;
				}
				case Code.Neg:
				case Code.Not: {
					var op1 = _es.Pop();
					var dst = _vs.CreateTempVar(op1.type);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.UnOp, GetUnaryIRCodeByILCode(code), InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(op1));
					AddInstruction(ir);
					break;
				}
				case Code.Conv_I1:
				case Code.Conv_I2:
				case Code.Conv_I4:
				case Code.Conv_I8:
				case Code.Conv_U1:
				case Code.Conv_U2:
				case Code.Conv_U4:
				case Code.Conv_U8:
				case Code.Conv_I:
				case Code.Conv_U:
				case Code.Conv_R4:
				case Code.Conv_R8:
				case Code.Conv_R_Un:
				case Code.Conv_Ovf_I1:
				case Code.Conv_Ovf_I2:
				case Code.Conv_Ovf_I4:
				case Code.Conv_Ovf_I8:
				case Code.Conv_Ovf_U1:
				case Code.Conv_Ovf_U2:
				case Code.Conv_Ovf_U4:
				case Code.Conv_Ovf_U8:
				case Code.Conv_Ovf_I:
				case Code.Conv_Ovf_U:
				case Code.Conv_Ovf_I1_Un:
				case Code.Conv_Ovf_I2_Un:
				case Code.Conv_Ovf_I4_Un:
				case Code.Conv_Ovf_I8_Un:
				case Code.Conv_Ovf_U1_Un:
				case Code.Conv_Ovf_U2_Un:
				case Code.Conv_Ovf_U4_Un:
				case Code.Conv_Ovf_U8_Un:
				case Code.Conv_Ovf_I_Un:
				case Code.Conv_Ovf_U_Un: {
					var op1 = _es.Pop();
					var dst = _vs.CreateTempVar(CalcConvertResultType(code));
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Conv, GetConvertIROpcodeByILCode(code), InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(op1));
					AddInstruction(ir);
					break;
				}
				case Code.Cpobj: {
					var src = _es.Pop();
					var dst = _es.Pop();
					var ir = IRInstruction.Create(IRFamily.Obj, IROpCode.CpObj, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					AddInstruction(ir);
					break;
				}
				case Code.Initobj: {
					var src = _es.Pop();
					var ir = IRInstruction.Create(IRFamily.Obj, IROpCode.InitObj, InstructionArgument.CreateVariable(src));
					AddInstruction(ir);
					break;
				}
				case Code.Ldobj: {
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(src.type);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Obj, IROpCode.LdObj, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					AddInstruction(ir);
					break;
				}
				case Code.Stobj: {
					var src = _es.Pop();
					var dst = _es.Pop();
					var ir = IRInstruction.Create(IRFamily.Obj, IROpCode.StObj, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					AddInstruction(ir);
					break;
				}
				case Code.Call:
				case Code.Callvirt: {
					IMethod callMethod = (IMethod)il.Operand;
					AddCall(GetCallIRCodeByILCode(code), callMethod, prefixData);
					break;
				}
				case Code.Calli: {
					MethodSig callMethodSig = (MethodSig)il.Operand;
					AddCall(IROpCode.CallI, il.Operand, callMethodSig.RetType, callMethodSig.Params.ToArray(), prefixData);
					break;
				}
				case Code.Newobj: {
					IMethod callMethod = (IMethod)il.Operand;
					AddNewObj(callMethod);
					break;
				}
				case Code.Castclass: {
					ITypeDefOrRef klass = (ITypeDefOrRef)il.Operand;
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.Object);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Cast, IROpCode.CastClass, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					ir.inlineOperand = klass;
					AddInstruction(ir);
					break;
				}
				case Code.Isinst: {
					ITypeDefOrRef klass = (ITypeDefOrRef)il.Operand;
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.Int32);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Cast, IROpCode.IsInst, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					ir.inlineOperand = klass;
					AddInstruction(ir);
					break;
				}
				case Code.Box: {
					ITypeDefOrRef klass = (ITypeDefOrRef)il.Operand;
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.Object);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Box, IROpCode.Box, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					ir.inlineOperand = klass;
					AddInstruction(ir);
					break;
				}
				case Code.Unbox: {
					ITypeDefOrRef klass = (ITypeDefOrRef)il.Operand;
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(klass.ToTypeSig());
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Box, IROpCode.Unbox, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					ir.inlineOperand = klass;
					AddInstruction(ir);
					break;
				}
				case Code.Unbox_Any: {
					ITypeDefOrRef klass = (ITypeDefOrRef)il.Operand;
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.IntPtr);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Box, IROpCode.Unbox_Any, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					ir.inlineOperand = klass;
					AddInstruction(ir);
					break;
				}
				case Code.Throw: {
					var src = _es.Pop();
					var ir = IRInstruction.Create(IRFamily.Exception, IROpCode.Throw, InstructionArgument.CreateVariable(src));
					AddInstruction(ir);
					_es.Clear();
					break;
				}
				case Code.Rethrow: {
					var ir = IRInstruction.Create(IRFamily.Exception, IROpCode.Rethrow);
					AddInstruction(ir);
					_es.Clear();
					break;
				}
				case Code.Leave:
				case Code.Leave_S: {
					IRBasicBlock target = GetIRBasicBlock((Instruction)il.Operand);
					_curIrbb.AddOutboundBasicBlock(target);
					var ir = IRInstruction.Create(IRFamily.Exception, IROpCode.Leave, target);
					AddInstruction(ir);
					_es.Clear();
					break;
				}
				case Code.Endfilter: {
					var value = _es.Pop();
					var ir = IRInstruction.Create(IRFamily.Exception, IROpCode.EndFilter, InstructionArgument.CreateVariable(value));
					AddInstruction(ir);
					break;
				}
				case Code.Endfinally: {
					var ir = IRInstruction.Create(IRFamily.Exception, IROpCode.EndFinallyOrFault);
					AddInstruction(ir);
					_es.Clear();
					break;
				}
				case Code.Ldfld: {
					var field = (IField)il.Operand;
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(GetFieldType(field));
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Field, IROpCode.Ldfld, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					ir.inlineOperand = field;
					AddInstruction(ir);
					break;
				}
				case Code.Ldflda: {
					var field = (IField)il.Operand;
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.IntPtr);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Field, IROpCode.Ldflda, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					ir.inlineOperand = field;
					AddInstruction(ir);
					break;
				}
				case Code.Stfld: {
					var field = (IField)il.Operand;
					var src = _es.Pop();
					var dst = _es.Pop();
					var ir = IRInstruction.Create(IRFamily.Field, IROpCode.Stfld, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					ir.inlineOperand = field;
					AddInstruction(ir);
					break;
				}
				case Code.Ldsfld: {
					var field = (IField)il.Operand;
					var dst = _vs.CreateTempVar(GetFieldType(field));
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Field, IROpCode.Ldsfld, InstructionArgument.CreateVariable(dst));
					ir.inlineOperand = field;
					AddInstruction(ir);
					break;
				}
				case Code.Ldsflda: {
					var field = (IField)il.Operand;
					var dst = _vs.CreateTempVar(_corLibTypes.UIntPtr);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Field, IROpCode.Ldsflda, InstructionArgument.CreateVariable(dst));
					ir.inlineOperand = field;
					AddInstruction(ir);
					break;
				}
				case Code.Stsfld: {
					var field = (IField)il.Operand;
					var src = _es.Pop();
					var ir = IRInstruction.Create(IRFamily.Field, IROpCode.Stsfld, InstructionArgument.CreateVariable(src));
					ir.inlineOperand = field;
					AddInstruction(ir);
					break;
				}
				case Code.Newarr: {
					var klass = (ITypeDefOrRef)il.Operand;
					var size = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.Object);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Array, IROpCode.Newarr, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(size));
					ir.inlineOperand = klass;
					AddInstruction(ir);
					break;
				}
				case Code.Ldlen: {
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.UIntPtr);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Array, IROpCode.LdLen, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					AddInstruction(ir);
					break;
				}
				case Code.Ldelema: {
					var index = _es.Pop();
					var arr = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.UIntPtr);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Array, IROpCode.Ldelema, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(arr), InstructionArgument.CreateVariable(index));
					AddInstruction(ir);
					break;
				}
				case Code.Ldelem_I1:
				case Code.Ldelem_U1:
				case Code.Ldelem_I2:
				case Code.Ldelem_U2:
				case Code.Ldelem_I4:
				case Code.Ldelem_U4:
				case Code.Ldelem_I8:
				case Code.Ldelem_I:
				case Code.Ldelem_R4:
				case Code.Ldelem_R8:
				case Code.Ldelem_Ref: {
					var index = _es.Pop();
					var arr = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.Object);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Array, GetArrayIROpcodeByILCode(code), InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(arr), InstructionArgument.CreateVariable(index));
					AddInstruction(ir);
					break;
				}
				case Code.Stelem_I:
				case Code.Stelem_I1:
				case Code.Stelem_I2:
				case Code.Stelem_I4:
				case Code.Stelem_I8:
				case Code.Stelem_R4:
				case Code.Stelem_R8:
				case Code.Stelem_Ref: {
					var value = _es.Pop();
					var index = _es.Pop();
					var arr = _es.Pop();
					var ir = IRInstruction.Create(IRFamily.Array, GetArrayIROpcodeByILCode(code), InstructionArgument.CreateVariable(arr), InstructionArgument.CreateVariable(index), InstructionArgument.CreateVariable(value));
					AddInstruction(ir);
					break;
				}
				case Code.Ldelem: {
					ITypeDefOrRef klass = (ITypeDefOrRef)il.Operand;
					var index = _es.Pop();
					var arr = _es.Pop();
					var dst = _vs.CreateTempVar(klass.ToTypeSig());
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Array, GetArrayIROpcodeByILCode(code), InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(arr), InstructionArgument.CreateVariable(index));
					ir.inlineOperand = klass;
					AddInstruction(ir);
					break;
				}
				case Code.Stelem: {
					ITypeDefOrRef klass = (ITypeDefOrRef)il.Operand;
					var value = _es.Pop();
					var index = _es.Pop();
					var arr = _es.Pop();
					var ir = IRInstruction.Create(IRFamily.Array, GetArrayIROpcodeByILCode(code), InstructionArgument.CreateVariable(arr), InstructionArgument.CreateVariable(index), InstructionArgument.CreateVariable(value));
					ir.inlineOperand = klass;
					AddInstruction(ir);
					break;
				}
				case Code.Ceq:
				case Code.Cgt:
				case Code.Cgt_Un:
				case Code.Clt:
				case Code.Clt_Un: {
					var op2 = _es.Pop();
					var op1 = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.Int32);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Compare, GetCompareIRCodeByILCode(code), InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(op1), InstructionArgument.CreateVariable(op2));
					AddInstruction(ir);
					break;
				}
				case Code.Ldftn: {
					var method = (IMethod)il.Operand;
					var dst = _vs.CreateTempVar(_corLibTypes.IntPtr);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Ldftn, IROpCode.Ldftn, InstructionArgument.CreateVariable(dst));
					ir.inlineOperand = method;
					AddInstruction(ir);
					break;
				}
				case Code.Ldvirtftn: {
					var method = (IMethod)il.Operand;
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.IntPtr);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Ldftn, IROpCode.Ldvirtftn, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src) );
					ir.inlineOperand = method;
					AddInstruction(ir);
					break;
				}
				case Code.Localloc: {
					var size = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.IntPtr);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Unspec, IROpCode.Localloc, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(size));
					AddInstruction(ir);
					break;
				}
				case Code.Unaligned: {
					int alignment = (int)il.Operand;
					prefixData = new PrefixData { code = PrefixCode.Unaligned, data = alignment };
					break;
				}
				case Code.Volatile: {
					prefixData = new PrefixData { code = PrefixCode.Volatile };
					break;
				}
				case Code.Tailcall: {
					prefixData = new PrefixData { code = PrefixCode.Tail };
					break;
				}
				case Code.Constrained: {
					var klass = (ITypeDefOrRef)il.Operand;
					prefixData = new PrefixData { code = PrefixCode.Constrained, data = klass };
					break;
				}
				case Code.Readonly: {
					prefixData = new PrefixData { code = PrefixCode.ReadOnly };
					break;
				}
				case Code.No: {
					prefixData = new PrefixData { code = PrefixCode.No, data = il.Operand };
					break;
				}
				case Code.Sizeof: {
					var klass = (ITypeDefOrRef)il.Operand;
					var dst = _vs.CreateTempVar(_corLibTypes.UIntPtr);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Unspec, IROpCode.Sizeof, InstructionArgument.CreateVariable(dst));
					ir.inlineOperand = klass;
					AddInstruction(ir);
					break;
				}
				case Code.Mkrefany: {
					var klass = (ITypeDefOrRef)il.Operand;
					var value = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.TypedReference);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Ref, IROpCode.Mkrefany, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(value));
					ir.inlineOperand = klass;
					AddInstruction(ir);
					break;
				}
				case Code.Refanytype: {
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(_corLibTypes.UIntPtr);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Ref, IROpCode.Refanytype, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					AddInstruction(ir);
					break;
				}
				case Code.Refanyval: {
					var klass = (ITypeDefOrRef)il.Operand;
					var src = _es.Pop();
					var dst = _vs.CreateTempVar(klass.ToTypeSig());
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Ref, IROpCode.Refanyval, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src));
					ir.inlineOperand = klass;
					AddInstruction(ir);
					break;
				}
				case Code.Ldtoken: {
					var token = (IMDTokenProvider)il.Operand;
					var dst = _vs.CreateTempVar(_corLibTypes.UIntPtr);
					_es.Push(dst);
					var ir = IRInstruction.Create(IRFamily.Unspec, IROpCode.Ldtoken, InstructionArgument.CreateVariable(dst));
					ir.inlineOperand = token;
					AddInstruction(ir);
					break;
				}
				case Code.Ckfinite: {
					var src = _es.GetTop();
					var ir = IRInstruction.Create(IRFamily.Unspec, IROpCode.Ckfinite, InstructionArgument.CreateVariable(src));
					AddInstruction(ir);
					break;
				}
				case Code.Initblk: {
					var size = _es.Pop();
					var value = _es.Pop();
					var src = _es.Pop();
					var ir = IRInstruction.Create(IRFamily.Unspec, IROpCode.Initblk, InstructionArgument.CreateVariable(src), InstructionArgument.CreateVariable(value), InstructionArgument.CreateVariable(size));
					AddInstruction(ir);
					break;
				}
				case Code.Cpblk: {
					var size = _es.Pop();
					var src = _es.Pop();
					var dst = _es.Pop();
					var ir = IRInstruction.Create(IRFamily.Unspec, IROpCode.Cpblk, InstructionArgument.CreateVariable(dst), InstructionArgument.CreateVariable(src), InstructionArgument.CreateVariable(size));
					AddInstruction(ir);
					break;
				}
				//case Code.Jmp:
				default: throw new NotSupportedException($"method:{_methodDef} IL:{il}");
				}
			}

			if (_curIrbb.nextIrbb != null) {
				var flowControl = bb.Instructions.Last().OpCode.FlowControl;
				if (flowControl != FlowControl.Return && flowControl != FlowControl.Branch && flowControl != FlowControl.Throw) {
					_es.SaveInbound(_curIrbb.nextIrbb);
					_curIrbb.AddOutboundBasicBlock(_curIrbb.nextIrbb);
				}
			}
			_es.SaveOutbound(_curIrbb);

		}

	}
}
