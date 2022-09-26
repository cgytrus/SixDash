using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace SixDash.Patcher;

// ReSharper disable once UnusedType.Global UnusedMember.Global
public static class Patcher {
    // ReSharper disable once InconsistentNaming UnusedMember.Global
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void Patch(AssemblyDefinition assembly) {
        ModuleDefinition module = assembly.Modules[0];
        //TypeDefinition list = module.GetType("System.Collections.Generic", "List`1");

        TypeDefinition checkpointScript = module.GetType("CheckpointScript");
        checkpointScript.Fields.Add(new FieldDefinition("savedOrbs", FieldAttributes.Private,
            module.TypeSystem.Single.MakeArrayType()));
        checkpointScript.Fields.Add(new FieldDefinition("savedPads", FieldAttributes.Private,
            module.TypeSystem.Single.MakeArrayType()));
        checkpointScript.Fields.Add(new FieldDefinition("savedPortals", FieldAttributes.Private,
            module.TypeSystem.Boolean.MakeArrayType()));

        TypeDefinition worldGen0 = module.GetType("WorldGenerator");
        TypeDefinition worldGen1 = module.GetType("WorldGeneratorEditor");
        AddMethod(worldGen0, new MethodDefinition("FixedUpdate", MethodAttributes.Private, module.TypeSystem.Void));
        AddMethod(worldGen1, new MethodDefinition("FixedUpdate", MethodAttributes.Private, module.TypeSystem.Void));
        AddMethod(worldGen0, new MethodDefinition("OnDestroy", MethodAttributes.Private, module.TypeSystem.Void));
        AddMethod(worldGen1, new MethodDefinition("OnDestroy", MethodAttributes.Private, module.TypeSystem.Void));

        TypeDefinition itemScript = module.GetType("ItemScript");
        RemoveMethod(itemScript, "Start");
        RemoveMethod(itemScript, "FixedUpdate");

        RemoveMethod(module.GetType("ColorChanger"), "Update");

        RemoveMethod(module.GetType("LevelManager"), "FixedUpdate");
        RemoveMethod(module.GetType("LevelManagerEditor"), "FixedUpdate");
    }

    private static void AddMethod(TypeDefinition type, MethodDefinition definition) {
        if(type.Methods.Any(def => def.Name == definition.Name))
            return;
        definition.Body.GetILProcessor().Emit(OpCodes.Ret);
        type.Methods.Add(definition);
    }

    private static void RemoveMethod(TypeDefinition type, string name) {
        MethodDefinition? definition = type.Methods.FirstOrDefault(def => def.Name == name);
        if(definition is null)
            return;
        type.Methods.Remove(definition);
    }

    //private static GenericInstanceType MakeList(TypeReference list, TypeReference type) =>
    //    list.MakeGenericInstanceType(type);
}
