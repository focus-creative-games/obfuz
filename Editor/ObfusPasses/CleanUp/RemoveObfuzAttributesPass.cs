// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

﻿using dnlib.DotNet;
using Obfuz.Editor;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.CleanUp
{
    public class RemoveObfuzAttributesPass : ObfuscationPassBase
    {
        public override ObfuscationPassType Type => ObfuscationPassType.None;

        public override void Start()
        {
        }

        public override void Stop()
        {

        }


        private void RemoveObfuzAttributes(IHasCustomAttribute provider)
        {
            CustomAttributeCollection customAttributes = provider.CustomAttributes;
            if (customAttributes.Count == 0)
                return;
            var toRemove = new List<CustomAttribute>();
            customAttributes.RemoveAll(ConstValues.ObfuzIgnoreAttributeFullName);
            customAttributes.RemoveAll(ConstValues.EncryptFieldAttributeFullName);
        }

        public override void Process()
        {
            var ctx = ObfuscationPassContext.Current;
            foreach (ModuleDef mod in ctx.modulesToObfuscate)
            {
                RemoveObfuzAttributes(mod);
                foreach (TypeDef type in mod.GetTypes())
                {
                    RemoveObfuzAttributes(type);
                    foreach (FieldDef field in type.Fields)
                    {
                        RemoveObfuzAttributes(field);
                    }
                    foreach (MethodDef method in type.Methods)
                    {
                        RemoveObfuzAttributes(method);
                        foreach (Parameter param in method.Parameters)
                        {
                            if (param.ParamDef != null)
                            {
                                RemoveObfuzAttributes(param.ParamDef);
                            }
                        }
                    }
                    foreach (PropertyDef property in type.Properties)
                    {
                        RemoveObfuzAttributes(property);
                    }
                    foreach (EventDef eventDef in type.Events)
                    {
                        RemoveObfuzAttributes(eventDef);
                    }
                }
            }
        }
    }
}
