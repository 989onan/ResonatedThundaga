﻿using HarmonyLib;
using NeosModLoader;
using System.Linq;
using System.Reflection;
using FrooxEngine;
using UnityNeos;

namespace Thundaga
{
    public class Thundaga : NeosMod
    {
        public override string Name => "Thundaga";
        public override string Author => "Fro Zen";
        public override string Version => "1.0.0";

        private Assembly _unityNeos;
        private static bool _startedNewUpdateLoop = false;

        private static bool _first_trigger = false;

        public override void OnEngineInit()
        {
            var harmony = new Harmony("Thundaga");
            _unityNeos = Assembly.GetAssembly(typeof(SkinnedMeshRendererConnector));
            Msg(_unityNeos.FullName);
            var needsChecked = _unityNeos.GetTypes().ToList();
            var methodNames = new[]
            {
                "Update", "OnPreCull", "OnWillRenderObject", "OnBecameVisible", "OnBecameInvisible", "OnPreRender",
                "OnRenderObject", "OnPostRender", "OnRenderImage", "ApplyChanges", "Destroy"
            };
            foreach (var n in methodNames)
            {
                Msg($"{n}:");
                var valid = needsChecked.Where(i => i.GetMethod(n) != null);
                foreach (var v in valid)
                {
                    Msg(v.FullName);
                }
            }
            harmony.PatchAll();
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
}