using System.Runtime.CompilerServices;
using UnityEngine;

public static class MyModuleInitializer
{
    [ModuleInitializer]
    public static void ModuleInit()
    {
        Debug.Log("Hello from module initializer");
    }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ModuleInitializerAttribute : Attribute
    {
        public ModuleInitializerAttribute()
        {
        }
    }
}

