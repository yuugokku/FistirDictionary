using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Neo.IronLua;

namespace FistirDictionary
{
    internal class FLua
    {
        public LuaGlobal LuaEnvironment { get; set; }

        private int rhymeCalculatedCount = 0;

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
            rhymeCalculatedCount++;
            return result.ToString();
        }

        public bool HasScanned()
        {
            return rhymeCalculatedCount > 0;
        }

        public static string GetScriptHash(string scriptPath)
        {
            using var sr = new System.IO.StreamReader(scriptPath);
            var script = sr.ReadToEnd();
            byte[] scriptBytes = Encoding.UTF8.GetBytes(script);

            SHA1 sha1 = SHA1.Create();
            byte[] scriptByteHashed = sha1.ComputeHash(scriptBytes);
            sha1.Clear();

            StringBuilder sb = new StringBuilder();
            foreach (byte b in scriptByteHashed)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
