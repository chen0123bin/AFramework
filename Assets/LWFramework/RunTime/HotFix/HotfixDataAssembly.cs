using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class HotfixDataAssembly : Singleton<HotfixDataAssembly>
{
    private Assembly assembly;

    public Assembly Assembly { get => assembly; }
    public HotfixDataAssembly()
    {
        assembly = Assembly.Load("Assembly-CSharp");
    }
}
