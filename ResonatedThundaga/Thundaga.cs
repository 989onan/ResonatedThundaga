using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using Elements.Core;
using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using Thundaga.Packets;
using UnityFrooxEngineRunner;

namespace Thundaga
{
    public class Thundaga : ResoniteMod
    {
        public override string Name => "ResonatedThundaga";
        public override string Author => "Fro Zen";
        public override string Version => "2.0.0";

        private static bool _first_trigger = false;
        
        [AutoRegisterConfigKey]
        //refresh connectors
        public readonly ModConfigurationKey<bool> Refresh = new ModConfigurationKey<bool>("refresh",
            "Refresh Connectors (toggle to refresh)", () => false);
        //when a world has updated this many ticks, refresh connectors automatically, if stuff fails to load increase this
        [AutoRegisterConfigKey]
        public readonly ModConfigurationKey<int> AutoRefreshTick = new ModConfigurationKey<int>("refreshtick",
            "Auto-Refresh World Tick (raise if stuff doesn't load)", () => 300);
        //after this many update cycles, force auto refresh all "local" slots
        [AutoRegisterConfigKey]
        public readonly ModConfigurationKey<int> AutoRefreshLocalTick = new ModConfigurationKey<int>("refreshlocaltick",
            "Auto-Refresh UIX Ticks (lower if UIX breaks often, raise if errors, -1 to disable)", () => 1800);
        //thread priority the neos thread gets spun up with
        [AutoRegisterConfigKey]
        public readonly ModConfigurationKey<ThreadPriority> ResoniteThreadPriority = new ModConfigurationKey<ThreadPriority>("threadpriority",
            "Resonite Thread Priority (requires restart)", () => ThreadPriority.Normal);
        //target update rate for neos thread
        [AutoRegisterConfigKey]
        public readonly ModConfigurationKey<float> UpdateRate = new ModConfigurationKey<float>("updaterate",
            "Resonite Thread Target Update Rate (similar to framerate, requires restart)", () => 60);

        private void OnConfigurationChanged(ConfigurationChangedEvent @event)
        {
            var config = GetConfiguration();
            if (@event.Key == Refresh)
                FrooxEngineRunnerPatch.ShouldRefreshAllConnectors = true;
            else if (@event.Key == AutoRefreshTick) WorldPatch.AutoRefreshTick = config.GetValue(AutoRefreshTick);
            else if (@event.Key == AutoRefreshLocalTick)
                FrooxEngineRunnerPatch.AutoLocalRefreshTick = config.GetValue(AutoRefreshLocalTick);
        }

        public override void OnEngineInit()
        {
            
            ModConfiguration.OnAnyConfigurationChanged += OnConfigurationChanged;
            var harmony = new Harmony("ResonatedThundaga");
            var config = GetConfiguration();
            WorldPatch.AutoRefreshTick = config.GetValue(AutoRefreshTick);
            UpdateLoop.TickRate = config.GetValue(UpdateRate);
            FrooxEngineRunnerPatch.ResoniteThreadPriority = config.GetValue(ResoniteThreadPriority);
            FrooxEngineRunnerPatch.AutoLocalRefreshTick = config.GetValue(AutoRefreshLocalTick);
            
            //string logoClass = null;


            //we're not going to worry about this, since there is like... one destroy unity object in there.
            //it might be an issue. The class is FrooxEngineRunner and the method's name rn is InitializeHeadOutputs(LaunchOptions options) and does UnityEngine.Object.Destroy(OverlayCamera.gameObject);
            //it'll be fine.
            // I hope
            // - @989onan

            //[HarmonyPatch("Initialize")]

            //
            /*switch (Engine.Current.Platform)
            {
                case Platform.Windows:
                    //viseme analyzer?
                    logoClass = "<>c__DisplayClass39_0";
                    break;
                case Platform.Linux:
                    logoClass = "<>c__DisplayClass38_0";
                    break;
                case Platform.Android:
                    Msg("Android not yet supported, may crash on startup!");
                    //TODO: figure out what the class for this platform is
                    break;
            }
            
            if (logoClass != null)
            {
                //the startup logo
                var destroy4 = AccessTools.AllTypes()
                    .First(i => i.Name.Contains(logoClass) &&
                                i.DeclaringType == typeof())
                    .GetMethod("<Start>b__6", AccessTools.all);
                var transpilerLogo = new HarmonyMethod(typeof(DestroyImmediateRemover).GetMethod(nameof(DestroyImmediateRemover.OnReadyTranspiler)));
                harmony.Patch(destroy4, transpiler: transpilerLogo);
               */

            harmony.PatchAll();
            Msg("Patched methods");
        }



