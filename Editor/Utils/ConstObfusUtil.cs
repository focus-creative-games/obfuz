using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Data;
using System.Collections.Generic;

namespace Obfuz.Utils
{
    internal static class ConstObfusUtil
    {
        public static void LoadConstInt(int a, IRandom random, float constProbability, ModuleConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            Instruction inst;
            if (random.NextInPercentage(constProbability))
            {
                inst = Instruction.Create(OpCodes.Ldc_I4, a);
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                inst = Instruction.Create(OpCodes.Ldsfld, field);
            }
            outputInsts.Add(inst);
        }

        public static void LoadConstLong(long a, IRandom random, float constProbability, ModuleConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            Instruction inst;
            if (random.NextInPercentage(constProbability))
            {
                inst = Instruction.Create(OpCodes.Ldc_I8, a);
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                inst = Instruction.Create(OpCodes.Ldsfld, field);
            }
            outputInsts.Add(inst);
        }

        public static void LoadConstFloat(float a, IRandom random, float constProbability, ModuleConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            Instruction inst;
            if (random.NextInPercentage(constProbability))
            {
                inst = Instruction.Create(OpCodes.Ldc_R4, a);
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                inst = Instruction.Create(OpCodes.Ldsfld, field);
            }
            outputInsts.Add(inst);
        }

        public static void LoadConstDouble(double a, IRandom random, float constProbability, ModuleConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            Instruction inst;
            if (random.NextInPercentage(constProbability))
            {
                inst = Instruction.Create(OpCodes.Ldc_R8, a);
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                inst = Instruction.Create(OpCodes.Ldsfld, field);
            }
            outputInsts.Add(inst);
        }


        public static void LoadConstTwoInt(int a, int b, IRandom random, float constProbability, ModuleConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            if (random.NextInPercentage(constProbability))
            {
                outputInsts.Add(Instruction.Create(OpCodes.Ldc_I4, a));

                // at most one ldc instruction
                FieldDef field = constFieldAllocator.Allocate(b);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));

                if (random.NextInPercentage(constProbability))
                {
                    // at most one ldc instruction
                    outputInsts.Add(Instruction.Create(OpCodes.Ldc_I4, b));
                }
                else
                {
                    field = constFieldAllocator.Allocate(b);
                    outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                }
            }
        }

        public static void LoadConstTwoLong(long a, long b, IRandom random, float constProbability, ModuleConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            if (random.NextInPercentage(constProbability))
            {
                outputInsts.Add(Instruction.Create(OpCodes.Ldc_I8, a));
                // at most one ldc instruction
                FieldDef field = constFieldAllocator.Allocate(b);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                if (random.NextInPercentage(constProbability))
                {
                    // at most one ldc instruction
                    outputInsts.Add(Instruction.Create(OpCodes.Ldc_I8, b));
                }
                else
                {
                    field = constFieldAllocator.Allocate(b);
                    outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                }
            }
        }

        public static void LoadConstTwoFloat(float a, float b, IRandom random, float constProbability, ModuleConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            if (random.NextInPercentage(constProbability))
            {
                outputInsts.Add(Instruction.Create(OpCodes.Ldc_R4, a));
                // at most one ldc instruction
                FieldDef field = constFieldAllocator.Allocate(b);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                if (random.NextInPercentage(constProbability))
                {
                    // at most one ldc instruction
                    outputInsts.Add(Instruction.Create(OpCodes.Ldc_R4, b));
                }
                else
                {
                    field = constFieldAllocator.Allocate(b);
                    outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                }
            }
        }

        public static void LoadConstTwoDouble(double a, double b, IRandom random, float constProbability, ModuleConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            if (random.NextInPercentage(constProbability))
            {
                outputInsts.Add(Instruction.Create(OpCodes.Ldc_R8, a));
                // at most one ldc instruction
                FieldDef field = constFieldAllocator.Allocate(b);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                if (random.NextInPercentage(constProbability))
                {
                    // at most one ldc instruction
                    outputInsts.Add(Instruction.Create(OpCodes.Ldc_R8, b));
                }
                else
                {
                    field = constFieldAllocator.Allocate(b);
                    outputInsts.Add(Instruction.Create(OpCodes.Ldsfld, field));
                }
            }
        }
    }
}
