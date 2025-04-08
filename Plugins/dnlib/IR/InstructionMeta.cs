using System.Collections.Generic;


namespace dnlib.IR {

	public enum InstructionFlag {
		None = 0,
		InlineToken = 0x1,
		SideEffect = 0x2,
		InlineOffset = 0x4,
	}

	public class InstructionMeta {
		// family
		// opcode
		//public IRFamily family;
		public readonly List<InstructionArgumentMeta> args;

		public readonly InstructionFlag flag;

		public InstructionMeta(InstructionArgumentMeta[] args, InstructionFlag flag) {
			this.args = new List<InstructionArgumentMeta>(args);
			this.flag = flag;
		}

		public static InstructionMeta Create(params InstructionArgumentMeta[] args) {
			return new InstructionMeta(args, InstructionFlag.None);
		}

		public static InstructionMeta Create(InstructionFlag flag, params InstructionArgumentMeta[] args) {
			return new InstructionMeta(args, flag);
		}

		private readonly static InstructionMeta s_loadorSet = Create(
			new InstructionArgumentMeta { name = "dest", flag = ArgumentMetaFlag.Out },
			new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_loadAddress = Create(
			new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
			new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In | ArgumentMetaFlag.LoadAddress });

		private readonly static InstructionMeta s_loadConstant = Create(
						new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
						new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In | ArgumentMetaFlag.Constant }
						);

		private readonly static InstructionMeta s_call = Create(InstructionFlag.SideEffect | InstructionFlag.InlineToken,
									new InstructionArgumentMeta { name = "ret", flag = ArgumentMetaFlag.Out },
									new InstructionArgumentMeta { name = "params", flag = ArgumentMetaFlag.In | ArgumentMetaFlag.Variadic }
									);

		private readonly static InstructionMeta s_newobj = Create(InstructionFlag.SideEffect | InstructionFlag.InlineToken,
									new InstructionArgumentMeta { name = "ret", flag = ArgumentMetaFlag.Out },
									new InstructionArgumentMeta { name = "params", flag = ArgumentMetaFlag.In | ArgumentMetaFlag.Variadic }
									);

		private readonly static InstructionMeta s_ret = Create(InstructionFlag.SideEffect,
									new InstructionArgumentMeta { name = "value", flag = ArgumentMetaFlag.In }
									);

		private readonly static InstructionMeta s_br = Create(InstructionFlag.InlineOffset,
												new InstructionArgumentMeta { name = "params", flag = ArgumentMetaFlag.In | ArgumentMetaFlag.Variadic }
																					);
		private readonly static InstructionMeta s_condBr = Create(InstructionFlag.InlineOffset,
																new InstructionArgumentMeta { name = "params", flag = ArgumentMetaFlag.In | ArgumentMetaFlag.Variadic }
				);

		private readonly static InstructionMeta s_switch = Create(InstructionFlag.InlineOffset,
			new InstructionArgumentMeta { name = "value", flag = ArgumentMetaFlag.In }
		);

		private readonly static InstructionMeta s_compare = Create(
						new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
						new InstructionArgumentMeta { name = "op1", flag = ArgumentMetaFlag.In },
						new InstructionArgumentMeta { name = "op2", flag = ArgumentMetaFlag.In }
								);

