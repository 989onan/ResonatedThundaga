using Elements.Core;
using Elements.Assets;
using FrooxEngine;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityFrooxEngineRunner;

namespace Thundaga.Packets
{
    public class SlotConnectorPacket : ConnectorPacket<SlotConnector>
    {
        private bool _shouldUpdateParent;
        private bool? _active;
        private Vector3? _position;
        private Quaternion? _rotation;
        private Vector3? _scale;
        
        public SlotConnectorPacket(SlotConnector connector)
        {
            _connector = connector;
            if (_connector.Owner == null)
            {
                UniLog.Log("Found orphaned connector, attempting to reconnect...");
                foreach (var component in Engine.Current.WorldManager.Worlds.ToList().SelectMany(world =>
                             world.AllSlots.ToList().SelectMany(slot => slot.Components.ToList())))
                {
                    if (!(component is ImplementableComponent<IConnector> implementableComponent) ||
                        implementableComponent.Connector != _connector) continue;
                    _connector.AssignOwner(implementableComponent);
                    break;
                }
            }
            var owner = connector.Owner;
            var parent = owner.Parent;
            //_parentConnector = parent?.Connector;
            _shouldUpdateParent = parent?.Connector != SlotConnectorInfo.ParentConnector.GetValue(connector) && parent != null;
            if (owner.ActiveSelf_Field.GetWasChangedAndClear()) _active = owner.ActiveSelf_Field.Value;

            if (owner.Position_Field.GetWasChangedAndClear())
            {
                _position = owner.Position_Field.Value.ToUnity();
            }
            if (owner.Rotation_Field.GetWasChangedAndClear())
            {
                _rotation = owner.Rotation_Field.Value.ToUnity();
            }
            if (owner.Scale_Field.GetWasChangedAndClear()) 
            {
                _scale = owner.Scale_Field.Value.ToUnity();
            }
        }
        public override void ApplyChange()
        {
            if (_connector.Owner?.Parent != null && _connector.Owner.Parent.Connector == null)
            {
                UniLog.Log("Slot connector's parent's connector does not exist, waiting for next valid update...");
                PacketManager.Enqueue(_connector.GetPacket());
            }
            if (_connector.GeneratedGameObject == null) return;
            if (_shouldUpdateParent) SlotConnectorPatches.UpdateParent(_connector);
            SlotConnectorPatches.UpdateLayer(_connector);
            UpdateData();
        }
        private void UpdateData()
        {
            if (_active.HasValue) _connector.GeneratedGameObject.SetActive(_active.Value);
            var transform = (UnityEngine.Transform)SlotConnectorInfo.Transform.GetValue(_connector);
            if (_position.HasValue) transform.localPosition = _position.Value;
            if (_rotation.HasValue) transform.localRotation = _rotation.Value;
            if (_scale.HasValue) transform.localScale = _scale.Value;
        }
    }
    public class SlotConnectorDestroyPacket : IConnectorPacket
    {
        private SlotConnector _connector;
        private bool _destroyingWorld;
        
        public SlotConnectorDestroyPacket(SlotConnector connector, bool destroyingWorld)
        {
            _connector = connector;
            _destroyingWorld = destroyingWorld;
        }
        public void ApplyChange() => SlotConnectorPatches.DestroyOriginal(_connector, _destroyingWorld);
    }
    public static class SlotConnectorInfo
    {
        public static readonly FieldInfo ParentConnector;
        public static readonly FieldInfo Transform;
        static SlotConnectorInfo()
        {
            ParentConnector = typeof(SlotConnector).GetField("parentConnector", AccessTools.all);
            Transform = typeof(SlotConnector).GetField("_transform", AccessTools.all);
        }
    }
    [HarmonyPatch]
    public static class SlotConnectorPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SlotConnector), "ApplyChanges")]
        private static bool ApplyChanges(SlotConnector __instance)
        {
            PacketManager.Enqueue(__instance.GetPacket());
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SlotConnector), "Destroy")]
        private static bool Destroy(SlotConnector __instance, bool destroyingWorld)
        {
            PacketManager.Enqueue(__instance.GetDestroyPacket(destroyingWorld));
            return false;
        }


        [HarmonyReversePatch]
        [HarmonyPatch(typeof(SlotConnector), "UpdateLayer")]
        public static void UpdateLayer(SlotConnector instance) => 
            throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(SlotConnector), "UpdateParent")]
        public static void UpdateParent(SlotConnector instance) => 
            throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(SlotConnector), "Destroy")]
        public static void DestroyOriginal(SlotConnector instance, bool destroyingWorld) =>
            throw new NotImplementedException();
            
    }
    [HarmonyPatch(typeof(Slot))]
    public static class SlotPatches
    {

        [HarmonyReversePatch]
        [HarmonyPatch("set_Connector")]
        public static void set_Connector(Slot instance, ISlotConnector connector) =>
            throw new NotImplementedException();
        
    }
}