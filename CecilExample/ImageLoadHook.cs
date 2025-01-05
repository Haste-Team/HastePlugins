using System.Runtime.CompilerServices;
using Landfall.Modding;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CecilExample;

public static class ImageLoadHook
{
    [ModuleInitializer]
    public static void Hook()
    {
        var cecilAsm = CecilPatcher.EditAssembly("Assembly-CSharp");
        var playerSlowMovement = cecilAsm.MainModule.GetType("PlayerSlowMovement");
        var body = playerSlowMovement.Methods.First(m => m.Name == "Jump").Body.GetILProcessor();
        var ldJumpForce = body.Body.Instructions.First(
            i => i.OpCode.Code == Code.Ldfld && i.Operand is FieldReference { Name: "jumpForce" });
        body.Remove(ldJumpForce.Previous); // remove load `this`
        body.Replace(ldJumpForce, body.Create(OpCodes.Ldc_R4, 100f));
    }
}
