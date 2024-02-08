using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Linq;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;
using UnityFrooxEngineRunner;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using ParticleSystem = FrooxEngine.ParticleSystem;
using ThreadPriority = System.Threading.ThreadPriority;
using Mono.Cecil.Cil;

namespace Thundaga
{
    public class UpdateLoop
    {
        public static bool Shutdown;
        public static float TickRate = 60;

        public static void Update()
        {
            var dateTime = DateTime.UtcNow;
            var tickRate = 1f / TickRate;
            while (!Shutdown)
            {
                Engine.Current.RunUpdateLoop();
                Thundaga.Msg("Engine running (check UpdateLoop for source of the spam)");
                PacketManager.FinishResoniteQueue();
                dateTime = dateTime.AddTicks((long) (10000000.0 * tickRate));
                var timeSpan = dateTime - DateTime.UtcNow;
                if (timeSpan.TotalMilliseconds > 0.0)
                {
                    var totalMilliseconds = (int) timeSpan.TotalMilliseconds;
                    if (totalMilliseconds > 0)
                        Thread.Sleep(totalMilliseconds);
                }
                else
                    dateTime = DateTime.UtcNow;
            }
        }
    }

    [HarmonyPatch]
    public static class FrooxEngineRunnerPatch
    {
        public static List<IConnector> Connectors = new List<IConnector>();
        private static bool _startedUpdating;
        private static int _lastDiagnosticReport = 1800;
        private static IntPtr? _renderThreadPointer;
        public static bool ShouldRefreshAllConnectors;
        private static readonly FieldInfo LocalSlots = typeof(World).GetField("_localSlots", AccessTools.all);
        public static ThreadPriority ResoniteThreadPriority = ThreadPriority.Normal;
        public static int AutoLocalRefreshTick = 1800;
        private static int _autoLocalRefreshTicks;

        private static void RefreshAllConnectors()
        {
            CheckForNullConnectors();
            var count = Engine.Current.WorldManager.Worlds.Sum(RefreshConnectorsForWorld);
            UniLog.Log($"Refreshed {count} components");
            //prevent updating removed connectors
            PacketManager.IntermittentPacketQueue.Clear();
            PacketManager.ResonitePacketQueue.Clear();
        }
        private static void RefreshAllLocalConnectors() =>
            Engine.Current.WorldManager.Worlds.Sum(RefreshLocalConnectorsForWorld);