        public static void Logmessage(string message){
            Msg(message);
        }
    }
    public interface IConnectorPacket
    {
        void ApplyChange();
    }

    public class ConnectorPacket<T> : IConnectorPacket where T : IConnector
    {
        protected T _connector;
        public virtual void ApplyChange()
        {
        }
    }

    public static class PacketExtensions
    {
        public static SlotConnectorPacket GetPacket(this SlotConnector connector) => new SlotConnectorPacket(connector);
        public static SlotConnectorDestroyPacket GetDestroyPacket(this SlotConnector connector, bool destroyingWorld) =>
            new SlotConnectorDestroyPacket(connector, destroyingWorld);
    }

    public static class PacketManager
    {
        public static List<IConnectorPacket> ResonitePacketQueue = new List<IConnectorPacket>();
        public static List<IConnectorPacket> ResoniteHighPriorityPacketQueue = new List<IConnectorPacket>();
        public static List<IConnectorPacket> IntermittentPacketQueue = new List<IConnectorPacket>();
        public static List<Action> AssetTaskQueue = new List<Action>();

        public static void Enqueue(IConnectorPacket packet) => ResonitePacketQueue.Add(packet);
        public static void EnqueueHigh(IConnectorPacket packet) => ResoniteHighPriorityPacketQueue.Add(packet);
        public static void FinishResoniteQueue()
        {
            lock (IntermittentPacketQueue)
            {
                IntermittentPacketQueue.AddRange(ResoniteHighPriorityPacketQueue);
                IntermittentPacketQueue.AddRange(ResonitePacketQueue);
                IntermittentPacketQueue.Add(new HeadsetPositionPacket());
                ResonitePacketQueue.Clear();
                ResoniteHighPriorityPacketQueue.Clear();
            }
        }
        public static List<IConnectorPacket> GetQueuedPackets()
        {
            lock (IntermittentPacketQueue)
            {
                var packets = new List<IConnectorPacket>(IntermittentPacketQueue);
                IntermittentPacketQueue.Clear();
                return packets;
            }
        }
        public static List<Action> GetQueuedAssetTasks()
        {
            lock (AssetTaskQueue)
            {
                var packets = new List<Action>(AssetTaskQueue);
                AssetTaskQueue.Clear();
                return packets;
            }
        }
    }
    [HarmonyPatch]
    public static class ImplementableComponentPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ImplementableComponent<IConnector>), "InternalUpdateConnector")]
        public static bool InternalUpdateConnector(ImplementableComponent<IConnector> __instance)
        {
            PacketManager.Enqueue(new GenericComponentPacket(__instance.Connector));
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ImplementableComponent<IConnector>), "InternalRunStartup")]
        public static bool InternalRunStartup(ImplementableComponent<IConnector> __instance)
        {
            __instance.InternalRunStartup();
            PacketManager.Enqueue(new GenericComponentInitializePacket(__instance.Connector, __instance));
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ImplementableComponent<IConnector>), "DisposeConnector")]
        public static bool DisposeConnector(ImplementableComponent<IConnector> __instance) {
            if (__instance.Connector == null){
                var destroyed = __instance.World.IsDisposed;
                PacketManager.Enqueue(new GenericComponentDestroyPacket(__instance.Connector, destroyed));
                __instance.Connector?.RemoveOwner();
                set_Connector(__instance, null);
            }
            return false;
        }
        //[HarmonyReversePatch]
        //[HarmonyPatch(typeof(ImplementableComponent<IConnector>), "set_Connector")]
        public static void set_Connector(ImplementableComponent<IConnector> instance, IConnector connector)
        {
            throw new NotImplementedException();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ImplementableComponent<IConnector>), "InitializeConnector")]
        public static void InitializeConnector(ImplementableComponent<IConnector> instance)
        {
            throw new NotImplementedException();
        }
            
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ImplementableComponent<IConnector>), "InitializeConnector")]
        public static IEnumerable<CodeInstruction> InitializeConnectorTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode != OpCodes.Box) continue;
                for (var h = 0; h < 5; h++)
                {
                    codes[i + h].opcode = OpCodes.Nop;
                    codes[i + h].operand = null;
                }

                return codes;
            }
            return codes;
        }
    }
    [HarmonyPatch]
    public static class ExtraPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UnityAssetIntegrator), "ProcessQueue", typeof(double))]
        public static bool ProcessQueue(UnityAssetIntegrator __instance, ref int __result,
            ref SpinQueue<Action> ___taskQueue, double maxMilliseconds)
        {
            lock (PacketManager.AssetTaskQueue)
                while (___taskQueue.TryDequeue(out var val))
                    PacketManager.AssetTaskQueue.Add(val);
            __result = 0;
            return false;
        }
    }
}