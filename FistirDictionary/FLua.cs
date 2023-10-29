using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.IronLua;

namespace FistirDictionary
{
    internal class FLua
    {
        public LuaGlobal LuaEnvironment { get; set; }

        public static FLua LoadScansionScript(string scansionScriptPath)
        {
            var lua = new Lua();
            var env = lua.CreateEnvironment<LuaGlobal>();
            var chunk = lua.CompileChunk(scansionScriptPath, options: new LuaCompileOptions());
            env.DoChunk(chunk);
            return new FLua() { LuaEnvironment = env };
        }

        public static FLua LoadDerivationScript(string derivationScriptPath)
        {
            var lua = new Lua();
            var env = lua.CreateEnvironment<LuaGlobal>();
            var chunk = lua.CompileChunk(derivationScriptPath, options: new LuaCompileOptions());
            env.DoChunk(chunk);
            return new FLua() { LuaEnvironment = env };
        }

        public string Scan(string headword, string translation, string example, string dictionaryName)
        {
            var result = LuaEnvironment.CallMember("scan", headword, translation, example, dictionaryName);
            return result.ToString();
        }
    }
}