        private static int RefreshConnectorsForWorld(World world) => RefreshConnectorsForWorld(world, false);
        private static int RefreshLocalConnectorsForWorld(World world) => RefreshConnectorsForWorld(world, true);
        private static int RefreshConnectorsForWorld(World world, bool localOnly)
        {
            //refresh world focus to fix overlapping worlds
            var focus = (World.WorldFocus)WorldConnectorPatch.Focus.GetValue(world);
            if (focus == World.WorldFocus.Focused || focus == World.WorldFocus.Background)
            {
                var connector = (WorldConnector) world.Connector;
                if (connector?.WorldRoot != null) connector.WorldRoot.SetActive(focus == World.WorldFocus.Focused);
            }
            //since the world state is constantly shifting we have to encapsulate them with try catch to prevent crashes
            var count = 0;
            try
            {
                if (!localOnly)
                {
                    var slots = world.AllSlots.ToList();
                    foreach (var slot in slots)
                    {
                        if (slot == null) continue;
                        var components = slot.Components.ToList();
                        foreach (var component in components)
                        {
                            if (!(component is ImplementableComponent<IConnector> implementable) || implementable is ParticleSystem) continue;
                            RefreshConnector(implementable);
                            count++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UniLog.Log(e);
            }
            try
            {
                var locals = ((List<Slot>) LocalSlots.GetValue(world)).ToList();
                foreach (var slot in locals)
                {
                    var components = slot.Components.ToList();
                    foreach (var component in components)
                    {
                        if (!(component is ImplementableComponent<IConnector> implementable) || implementable is ParticleSystem) continue;
                        RefreshConnector(implementable);
                        count++;
                    }
                }
            }
            catch (Exception e)
            {
                UniLog.Log(e);
            }
            return count;
        }

        private static void CheckForNullConnectors()
        {
            var toRemove = new List<IConnector>();
            foreach (var connector in Connectors)
            {
                if (connector.Owner != null && !connector.Owner.IsRemoved) continue;
                try
                {
                    connector.Destroy(false);
                    connector.RemoveOwner();
                    toRemove.Add(connector);
                }
                catch (Exception e)
                {
                    UniLog.Log(e);
                }
            }
            foreach (var remove in toRemove) Connectors.Remove(remove);
        }

        private static void RefreshConnector(ImplementableComponent<IConnector> implementable)
        {
            try
            {
                implementable.Connector.Destroy(false);
                ImplementableComponentPatches.InitializeConnector(implementable);
                implementable.Connector.AssignOwner(implementable);
                var con = implementable.Connector;
                con.Initialize();
                if (con is MeshRendererConnector mesh)
                {
                    mesh.meshWasChanged = true;


                }
                if (con is SkinnedMeshRendererConnector smesh)
                {
                    smesh.meshWasChanged = true;
                }
                con.ApplyChanges();
            }
            catch (Exception e)
            {
                UniLog.Log(e);
            }
        }

        public static void UpdateLoopReplacement(Engine __instance)
        {
            Thundaga.Msg("running asset queues");
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null) MouseDriverPatch.NewDirectDelta += mouse.delta.ReadValue().ToEngine();

            //___engine.RunUpdateLoop(6.0);
            Thundaga.Msg("running asset queues1");
            var packets = PacketManager.GetQueuedPackets();
            Thundaga.Msg("packets number: " + packets.Count.ToString());
            foreach (var packet in packets)
            {
                try
                {
                    Thundaga.Msg("Applying a packet change");
                    packet.ApplyChange();
                }
                catch (Exception e)
                {
                    UniLog.Error(e.ToString());
                }
            }
            Thundaga.Msg("running asset queues2");
            var assetTaskQueue = PacketManager.GetQueuedAssetTasks();
            foreach (var task in assetTaskQueue)
            {
                try
                {
                    task();
                }
                catch (Exception e)
                {
                    UniLog.Error(e.ToString());
                }
            }
            Thundaga.Msg("running asset queues3");
            var assetIntegrator = Engine.Current.AssetManager.Connector as UnityAssetIntegrator;
            if (!_renderThreadPointer.HasValue)
                _renderThreadPointer =
                    (IntPtr)assetIntegrator.renderThreadPointer;

            //if (((SpinQueue<>) AssetIntegratorPatch.RenderThreadQueue
            //       .GetValue(assetIntegrator))
            //    .Count > 0)
            Thundaga.Msg("running asset queues4");
            GL.IssuePluginEvent(_renderThreadPointer.Value, 0);
            assetIntegrator.ProcessQueue(2, false);

            if (ShouldRefreshAllConnectors)
            {
                ShouldRefreshAllConnectors = false;
                RefreshAllConnectors();
            }
            Thundaga.Msg("running asset queues5");
            //prevent people from crashing themselves by setting it to a really low number
            if (AutoLocalRefreshTick > 300)
            {
                _autoLocalRefreshTicks++;
                if (_autoLocalRefreshTicks > AutoLocalRefreshTick)
                {
                    _autoLocalRefreshTicks = 0;
                    RefreshAllLocalConnectors();
                }
            }
            Thundaga.Msg("finished asset queues");
        }




        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FrooxEngineRunner), "UpdateFrooxEngine")]
        private static IEnumerable<CodeInstruction> UpdateFrooxEngineTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (var i = 0; i<codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Callvirt || !codes[i].operand.ToString().Contains("RunUpdateLoop")) continue;

