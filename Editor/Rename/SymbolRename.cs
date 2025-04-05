using Obfuz.Rename;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Obfuz
{

    public class SymbolRename
    {
        private readonly ObfuscatorContext _ctx;

        private readonly IRenamePolicy _renamePolicy;

        public SymbolRename(ObfuscatorContext ctx)
        {
            _ctx = ctx;
            _renamePolicy = new RenamePolicy();
        }

        public void Process()
        {

        }
    }
}
