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



    //
    /// <summary>
    /// Patching generics is a pain, so we patch skinned and normal mesh renderers. this is the mesh renderer patch.
    /// here we copied the code verbatim, and changed out this and base with __instance as would be done with harmony
    /// next, we patch out the methods that cause issues, like destroy immediates, adding components, and GetWasChangedAndClear().
    /// lastly we prevent the original method from running at any point.
    /// this is basically a transpiler but much worsely coded.
    /// </summary>

    [HarmonyPatch(typeof(MeshRendererConnectorBase<MeshRenderer, UnityEngine.MeshRenderer>))]
    public class MeshRendererConnectorPatch
    {
        [HarmonyPatch("ApplyChanges")]
        [HarmonyPrefix]
        public static bool ApplyChangesTranspiler(
            MeshRendererConnectorBase<MeshRenderer, UnityEngine.MeshRenderer> __instance)
        {
            Thundaga.Msg("pushing buffer for message");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("trying to run a mesh renderer connector patch");
            Thundaga.Msg("finished pushing buffer");
            if (!__instance.Owner.ShouldBeEnabled)
            {
                __instance.CleanupRenderer(false);
                return false ;
            }
            bool flag = false;
            if (__instance.MeshRenderer == null)
            {
                GameObject gameObject = new GameObject("");
                gameObject.transform.SetParent(__instance.attachedGameObject.transform, false);
                gameObject.layer = __instance.attachedGameObject.layer;
                if (__instance.UseMeshFilter)
                {
                    __instance.meshFilter = gameObject.AddComponent<MeshFilter>();
                }
                __instance.MeshRenderer = (UnityEngine.MeshRenderer)MeshGenericFix.SetMeshRendererPatch(gameObject, __instance);
                __instance.OnAttachRenderer();
                flag = true;
            }
            if (__instance.meshWasChanged)
            {
                UnityEngine.Mesh unity = __instance.Owner.Mesh.Asset.GetUnity();
                if (__instance.UseMeshFilter)
                {
                    __instance.meshFilter.sharedMesh = unity;
                }
                else
                {
                    __instance.AssignMesh(__instance.MeshRenderer, unity);
                }
            }
            bool flag2 = false;
            if (__instance.Owner.MaterialsChanged || __instance.meshWasChanged)
            {
                __instance.Owner.MaterialsChanged = false;
                flag2 = true;
                __instance.materialCount = 1;
                UnityEngine.Material material = __instance.Owner.IsLocalElement ? MaterialConnector.InvisibleMaterial : MaterialConnector.NullMaterial;
                if (__instance.Owner.Materials.Count > 1 || __instance.unityMaterials != null)
                {
                    __instance.unityMaterials = __instance.unityMaterials.EnsureExactSize(__instance.Owner.Materials.Count, false, true);
                    for (int i = 0; i < __instance.unityMaterials.Length; i++)
                    {
                        UnityEngine.Material[] array = __instance.unityMaterials;
                        int num = i;
                        IAssetProvider<FrooxEngine.Material> assetProvider = __instance.Owner.Materials[i];
                        array[num] = ((assetProvider != null) ? assetProvider.Asset.GetUnity(material) : null);
                    }
                    __instance.MeshRenderer.sharedMaterials = __instance.unityMaterials;
                    __instance.materialCount = __instance.unityMaterials.Length;
                }
                else if (__instance.Owner.Materials.Count == 1)
                {
                    Renderer renderer = __instance.MeshRenderer;
                    AssetRef<FrooxEngine.Material> material2 = __instance.Owner.Material;
                    renderer.sharedMaterial = ((material2 != null) ? material2.Asset.GetUnity(material) : null);
                }
                else
                {
                    __instance.MeshRenderer.sharedMaterial = material;
                }
            }
            if (__instance.Owner.MaterialPropertyBlocksChanged || flag2)
            {
                __instance.Owner.MaterialPropertyBlocksChanged = false;
                if (__instance.Owner.MaterialPropertyBlocks.Count > 0)
                {
                    for (int j = 0; j < __instance.materialCount; j++)
                    {
                        if (j < __instance.Owner.MaterialPropertyBlocks.Count)
                        {
                            Renderer renderer2 = __instance.MeshRenderer;
                            IAssetProvider<FrooxEngine.MaterialPropertyBlock> assetProvider2 = __instance.Owner.MaterialPropertyBlocks[j];
                            renderer2.SetPropertyBlock((assetProvider2 != null) ? assetProvider2.Asset.GetUnity() : null, j);
                        }
                        else
                        {
                            __instance.MeshRenderer.SetPropertyBlock(null, j);
                        }
                    }
                    __instance.usesMaterialPropertyBlocks = true;
                }
                else if (__instance.usesMaterialPropertyBlocks)
                {
                    for (int k = 0; k < __instance.materialCount; k++)
                    {
                        __instance.MeshRenderer.SetPropertyBlock(null, k);
                    }
                    __instance.usesMaterialPropertyBlocks = false;
                }
            }
            //prevent from running this from original method.
            /*
            bool enabled = __instance.Owner.Enabled;
            if (__instance.MeshRenderer.enabled != enabled)
            {
                __instance.MeshRenderer.enabled = enabled;
            }
            if (__instance.Owner.SortingOrder.GetWasChangedAndClear() || flag)
            {
                __instance.MeshRenderer.sortingOrder = __instance.Owner.SortingOrder.Value;
            }
            if (__instance.Owner.ShadowCastMode.GetWasChangedAndClear() || flag)
            {
                __instance.MeshRenderer.shadowCastingMode = __instance.Owner.ShadowCastMode.Value.ToUnity();
            }
            if (__instance.Owner.MotionVectorMode.GetWasChangedAndClear() || flag)
            {
                __instance.MeshRenderer.motionVectorGenerationMode = __instance.Owner.MotionVectorMode.Value.ToUnity();
            }*/
            __instance.OnUpdateRenderer(flag);
            return false;
        }
    }

    public static class transpilerCodeMeshConnectorGeneric{
        public static IEnumerable<CodeInstruction> ApplyChangesTranspilerGeneric(
            IEnumerable<CodeInstruction> instructions)
        {

            //remove GetWasChangedAndClear methods to prevent thread errors
            var codes = new List<CodeInstruction>(instructions);
            codes.Reverse();
            
            /*for (var i = 0; i < codes.Count; i++)
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
                    
            }*/


            //get rid of renderer enabled to prevent thread errors.
            /*for (var i = 0; i < codes.Count; i++){
                if (codes[i].opcode != OpCodes.Callvirt || !codes[i].operand.ToString().Contains("Renderer::set_Enabled")) continue;

                for(var h = 0; h< 5; h++)
                {
                    var code = codes[i + h];
                    code.opcode = OpCodes.Nop;
                    code.operand = null;

                }

            }*/

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

            /*var index = codes.IndexOf(codes.Where(i =>
                i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("AddComponent")).ToList()[1]);
            codes[index].operand = typeof(MeshGenericFix).GetMethod("SetMeshRendererPatch");
            codes[index].opcode = OpCodes.Call;
            codes.Insert(index, new CodeInstruction(OpCodes.Ldarg_0));*/
            Thundaga.Msg("Patched Mesh renderer connector.");

            Thundaga.Msg("Patch new codes");
            foreach (CodeInstruction code1 in codes)
            {
                Thundaga.Msg(code1.opcode.ToString());
                try
                {
                    Thundaga.Msg(code1.operand.ToString());
                }
                catch
                {
                    
                    Thundaga.Msg("failed to print operand");
                }

            }
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




    [HarmonyPatch]
    public static class SkinnedMeshRendererConnectorPatchA
    {
        [HarmonyPatch(typeof(SkinnedMeshRendererConnector), "OnUpdateRenderer", typeof(bool))]
        [HarmonyReversePatch]
        public static bool OnUpdateRendererPatch(SkinnedMeshRendererConnector __instance, bool instantiated)
        {
            SkinnedBounds skinnedBounds = __instance.Owner.BoundsComputeMethod.Value;
            if (skinnedBounds == SkinnedBounds.Static && __instance.Owner.Slot.ActiveUserRoot == __instance.Owner.LocalUserRoot)
            {
                skinnedBounds = SkinnedBounds.FastDisjointRootApproximate;
            }

            if (__instance.meshWasChanged || __instance._currentBoundsMethod != skinnedBounds || __instance.Owner.ProxyBoundsSource.WasChanged || __instance.Owner.ExplicitLocalBounds.WasChanged)
            {
                __instance.Owner.ProxyBoundsSource.WasChanged = false;
                __instance.Owner.ExplicitLocalBounds.WasChanged = false;
                if (skinnedBounds != 0 && skinnedBounds != SkinnedBounds.Proxy && skinnedBounds != SkinnedBounds.Explicit)
                {
                    if (__instance._boundsUpdater == null)
                    {
                        __instance.LocalBoundingBoxAvailable = false;
                        __instance._boundsUpdater = __instance.MeshRenderer.gameObject.AddComponent<SkinBoundsUpdater>();
                        __instance._boundsUpdater.connector = __instance;
                    }

                    __instance._boundsUpdater.boundsMethod = skinnedBounds;
                    __instance._boundsUpdater.boneMetadata = __instance.Owner.Mesh.Asset.BoneMetadata;
                    __instance._boundsUpdater.approximateBounds = __instance.Owner.Mesh.Asset.ApproximateBoneBounds;
                    __instance.MeshRenderer.updateWhenOffscreen = skinnedBounds == SkinnedBounds.SlowRealtimeAccurate;
                }
                else
                {
                    if (__instance._boundsUpdater != null)
                    {
                        __instance.LocalBoundingBoxAvailable = false;
                        __instance.MeshRenderer.updateWhenOffscreen = false;
                        __instance.CleanupBoundsUpdater();
                    }

                    //if (skinnedBounds == SkinnedBounds.Proxy)
                    //{
                    //    __instance.CleanupProxy();
                    //    __instance._proxySource = __instance.Owner.ProxyBoundsSource.Target?.SkinConnector as SkinnedMeshRendererConnector;
                    //    if (__instance._proxySource != null)
                    //    {
                    //        __instance._proxySource.BoundsUpdated += __instance.ProxyBoundsUpdated;
                    //        __instance.ProxyBoundsUpdated();
                    //    }
                    //}

                    if (skinnedBounds == SkinnedBounds.Explicit)
                    {
                        __instance.MeshRenderer.localBounds = __instance.Owner.ExplicitLocalBounds.Value.ToUnity();
                        __instance.LocalBoundingBoxAvailable = true;
                        __instance.SendBoundsUpdated();
                    }
                }

                __instance._currentBoundsMethod = skinnedBounds;
            }

            bool flag = __instance.meshWasChanged;
            bool bonesChanged = __instance.Owner.BonesChanged;
            bool blendShapeWeightsChanged = __instance.Owner.BlendShapeWeightsChanged;
            bool num = bonesChanged || flag;
            blendShapeWeightsChanged = blendShapeWeightsChanged || flag;
            if (num)
            {
                __instance.Owner.BonesChanged = false;
                int? num2 = __instance.Owner.Mesh.Asset?.Data?.BoneCount;
                int? num3 = __instance.Owner.Mesh.Asset?.Data?.BlendShapeCount;
                bool flag2 = num2 == 0 && num3 > 0;
                if (flag2)
                {
                    num2 = 1;
                }

                __instance.bones = __instance.bones.EnsureExactSize(num2.GetValueOrDefault());
                if (__instance.bones != null)
                {
                    if (flag2)
                    {
                        __instance.bones[0] = __instance.attachedGameObject.transform;
                    }
                    else
                    {
                        int num4 = MathX.Min(__instance.bones.Length, __instance.Owner.Bones.Count);
                        int num5 = 0;
                        for (int i = 0; i < num4; i++)
                        {
                            SlotConnector slotConnector = __instance.Owner.Bones[i]?.Connector as SlotConnector;
                            if (slotConnector != null)
                            {
                                __instance.bones[i] = slotConnector.ForceGetGameObject().transform;
                                num5++;
                            }
                        }
                    }
                }

                __instance.MeshRenderer.bones = __instance.bones;
                __instance.MeshRenderer.rootBone = (flag2 ? __instance.attachedGameObject.transform : (__instance.Owner.GetRootBone()?.Connector as SlotConnector)?.ForceGetGameObject().transform);
            }

            if (blendShapeWeightsChanged)
            {
                DoBlendShapes(__instance);
            }

            if (__instance._forceRecalcPerRender)
            {
                __instance.MeshRenderer.forceMatrixRecalculationPerRender = true;
            }

            __instance.SendBoundsUpdated();

            return false;
        }
        /*public static IEnumerable<CodeInstruction> ApplyChangesTranspiler(
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
        }*/

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
    public class SkinnedMeshRendererConnectorPatch
    {
        [HarmonyPatch("ApplyChanges")]
        [HarmonyPrefix]
        public static bool ApplyChanges(MeshRendererConnectorBase<MeshRenderer, UnityEngine.SkinnedMeshRenderer> __instance){
            Thundaga.Msg("pushing buffer for message");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("trying to run a Skinned mesh renderer connector patch");
            Thundaga.Msg("finished pushing buffer");
            if (!__instance.Owner.ShouldBeEnabled)
            {
                __instance.CleanupRenderer(false);
                return false;
            }
            bool flag = false;
            if (__instance.MeshRenderer == null)
            {
                GameObject gameObject = new GameObject("");
                gameObject.transform.SetParent(__instance.attachedGameObject.transform, false);
                gameObject.layer = __instance.attachedGameObject.layer;
                if (__instance.UseMeshFilter)
                {
                    __instance.meshFilter = gameObject.AddComponent<MeshFilter>();
                }
                __instance.MeshRenderer = (UnityEngine.SkinnedMeshRenderer)MeshGenericFix.SetMeshRendererPatch(gameObject, __instance);
                __instance.OnAttachRenderer();
                flag = true;
            }

            if (__instance.meshWasChanged)
            {
                UnityEngine.Mesh unity = __instance.Owner.Mesh.Asset.GetUnity();
                if (__instance.UseMeshFilter)
                {
                    __instance.meshFilter.sharedMesh = unity;
                }
                else
                {
                    __instance.AssignMesh(__instance.MeshRenderer, unity);
                }
            }
            bool flag2 = false;
            if (__instance.Owner.MaterialsChanged || __instance.meshWasChanged)
            {
                __instance.Owner.MaterialsChanged = false;
                flag2 = true;
                __instance.materialCount = 1;
                UnityEngine.Material material = __instance.Owner.IsLocalElement ? MaterialConnector.InvisibleMaterial : MaterialConnector.NullMaterial;
                if (__instance.Owner.Materials.Count > 1 || __instance.unityMaterials != null)
                {
                    __instance.unityMaterials = __instance.unityMaterials.EnsureExactSize(__instance.Owner.Materials.Count, false, true);
                    for (int i = 0; i < __instance.unityMaterials.Length; i++)
                    {
                        UnityEngine.Material[] array = __instance.unityMaterials;
                        int num = i;
                        IAssetProvider<FrooxEngine.Material> assetProvider = __instance.Owner.Materials[i];
                        array[num] = ((assetProvider != null) ? assetProvider.Asset.GetUnity(material) : null);
                    }
                    __instance.MeshRenderer.sharedMaterials = __instance.unityMaterials;
                    __instance.materialCount = __instance.unityMaterials.Length;
                }
                else if (__instance.Owner.Materials.Count == 1)
                {
                    Renderer renderer = __instance.MeshRenderer;
                    AssetRef<FrooxEngine.Material> material2 = __instance.Owner.Material;
                    renderer.sharedMaterial = ((material2 != null) ? material2.Asset.GetUnity(material) : null);
                }
                else
                {
                    __instance.MeshRenderer.sharedMaterial = material;
                }
            }
            if (__instance.Owner.MaterialPropertyBlocksChanged || flag2)
            {
                __instance.Owner.MaterialPropertyBlocksChanged = false;
                if (__instance.Owner.MaterialPropertyBlocks.Count > 0)
                {
                    for (int j = 0; j < __instance.materialCount; j++)
                    {
                        if (j < __instance.Owner.MaterialPropertyBlocks.Count)
                        {
                            Renderer renderer2 = __instance.MeshRenderer;
                            IAssetProvider<FrooxEngine.MaterialPropertyBlock> assetProvider2 = __instance.Owner.MaterialPropertyBlocks[j];
                            renderer2.SetPropertyBlock((assetProvider2 != null) ? assetProvider2.Asset.GetUnity() : null, j);
                        }
                        else
                        {
                            __instance.MeshRenderer.SetPropertyBlock(null, j);
                        }
                    }
                    __instance.usesMaterialPropertyBlocks = true;
                }
                else if (__instance.usesMaterialPropertyBlocks)
                {
                    for (int k = 0; k < __instance.materialCount; k++)
                    {
                        __instance.MeshRenderer.SetPropertyBlock(null, k);
                    }
                    __instance.usesMaterialPropertyBlocks = false;
                }
            }
            //prevent from running this from original method.
            /*
            bool enabled = __instance.Owner.Enabled;
            if (__instance.MeshRenderer.enabled != enabled)
            {
                __instance.MeshRenderer.enabled = enabled;
            }
            if (__instance.Owner.SortingOrder.GetWasChangedAndClear() || flag)
            {
                __instance.MeshRenderer.sortingOrder = __instance.Owner.SortingOrder.Value;
            }
            if (__instance.Owner.ShadowCastMode.GetWasChangedAndClear() || flag)
            {
                __instance.MeshRenderer.shadowCastingMode = __instance.Owner.ShadowCastMode.Value.ToUnity();
            }
            if (__instance.Owner.MotionVectorMode.GetWasChangedAndClear() || flag)
            {
                __instance.MeshRenderer.motionVectorGenerationMode = __instance.Owner.MotionVectorMode.Value.ToUnity();
            }*/
            __instance.OnUpdateRenderer(flag);
            return false;
        }
    }
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