		private readonly static InstructionMeta s_loadInd = Create(
			new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
			new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In | ArgumentMetaFlag.LoadIndirect });

		private readonly static InstructionMeta s_storeInd = Create(
			new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out | ArgumentMetaFlag.StoreIndirect },
						new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_binOp = Create(
						new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
									new InstructionArgumentMeta { name = "op1", flag = ArgumentMetaFlag.In },
												new InstructionArgumentMeta { name = "op2", flag = ArgumentMetaFlag.In }
														);

		private readonly static InstructionMeta s_unaryOp = Create(
									new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
																		new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In }
																																);

		private readonly static InstructionMeta s_conv = Create(
						new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
									new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In }
											);

		private readonly static InstructionMeta s_initobj = Create(
									new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.InOut | ArgumentMetaFlag.StoreIndirect }
																													);

		private readonly static InstructionMeta s_cpobj = Create(
						new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out | ArgumentMetaFlag.StoreIndirect },
									new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In | ArgumentMetaFlag.LoadIndirect }
											);

		private readonly static InstructionMeta s_ldobj = Create(
									new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
																		new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In | ArgumentMetaFlag.LoadIndirect }
																	);

		private readonly static InstructionMeta s_stobj = Create(
						new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out | ArgumentMetaFlag.StoreIndirect },
															new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In }

															);

		private readonly static InstructionMeta s_castclass = Create(InstructionFlag.InlineToken | InstructionFlag.SideEffect,
						new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
									new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In }
											);

		private readonly static InstructionMeta s_isinst = Create(InstructionFlag.InlineToken,
									new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
																		new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In }
																													);

		private readonly static InstructionMeta s_box = Create(InstructionFlag.InlineToken,
									new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
																		new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In }
																													);

		private readonly static InstructionMeta s_unbox = Create(InstructionFlag.InlineToken | InstructionFlag.SideEffect,
									new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
																		new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In }
																													);

		private readonly static InstructionMeta s_unboxAny = Create(InstructionFlag.InlineToken | InstructionFlag.SideEffect,
									new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
									new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_throw = Create(InstructionFlag.SideEffect,
												new InstructionArgumentMeta { name = "exObj", flag = ArgumentMetaFlag.In });
		private readonly static InstructionMeta s_rethrow = Create(InstructionFlag.SideEffect);

		private readonly static InstructionMeta s_leave = Create(InstructionFlag.InlineOffset);

		private readonly static InstructionMeta s_endFinallyOrFault = Create(InstructionFlag.SideEffect);

		private readonly static InstructionMeta s_endfilter = Create(InstructionFlag.SideEffect,
															new InstructionArgumentMeta { name = "value", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_loadExceptionObj = Create(
			new InstructionArgumentMeta { name = "exObj", flag = ArgumentMetaFlag.Out });

		private readonly static InstructionMeta s_ldfld = Create(InstructionFlag.InlineToken,
						new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
									new InstructionArgumentMeta { name = "obj", flag = ArgumentMetaFlag.In }
											);
		private readonly static InstructionMeta s_ldflda = Create(InstructionFlag.InlineToken,
									new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
																		new InstructionArgumentMeta { name = "obj", flag = ArgumentMetaFlag.In }
																													);

		private readonly static InstructionMeta s_stfld = Create(InstructionFlag.InlineToken | InstructionFlag.SideEffect,
						new InstructionArgumentMeta { name = "obj", flag = ArgumentMetaFlag.In },
															new InstructionArgumentMeta { name = "value", flag = ArgumentMetaFlag.In }
																										);

		private readonly static InstructionMeta s_ldsfld = Create(InstructionFlag.InlineToken | InstructionFlag.SideEffect,
									new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out }
																											 );

		private readonly static InstructionMeta s_ldsflda = Create(InstructionFlag.InlineToken | InstructionFlag.SideEffect,
			new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out });

		private readonly static InstructionMeta s_stsfld = Create(InstructionFlag.InlineToken | InstructionFlag.SideEffect,
			new InstructionArgumentMeta { name = "value", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_newarr = Create(InstructionFlag.InlineToken | InstructionFlag.SideEffect,
						new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
									new InstructionArgumentMeta { name = "size", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_ldlen = Create(InstructionFlag.SideEffect,
									new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
																		new InstructionArgumentMeta { name = "arr", flag = ArgumentMetaFlag.In }
																													);

		private readonly static InstructionMeta s_ldelema = Create(InstructionFlag.SideEffect,
									new InstructionArgumentMeta { name = "address", flag = ArgumentMetaFlag.Out },
									new InstructionArgumentMeta { name = "arr", flag = ArgumentMetaFlag.In },
									new InstructionArgumentMeta { name = "index", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_ldelem = Create(InstructionFlag.SideEffect,
							new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
							new InstructionArgumentMeta { name = "arr", flag = ArgumentMetaFlag.In },
							new InstructionArgumentMeta { name = "index", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_stelem = Create(InstructionFlag.SideEffect,
						new InstructionArgumentMeta { name = "arr", flag = ArgumentMetaFlag.In },
												new InstructionArgumentMeta { name = "index", flag = ArgumentMetaFlag.In },
																		new InstructionArgumentMeta { name = "value", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_ldelemAny = Create(InstructionFlag.SideEffect | InstructionFlag.InlineToken,
							new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
							new InstructionArgumentMeta { name = "arr", flag = ArgumentMetaFlag.In },
							new InstructionArgumentMeta { name = "index", flag = ArgumentMetaFlag.In }
			);

		private readonly static InstructionMeta s_stelemAny = Create(InstructionFlag.SideEffect | InstructionFlag.InlineToken,
									new InstructionArgumentMeta { name = "arr", flag = ArgumentMetaFlag.In },
															new InstructionArgumentMeta { name = "index", flag = ArgumentMetaFlag.In },
																					new InstructionArgumentMeta { name = "value", flag = ArgumentMetaFlag.In }
																								);

		private readonly static InstructionMeta s_loadftn = Create(InstructionFlag.InlineToken,
						new InstructionArgumentMeta { name = "method", flag = ArgumentMetaFlag.Out }
											);

		private readonly static InstructionMeta s_ldvirtftn = Create(InstructionFlag.InlineToken | InstructionFlag.SideEffect,
						new InstructionArgumentMeta { name = "method", flag = ArgumentMetaFlag.Out },
															new InstructionArgumentMeta { name = "obj", flag = ArgumentMetaFlag.In }
																										);

		private readonly static InstructionMeta s_localloc = Create(
			new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
									new InstructionArgumentMeta { name = "size", flag = ArgumentMetaFlag.In }
											);

		private readonly static InstructionMeta s_initblk = Create(
									new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In | ArgumentMetaFlag.StoreIndirect },
																		new InstructionArgumentMeta { name = "value", flag = ArgumentMetaFlag.In },
																														new InstructionArgumentMeta { name = "size", flag = ArgumentMetaFlag.In }
																																												);
		private readonly static InstructionMeta s_cpblk = Create(
									new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out | ArgumentMetaFlag.StoreIndirect },
									new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In | ArgumentMetaFlag.LoadIndirect },
									new InstructionArgumentMeta { name = "size", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_sizeof = Create(InstructionFlag.InlineToken,
			new InstructionArgumentMeta { name = "size", flag = ArgumentMetaFlag.Out });

		private readonly static InstructionMeta s_mkrefany = Create(InstructionFlag.InlineToken,
			new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
			new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_refanytype = Create(
			new InstructionArgumentMeta { name = "type", flag = ArgumentMetaFlag.Out },
			new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_refanyval = Create(InstructionFlag.InlineToken,
			new InstructionArgumentMeta { name = "dst", flag = ArgumentMetaFlag.Out },
			new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_ckfinite = Create(InstructionFlag.SideEffect,
			new InstructionArgumentMeta { name = "src", flag = ArgumentMetaFlag.In });

		private readonly static InstructionMeta s_ldtoken = Create(InstructionFlag.InlineToken,
						new InstructionArgumentMeta { name = "runtimeHandle", flag = ArgumentMetaFlag.Out });



		public static InstructionMeta Get(IROpCode code) {
			switch (code) {
			case IROpCode.LoadOrSet: return s_loadorSet;
			case IROpCode.LoadAddress: return s_loadAddress;
			case IROpCode.LoadConstant: return s_loadConstant;
			case IROpCode.Call:
			case IROpCode.CallVir:
			case IROpCode.CallI: return s_call;
			case IROpCode.NewObj: return s_newobj;
			case IROpCode.Ret: return s_ret;
			case IROpCode.UnconditionBranch: return s_br;
			case IROpCode.BranchFalse:
			case IROpCode.BranchTrue:
			case IROpCode.Beq:
			case IROpCode.Bge:
			case IROpCode.Bgt:
			case IROpCode.Ble:
			case IROpCode.Blt:
			case IROpCode.Bne_Un:
			case IROpCode.Bge_Un:
			case IROpCode.Bgt_Un:
			case IROpCode.Ble_Un:
			case IROpCode.Blt_Un: return s_condBr;
			case IROpCode.Switch: return s_switch;
			case IROpCode.Ceq:
			case IROpCode.Cgt:
			case IROpCode.Cgt_Un:
			case IROpCode.Clt:
			case IROpCode.Clt_Un: return s_compare;
			case IROpCode.LoadIndirect: return s_loadInd;
			case IROpCode.StoreIndirect: return s_storeInd;
			case IROpCode.Add:
			case IROpCode.Add_Ovf:
			case IROpCode.Add_Ovf_Un:
			case IROpCode.Sub:
			case IROpCode.Sub_Ovf:
			case IROpCode.Sub_Ovf_Un:
			case IROpCode.Mul:
			case IROpCode.Mul_Ovf:
			case IROpCode.Mul_Ovf_Un:
			case IROpCode.Div:
			case IROpCode.Div_Un:
			case IROpCode.Rem:
			case IROpCode.Rem_Un:
			case IROpCode.And:
			case IROpCode.Or:
			case IROpCode.Xor:
			case IROpCode.Shl:
			case IROpCode.Shr:
			case IROpCode.Shr_Un: return s_binOp;
			case IROpCode.Neg:
			case IROpCode.Not: return s_unaryOp;
			case IROpCode.Conv_I1:
			case IROpCode.Conv_I2:
			case IROpCode.Conv_I4:
			case IROpCode.Conv_I8:
			case IROpCode.Conv_U1:
			case IROpCode.Conv_U2:
			case IROpCode.Conv_U4:
			case IROpCode.Conv_U8:
			case IROpCode.Conv_I:
			case IROpCode.Conv_U:
			case IROpCode.Conv_R4:
			case IROpCode.Conv_R8:
			case IROpCode.Conv_Ovf_I1:
			case IROpCode.Conv_Ovf_I2:
			case IROpCode.Conv_Ovf_I4:
			case IROpCode.Conv_Ovf_I8:
			case IROpCode.Conv_Ovf_U1:
			case IROpCode.Conv_Ovf_U2:
			case IROpCode.Conv_Ovf_U4:
			case IROpCode.Conv_Ovf_U8:
			case IROpCode.Conv_Ovf_I:
			case IROpCode.Conv_Ovf_U:
			case IROpCode.Conv_Ovf_I1_Un:
			case IROpCode.Conv_Ovf_I2_Un:
			case IROpCode.Conv_Ovf_I4_Un:
			case IROpCode.Conv_Ovf_I8_Un:
			case IROpCode.Conv_Ovf_U1_Un:
			case IROpCode.Conv_Ovf_U2_Un:
			case IROpCode.Conv_Ovf_U4_Un:
			case IROpCode.Conv_Ovf_U8_Un:
			case IROpCode.Conv_Ovf_I_Un:
			case IROpCode.Conv_Ovf_U_Un: return s_conv;
			case IROpCode.InitObj: return s_initobj;
			case IROpCode.CpObj: return s_cpobj;
			case IROpCode.LdObj: return s_ldobj;
			case IROpCode.StObj: return s_stobj;
			case IROpCode.CastClass: return s_castclass;
			case IROpCode.IsInst: return s_isinst;
			case IROpCode.Box: return s_box;
			case IROpCode.Unbox: return s_unbox;
			case IROpCode.Unbox_Any: return s_unboxAny;
			case IROpCode.Throw: return s_throw;
			case IROpCode.Rethrow: return s_rethrow;
			case IROpCode.Leave: return s_leave;
			case IROpCode.EndFinallyOrFault: return s_endFinallyOrFault;
			case IROpCode.EndFilter: return s_endfilter;
			case IROpCode.LoadExceptionObject: return s_loadExceptionObj;
			case IROpCode.Ldfld: return s_ldfld;
			case IROpCode.Ldflda: return s_ldflda;
			case IROpCode.Stfld: return s_stfld;
			case IROpCode.Ldsfld: return s_ldsfld;
			case IROpCode.Ldsflda: return s_ldsflda;
			case IROpCode.Stsfld: return s_stsfld;
			case IROpCode.Newarr: return s_newarr;
			case IROpCode.LdLen: return s_ldlen;
			case IROpCode.Ldelema: return s_ldelema;
			case IROpCode.Ldelem_I1:
			case IROpCode.Ldelem_U1:
			case IROpCode.Ldelem_I2:
			case IROpCode.Ldelem_U2:
			case IROpCode.Ldelem_I4:
			case IROpCode.Ldelem_U4:
			case IROpCode.Ldelem_I8:
			case IROpCode.Ldelem_I:
			case IROpCode.Ldelem_R4:
			case IROpCode.Ldelem_R8:
			case IROpCode.Ldelem_Ref: return s_ldelem;
			case IROpCode.Stelem_I:
			case IROpCode.Stelem_I1:
			case IROpCode.Stelem_I2:
			case IROpCode.Stelem_I4:
			case IROpCode.Stelem_I8:
			case IROpCode.Stelem_R4:
			case IROpCode.Stelem_R8:
			case IROpCode.Stelem_Ref: return s_stelem;

			case IROpCode.Stelem: return s_stelemAny;
			case IROpCode.Ldelem: return s_ldelemAny;
			case IROpCode.Ldftn: return s_loadftn;
			case IROpCode.Ldvirtftn: return s_ldvirtftn;
			case IROpCode.Localloc: return s_localloc;
			case IROpCode.Initblk: return s_initblk;
			case IROpCode.Cpblk: return s_cpblk;
			case IROpCode.Sizeof: return s_sizeof;
			case IROpCode.Mkrefany: return s_mkrefany;
			case IROpCode.Refanytype: return s_refanytype;
			case IROpCode.Refanyval: return s_refanyval;
			case IROpCode.Ckfinite: return s_ckfinite;
			case IROpCode.Ldtoken: return s_ldtoken;
			default: throw new System.Exception($"Unknown opcode:{code}");
			}
		}
	}
}
