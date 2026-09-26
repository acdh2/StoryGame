using UnityEngine;
using MoonSharp.Interpreter;

public class LuaTest : MonoBehaviour
{
    void Start()
    {
        var lua = new Script();
        lua.Globals["print"] = (System.Action<string>)Debug.Log;
        
        lua.DoString(@"
            print('Hallo vanuit Lua!')
            return 5
        ");

        DynValue res = Script.RunString("return 15");
	    print(res.Number);
    }
}