                var newMethod = typeof(FrooxEngineRunnerPatch).GetMethod("UpdateLoopReplacement");
                codes[i].operand = newMethod;
                
            }

            return codes;
        }





        [HarmonyPrefix]
        [HarmonyPatch(typeof(FrooxEngineRunner), "Update")]
        private static bool Update(FrooxEngineRunner __instance)
        {

            if (!__instance._frooxEngine.IsInitialized)
            {
                return true;
            }
            if (!_startedUpdating)
            {
                var updateLoop = new Thread(UpdateLoop.Update)
                {
                    Name = "Update Loop",
                    Priority = ResoniteThreadPriority,
                    IsBackground = false
                };
                updateLoop.Start();
                _startedUpdating = true;
            }
            else
            {
                //_lastDiagnosticReport--;
                //if (_lastDiagnosticReport <= 0)
                //{
                //
                //    _lastDiagnosticReport = 3600;
                //_refreshAllConnectors = true;
                //UniLog.Log("Reinitializing...");
                //UniLog.Log("SkinnedMeshRenderer: " + UnityEngine.Object.FindObjectsOfType<UnityEngine.SkinnedMeshRenderer>().Length);
                //}
                /*
                try
                {

                        
                    var focusedWorld = __instance._frooxEngine.WorldManager.FocusedWorld;
                    if (focusedWorld != null)
                    {
                        var num = __instance._frooxEngine.InputInterface.VR_Active;
                        var headOutput1 = num ? __instance.VROutput : __instance.ScreenOutput;
                        var headOutput2 = num ? __instance.ScreenOutput : __instance.VROutput;
                        if (headOutput2 != null &&
                            headOutput2.gameObject.activeSelf)
                            headOutput2.gameObject.SetActive(false);
                        if (!headOutput1.gameObject.activeSelf)
                            headOutput1.gameObject.SetActive(true);
                        headOutput1.UpdatePositioning(focusedWorld);
                        Vector3 vector3;
                        Quaternion quaternion;
                        if (focusedWorld.OverrideEarsPosition)
                        {
                            vector3 = focusedWorld.LocalUserEarsPosition.ToUnity();
                            quaternion = focusedWorld.LocalUserEarsRotation.ToUnity();
                        }
                        else
                        {
                            var cameraRoot = headOutput1.CameraRoot;
                            vector3 = cameraRoot.position;
                            quaternion = cameraRoot.rotation;
                        }

                        var transform = __instance._audioListener.transform;
                        transform.position = vector3;
                        transform.rotation = quaternion;
                        __instance._frooxEngine.WorldManager.GetWorlds(__instance._worlds);
                        var transform1 = headOutput1.transform;
                        foreach (var world in __instance._worlds)
                        {
                            if (world.Focus != World.WorldFocus.Overlay &&
                                world.Focus != World.WorldFocus.PrivateOverlay) continue;
                            var transform2 = ((WorldConnector)world.Connector).WorldRoot.transform;
                            var userGlobalPosition = world.LocalUserGlobalPosition;
                            var userGlobalRotation = world.LocalUserGlobalRotation;
                            transform2.transform.position = transform1.position - userGlobalPosition.ToUnity();
                            transform2.transform.rotation = transform1.rotation * userGlobalRotation.ToUnity();
                            transform2.transform.localScale = transform1.localScale;
                        }

                        __instance._worlds.Clear();
                    }

                    if (focusedWorld != __instance._lastFocusedWorld)
                    {
                        __instance.DynamicGI.UpdateDynamicGI();
                        __instance._lastFocusedWorld = focusedWorld;
                        __instance.StartCoroutine(nameof(DynamicGIManager.ScheduleDynamicGIUpdate), true);
                    }

                    var num1 = __instance._frooxEngine.InputInterface.VR_Active ? 1 : 0;
                    var lastVractive = __instance._lastVRactive;
                    var num2 = lastVractive.GetValueOrDefault() ? 1 : 0;
                    if (!(num1 == num2 & lastVractive.HasValue))
                    {
                        __instance._lastVRactive = __instance._frooxEngine.InputInterface.VR_Active;
                        if (__instance._lastVRactive.Value)
                        {
                            QualitySettings.lodBias = 3.8f;
                            QualitySettings.vSyncCount = 0;
                            QualitySettings.maxQueuedFrames = 0;
                        }
                        else
                        {
                            QualitySettings.lodBias = 2f;
                            QualitySettings.vSyncCount = 1;
                            QualitySettings.maxQueuedFrames = 2;
                        }
                    }
                    Thundaga.Msg("finished VR queues");
                }
                catch (Exception ex)
                {
                    UniLog.Error(ex.ToString());
                    Debug.LogError(ex.ToString());
                    __instance.Shutdown();
                }*/
                // }
                //this would be a memory leak...
                //but it doesn't seem to be used anywhere...
                /*
                while (__instance.messages.TryDequeue(out var val2))
                {
                    if (val2.error)
                        __instance.UnityError(val2.msg);
                    else
                        __instance.UnityLog(val2.msg);
                }
                */


            }
            return true;
        }
    }
    
    //Might use this later - @989onan
    /*
    [HarmonyPatch(typeof(SystemInfoConnector))]
    public static class SystemInfoConnectorPatch
    {
        public static PropertyInfo ExternalUpdateTime =
            typeof(SystemInfoConnector).GetProperty("ExternalUpdateTime", AccessTools.all);
        public static PropertyInfo RenderTime =
            typeof(SystemInfoConnector).GetProperty("RenderTime", AccessTools.all);
        public static PropertyInfo FPS =
            typeof(SystemInfoConnector).GetProperty("FPS", AccessTools.all);
        public static PropertyInfo ImmediateFPS =
            typeof(SystemInfoConnector).GetProperty("ImmediateFPS", AccessTools.all);
        [HarmonyReversePatch]
        [HarmonyPatch("ExternalUpdateTime", MethodType.Setter)]
        public static void set_ExternalUpdateTime(SystemInfoConnector instance, float value) =>
            throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch("RenderTime", MethodType.Setter)]
        public static void set_RenderTime(SystemInfoConnector instance, float value) =>
            throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch("FPS", MethodType.Setter)]
        public static void set_FPS(SystemInfoConnector instance, float value) =>
            throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch("ImmediateFPS", MethodType.Setter)]
        public static void set_ImmediateFPS(SystemInfoConnector instance, float value) =>
            throw new NotImplementedException();
    }*/
}