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

﻿using System;
using System.Collections.Generic;

namespace Obfuz.EncryptionVM.Instructions
{

    public class EncryptFunction : EncryptionInstructionBase
    {
        private readonly IEncryptionInstruction[] _instructions;

        public EncryptFunction(IEncryptionInstruction[] instructions)
        {
            _instructions = instructions;
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            foreach (var instruction in _instructions)
            {
                value = instruction.Encrypt(value, secretKey, salt);
            }
            return value;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            for (int i = _instructions.Length - 1; i >= 0; i--)
            {
                value = _instructions[i].Decrypt(value, secretKey, salt);
            }
            return value;
        }

        public override void GenerateEncryptCode(List<string> lines, string indent)
        {
            throw new NotImplementedException();
        }

        public override void GenerateDecryptCode(List<string> lines, string indent)
        {
            throw new NotImplementedException();
        }
    }
}
