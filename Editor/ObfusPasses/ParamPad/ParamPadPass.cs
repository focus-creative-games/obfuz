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

using dnlib.DotNet;
using Obfuz.ObfusPasses.SymbolObfus;
using Obfuz.Settings;
using System;
using UnityEngine;

namespace Obfuz.ObfusPasses.ParamPad
{
    public class ParamPadPass : ObfuscationPassBase
    {
        private readonly ParamPadSettingsFacade _settings;
        private IObfuscationPolicy _renamePolicy;

        public override ObfuscationPassType Type => ObfuscationPassType.ParamPad;

        public ParamPadPass(ParamPadSettingsFacade settings)
        {
            _settings = settings;
        }

        public override void Start()
        {
            _renamePolicy = SymbolRename.CreateDefaultRenamePolicy(_settings.ruleFiles, _settings.customRenamePolicyTypes, ObfuscationPassType.ParamPad);
        }

        /// <summary>
        /// Deliberately empty. The work happens in Stop(), see the comment there.
        /// </summary>
        public override void Process()
        {
        }

        /// <summary>
        /// Padding runs in Stop(), not Process(), and is registered after CallObfus.
        ///
        /// CallObfus generates its dispatch proxy BODIES in Stop(). Running before it means those
        /// bodies do not exist yet and their calls to padded methods keep the old argument count
        /// (a broken build). Running after it in Process() is impossible for the same reason. But
        /// Stop() runs in registration order, so padding last in Stop() sees the finished proxies.
        ///
        /// That ordering is what keeps the proxies useful. CallObfus groups call targets by shared
        /// signature, so if it saw padded signatures the pool would fragment — measured on the real
        /// game, hubs went from 3209 (mean 6.2 callees) to 9717 (mean 2.46, median 1), i.e. mostly
        /// one-to-one indirections that any tool collapses. Padding afterwards leaves the proxy
        /// signatures unpadded, so the hubs stay dense, and the junk arguments get materialised
        /// once inside each proxy case instead of at every call site that funnels through it.
        ///
        /// Everything else has already run by now, which is also why the junk constants are never
        /// const-encrypted and the consume fold is never re-obfuscated by ExprObfus or flattened
        /// by ControlFlowObfus.
        /// </summary>
        public override void Stop()
        {
            var ctx = ObfuscationPassContext.Current;
            int seed = _settings.randomSeed != 0 ? _settings.randomSeed : (Guid.NewGuid().GetHashCode() | 1);
            Debug.Log($"[ParamPad] padding parameters with seed {seed}, count range [{_settings.minCount},{_settings.maxCount}].");

            var padding = new ParameterPadding(seed, _settings.minCount, _settings.maxCount, IsSafeToPad);
            padding.Process(ctx.modulesToObfuscate, ctx.allObfuscationRelativeModules);
            Debug.Log($"[ParamPad] padded {padding.PaddedMethodCount} of {padding.CandidateCount} candidate methods ({padding.VetoedCount} vetoed because a call site could not be rewritten).");
        }

        private bool IsSafeToPad(MethodDef method)
        {
            var ctx = ObfuscationPassContext.Current;
            if (ctx.whiteList.IsInWhiteList(method.Module) || ctx.whiteList.IsInWhiteList(method.DeclaringType) || ctx.whiteList.IsInWhiteList(method))
            {
                return false;
            }
            if (!Support(ctx.passPolicy.GetMethodObfuscationPasses(method)))
            {
                return false;
            }
            if (ctx.obfuzIgnoreScopeComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(method, method.DeclaringType, ObfuzScope.MethodParameter))
            {
                return false;
            }
            // the rename policy already encodes every contract that binds a method from outside
            // the IL: MonoBehaviour messages, DOTS and source generated types, MonoPInvokeCallback,
            // delegate members, plus the project's own rule files and custom policies.
            return _renamePolicy.NeedRename(method);
        }
    }
}
