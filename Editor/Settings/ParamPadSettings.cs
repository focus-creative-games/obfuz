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

using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obfuz.Settings
{
    public class ParamPadSettingsFacade
    {
        public int randomSeed;
        public int minCount;
        public int maxCount;
        public List<string> ruleFiles;
        public List<Type> customRenamePolicyTypes;
    }

    [Serializable]
    public class ParamPadSettings
    {
        [Tooltip("random seed for the junk parameters. 0 draws a fresh seed on every build, so every build has a different signature table")]
        public int randomSeed = 0;

        [Tooltip("minimum number of junk parameters added to an eligible method")]
        [Range(1, 20)]
        public int minCount = 5;

        [Tooltip("maximum number of junk parameters added to an eligible method. Every call to a padded method writes this many extra argument slots, so lower it if a build shows a measurable cost")]
        [Range(1, 20)]
        public int maxCount = 10;

        [Tooltip("a method is only padded if it is also safe to rename, so these are the symbol obfuscation rule files")]
        public string[] ruleFiles;

        [Tooltip("custom rename policy types, same contract as SymbolObfuscationSettings.customRenamePolicyTypes")]
        public string[] customRenamePolicyTypes;

        public ParamPadSettingsFacade ToFacade()
        {
            return new ParamPadSettingsFacade
            {
                randomSeed = randomSeed,
                // an asset serialized before this section existed deserializes as zeros, which
                // the transform rejects; clamp rather than fail the build
                minCount = Math.Max(1, minCount),
                maxCount = Math.Max(Math.Max(1, minCount), maxCount),
                ruleFiles = ruleFiles?.ToList() ?? new List<string>(),
                customRenamePolicyTypes = customRenamePolicyTypes?.Select(typeName => ReflectionUtil.FindUniqueTypeInCurrentAppDomain(typeName)).ToList() ?? new List<Type>(),
            };
        }
    }
}
