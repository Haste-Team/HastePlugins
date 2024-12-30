using Landfall.Modding;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace CecilExample;

public static class ImageLoadHook
{
    public static void Hook(IntPtr imagePtr)
    {
        var image = new MonoImage(imagePtr);
        if (!image.Storage.Key.Contains("Assembly-CSharp"))
            return;
        var resolver = new DefaultAssemblyResolver();
        // add game's assembly directories to the resolve search path
        resolver.AddSearchDirectory(Path.Join(Application.dataPath, "Managed"));
        var readerParams = new ReaderParameters { AssemblyResolver = resolver };
        using var stream = image.AssemblyDataStream;
        var cecilAsm = AssemblyDefinition.ReadAssembly(stream, readerParams);
        var playerSlowMovement = cecilAsm.MainModule.GetType("PlayerSlowMovement");
        var body = playerSlowMovement.Methods.First(m => m.Name == "Jump").Body.GetILProcessor();
        var ldJumpForce = body.Body.Instructions.First(
            i => i.OpCode.Code == Code.Ldfld && i.Operand is FieldReference { Name: "jumpForce" });
        body.Remove(ldJumpForce.Previous); // remove load `this`
        body.Replace(ldJumpForce, body.Create(OpCodes.Ldc_R4, 100f));
        using var memstream = new MemoryStream();
        cecilAsm.Write(memstream);
        image.OverwriteAssembly(memstream);
        Debug.Log("CecilExample jump hook installed");
    }
}
