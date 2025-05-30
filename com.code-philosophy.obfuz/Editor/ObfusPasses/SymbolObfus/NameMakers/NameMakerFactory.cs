using System.Collections.Generic;

namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{
    public static class NameMakerFactory
    {
        public static INameMaker CreateDebugNameMaker()
        {
            return new DebugNameMaker();
        }

        public static INameMaker CreateNameMakerBaseASCIICharSet(string namePrefix)
        {
            var words = new List<string>();
            for (int i = 0; i < 26; i++)
            {
                words.Add(((char)('a' + i)).ToString());
                words.Add(((char)('A' + i)).ToString());
            }
            return new WordSetNameMaker(namePrefix, words);
        }
    }
}
