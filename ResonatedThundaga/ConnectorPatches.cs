using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using UnityEngine;
using UnityFrooxEngineRunner;
using Component = FrooxEngine.Component;
using MeshRenderer = FrooxEngine.MeshRenderer;
using SkinnedMeshRenderer = FrooxEngine.SkinnedMeshRenderer;

namespace Thundaga
{
    [HarmonyPatch]
    public static class ComponentBasePatch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ComponentBase<Component>), "InternalRunStartup")]
        public static void InternalRunStartup(ComponentBase<Component> instance) =>
            throw new NotImplementedException();
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ComponentBase<Component>), "InternalRunDestruction")]
        public static void InternalRunDestruction(ComponentBase<Component> instance) =>
            throw new NotImplementedException();
    }



    //Patching generics is a pain, so we patch skinned and normal mesh renderers. this is the mesh renderer patch.
    /*
    [HarmonyPatch(typeof(MeshRendererConnectorBase<MeshRenderer, UnityEngine.MeshRenderer>))]
    public class MeshRendererConnectorPatch
    {
        [HarmonyPatch("ApplyChanges")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ApplyChangesTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {

            Thundaga.Msg("patching MeshRendererConnectorBase for SkinnedMeshRendererConnector implementation");

            return transpilerCodeMeshConnectorGeneric.ApplyChangesTranspilerGeneric(instructions);


        }
    }

    public static class transpilerCodeMeshConnectorGeneric{
        public static IEnumerable<CodeInstruction> ApplyChangesTranspilerGeneric(
            IEnumerable<CodeInstruction> instructions)
        {

            //remove GetWasChangedAndClear methods to prevent thread errors
            var codes = new List<CodeInstruction>(instructions);
            codes.Reverse();
            
            for (var i = 0; i < codes.Count; i++)
            {
                //this makes more sense, since instead of looking for the op code, look for the operand calling GetWasChangedAndClear and remove and and surrounding till it doesn't cause errors.
                //- @989onan
                if (codes[i].opcode != OpCodes.Callvirt || !codes[i].operand.ToString().Contains("GetWasChangedAndClear")) continue;

                for (var h = 0; h < 3; h++)
                {
                    var code = codes[i + h];
                    code.opcode = OpCodes.Nop;
                    code.operand = null;
                }
                    
            }


            //get rid of renderer enabled to prevent thread errors.
            for (var i = 0; i < codes.Count; i++){
                if (codes[i].opcode != OpCodes.Callvirt || !codes[i].operand.ToString().Contains("Renderer::set_Enabled")) continue;

                for(var h = 0; h< 5; h++)
                {
                    var code = codes[i + h];
                    code.opcode = OpCodes.Nop;
                    code.operand = null;

                }

            }

            //get rid of setting mesh was changed to prevent thread errors? 
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call || !codes[i].operand.ToString().Contains("set_meshWasChanged")) continue;
                for (var h = 0; h < 9; h++)
                {
                    var code = codes[i + h];
                    code.opcode = OpCodes.Nop;
                    code.operand = null;
                }
                break;
            }
            codes.Reverse();



            //replace generic set with our method - Fro Zen
            //this 

            var index = codes.IndexOf(codes.Where(i =>
                i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("AddComponent")).ToList()[1]);
            codes[index].operand = typeof(MeshGenericFix).GetMethod("SetMeshRendererPatch");
            codes[index].opcode = OpCodes.Call;
            codes.Insert(index, new CodeInstruction(OpCodes.Ldarg_0));
            Thundaga.Msg("Patched Mesh renderer connector.");

            return codes;
        }


    }

    public static class MeshGenericFix
    {
        public static Renderer SetMeshRendererPatch(GameObject gameObject, IConnector obj)
        {
            if (obj is MeshRendererConnector)
                return gameObject.AddComponent<UnityEngine.MeshRenderer>();
            return gameObject.AddComponent<UnityEngine.SkinnedMeshRenderer>();
        }
    }
    [HarmonyPatch(typeof(SkinnedMeshRendererConnector))]
    public static class SkinnedMeshRendererConnectorPatchA
    {
        [HarmonyPatch("OnUpdateRenderer")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ApplyChangesTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {
            //remove WasChanged set to prevent thread errors
            var codes = new List<CodeInstruction>(instructions);
            codes.Reverse();
            for (var a = 0; a < 2; a++)
            {
                for (var i = 0; i < codes.Count; i++)
                    if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString().Contains("set_WasChanged"))
                    {
                        for (var h = 0; h < 5; h++)
                        {
                            codes[i+h].opcode = OpCodes.Nop;
                            codes[i+h].operand = null;
                        }
                        break;
                    }
            }
            codes.Reverse();
            //replace buggy blendshape code
            //&& i.operand.ToString().Contains("get_Owner")
            var index = codes.LastIndexOf(codes.Last(i => i.opcode == OpCodes.Ldloc_2));
            codes[index].operand = typeof(SkinnedMeshRendererConnectorPatchA).GetMethod("DoBlendShapes");
            var index2 = codes.LastIndexOf(codes.Last(i => i.opcode == OpCodes.Call && i.operand.ToString().Contains("SendBoundsUpdated"))) - 1;
            for (var i = index + 1; i < index2; i++)
            {
                codes[i].opcode = OpCodes.Nop;
                codes[i].operand = null;
            }
            
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    if (codes[i].opcode != OpCodes.Ldloc_S ||
            //        !codes[i].operand.ToString().Contains("14")) continue;
            //    codes[i].opcode = OpCodes.Ldarg_0;
            //    codes[i].operand = null;
            //    codes[i + 1].operand = typeof(SkinnedMeshRendererConnectorPatchA).GetMethod("GetBlendShapeCount");
            //   
            //    var insertCodes = new CodeInstruction[]
            //    {
            //        new CodeInstruction(OpCodes.Ldarg_0),
            //        new CodeInstruction(OpCodes.Callvirt,
            //            typeof(SkinnedMeshRendererConnectorPatchA).GetMethod("GetBlendShapeCount")),
            //        new CodeInstruction(OpCodes.Stloc_S, (byte)20)
            //    };
            //    codes.InsertRange(i, insertCodes);
            //    
            //
            //    break;
            //}
            
            Thundaga.Msg("patched SkinnedMeshRendererConnector");
            return codes;
        }

        public static int GetBlendShapeCount(ref SkinnedMeshRendererConnector __instance) => __instance.MeshRenderer.sharedMesh.blendShapeCount;
        public static bool DoBlendShapes(SkinnedMeshRendererConnector instance)
        {
            if (instance == null) return false ;
            var renderer = instance.MeshRenderer;
            if (renderer == null) return false;
            var mesh = renderer.sharedMesh;
            if (mesh == null) return false;
            var count = mesh.blendShapeCount;
            var weights = instance.Owner.BlendShapeWeights.ToList();
            var weightsCount = weights.Count;
            for (var i = 0; i < count; i++)
            {
                try
                {
                    renderer.SetBlendShapeWeight(i, weightsCount > i ? weights[i] : 0);
                }
                catch (Exception)
                {
                    break;
                }
            }
            return false;
        }
    }

    //Patching generics is a pain, so we patch skinned and normal mesh renderers. this is the skinned mesh renderer patch.
    [HarmonyPatch(typeof(MeshRendererConnectorBase<SkinnedMeshRenderer, UnityEngine.SkinnedMeshRenderer>))]
    public static class SkinnedMeshRendererConnectorPatchB
    {
        [HarmonyPatch("ApplyChanges")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OnUpdateRendererTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {

            Thundaga.Msg("patching MeshRendererConnectorBase for SkinnedMeshRendererConnector implementation");

            return transpilerCodeMeshConnectorGeneric.ApplyChangesTranspilerGeneric(instructions);
        }
    }*/
    [HarmonyPatch]
    public static class MeshConnectorPatch
    {
        [HarmonyPatch(typeof(MeshConnector), "Upload")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UploadTranspiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate();
        
        [HarmonyPatch(typeof(MeshConnector), "Destroy")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DestroyTranspiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate();
    }
    [HarmonyPatch]
    public static class TextureConnectorPatch
    {
        [HarmonyPatch(typeof(TextureConnector), "Destroy")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DestroyTranspiler(
            IEnumerable<CodeInstruction> instructions)
        { return instructions.RemoveDestroyImmediate().RemoveDestroyImmediate().RemoveDestroyImmediate(); }
            
    }
    [HarmonyPatch(typeof(MaterialConnector))]
    public static class MaterialConnectorPatch
    {
        [HarmonyPatch("CleanupMaterial")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CleanupMaterialTranspiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate();
    }
    [HarmonyPatch(typeof(CubemapConnector))]
    public static class CubemapConnectorPatch
    {
        [HarmonyPatch("Destroy")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DestroyTranspiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate();
    }
    [HarmonyPatch(typeof(HeadOutput))]
    public static class HeadOutputPatch
    {
        public static bool JitterFix = true;
        public static float3 GlobalPosition { get; set; }
        public static float3 ViewPosition { get; set; }
        public static floatQ GlobalRotation { get; set; }
        public static floatQ ViewRotation { get; set; }

        private static MethodInfo _globalPosition =
            typeof(HeadOutputPatch).GetProperty("GlobalPosition").GetGetMethod();
        private static MethodInfo _viewPosition = 
            typeof(HeadOutputPatch).GetProperty("ViewPosition").GetGetMethod();
        private static MethodInfo _globalRotation =
            typeof(HeadOutputPatch).GetProperty("GlobalRotation").GetGetMethod();
        private static MethodInfo _viewRotation = 
            typeof(HeadOutputPatch).GetProperty("ViewRotation").GetGetMethod();
        [HarmonyPatch("UpdatePositioning")]
        [HarmonyTranspiler]
        public static List<CodeInstruction> UpdatePositioningTranspiler(this IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            
            codes[0].opcode = OpCodes.Nop;
            codes[1].operand = _globalPosition;
            codes[3].opcode = OpCodes.Nop;
            codes[4].operand = _globalRotation;
            var index = codes.IndexOf(codes.First(i =>
                i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("get_LocalUserViewPosition")));
            codes[index - 1].opcode = OpCodes.Nop;
            codes[index].operand = _viewPosition;
            codes[index + 3].opcode = OpCodes.Nop;
            codes[index + 4].operand = _viewRotation;
            return codes;
        }
    }
    [HarmonyPatch(typeof(MouseDriver))]
    public static class MouseDriverPatch
    {
        public static float2 NewDirectDelta = float2.Zero;
        public static float2 GetDelta()
        {
            var delta = NewDirectDelta;
            NewDirectDelta = float2.Zero;
            return delta;
        }

        [HarmonyPatch("UpdateMouse")]
        [HarmonyTranspiler]
        public static List<CodeInstruction> UpdateMouseTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var method = typeof(MouseDriverPatch).GetMethod("GetDelta", AccessTools.all);
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode != OpCodes.Callvirt || !code.operand.ToString().Contains("get_deltaTime")) continue;
                codes[i - 1].opcode = OpCodes.Nop;
                codes[i].opcode = OpCodes.Call;
                codes[i].operand = method;
                codes[i + 1].opcode = OpCodes.Nop;
                codes[i + 1].operand = null;
                codes[i + 2].opcode = OpCodes.Nop;
                codes[i + 2].operand = null;
                break;
            }
            return codes;
        }
    }

    //FIXED I THINK - 989onan
    public static class DestroyImmediateRemover
    {
        public static IEnumerable<CodeInstruction> OnReadyTranspiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate(false).RemoveDestroyImmediate().RemoveDestroyImmediate();


        [HarmonyPatch(typeof(RenderTextureConnector), "Unload")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler1(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate();

        [HarmonyPatch(typeof(TextureConnector), "SetTextureFormatDX11Native", typeof(TextureConnector.TextureFormatData))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> TranspilerTwice(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate(false).RemoveDestroyImmediate(false);

        [HarmonyPatch(typeof(TextureConnector), "GenerateUnityTextureFromOpenGL", typeof(TextureConnector.TextureFormatData))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> TranspilerTwice2(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate(false).RemoveDestroyImmediate(false);
        public static List<CodeInstruction> RemoveDestroyImmediate(this IEnumerable<CodeInstruction> instructions, bool hasOperand = true)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode != OpCodes.Call || !code.operand.ToString().Contains("DestroyImmediate")) continue;
                var newMethod = typeof(UnityEngine.Object).GetMethod("Destroy", new[] {typeof(UnityEngine.Object)});
                code.operand = newMethod;
                if (hasOperand) codes[i-1].opcode = OpCodes.Nop;
                break;
            }
            return codes;
        }
    }
}