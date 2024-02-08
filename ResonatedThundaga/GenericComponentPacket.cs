using System;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using Thundaga.Packets;
using Thundaga;
using UnityFrooxEngineRunner;
using HarmonyLib;
using System.Reflection;

namespace Thundaga
{
    public class GenericComponentPacket : ConnectorPacket<IConnector>
    {
        public override void ApplyChange()
        {
            if (_connector?.Owner != null) _connector.ApplyChanges();
        }
        public GenericComponentPacket(IConnector connector, bool refresh = false)
        {
            _connector = connector;
            if (refresh) return;
            switch (connector)
            {
                //TODO: is this heavy on performance?
                case MeshRendererConnector meshConnector:
                {
                        var owner = meshConnector.Owner;
                        meshConnector.meshWasChanged = owner.Mesh.GetWasChangedAndClear();
                        owner.SortingOrder.GetWasChangedAndClear();
                        owner.ShadowCastMode.GetWasChangedAndClear();
                        owner.MotionVectorMode.GetWasChangedAndClear();
                        break;
                }
                case SkinnedMeshRendererConnector skinnedMeshRendererConnector:
                    var owner2 = skinnedMeshRendererConnector.Owner;
                    skinnedMeshRendererConnector.meshWasChanged = owner2.Mesh.GetWasChangedAndClear();
                    owner2.SortingOrder.GetWasChangedAndClear();
                    owner2.ShadowCastMode.GetWasChangedAndClear();
                    owner2.MotionVectorMode.GetWasChangedAndClear();
                    owner2.ProxyBoundsSource.GetWasChangedAndClear();
                    owner2.ExplicitLocalBounds.GetWasChangedAndClear();
                    break;
            }
        }
    }

    public class GenericComponentDestroyPacket : ConnectorPacket<IConnector>
    {
        private bool _destroyingWorld;
        public override void ApplyChange()
        {
            if (_connector == null) return;
            FrooxEngineRunnerPatch.Connectors.Remove(_connector);
            _connector.Destroy(_destroyingWorld);
            _connector.RemoveOwner();
        }
        public GenericComponentDestroyPacket(IConnector connector, bool destroyingWorld)
        {
            _connector = connector;
            _destroyingWorld = destroyingWorld;
        }
    }
    public class GenericComponentInitializePacket : ConnectorPacket<IConnector>
    {
        private readonly ImplementableComponent<IConnector> _initializing;
        public override void ApplyChange()
        {
            //this connector has likely been replaced by a refresh, ignore
            if (_connector == null || _initializing.Slot.IsDisposed || _initializing.Connector != _connector) return;
            if (!FrooxEngineRunnerPatch.Connectors.Contains(_connector)) FrooxEngineRunnerPatch.Connectors.Add(_connector);
            _connector.Initialize();
        }
        public GenericComponentInitializePacket(IConnector connector, ImplementableComponent<IConnector> owner = null)
        {
            _connector = connector;
            _initializing = owner;
        }
    }
}