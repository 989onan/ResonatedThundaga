using FrooxEngine;

namespace Thundaga
{
    public class HeadsetPositionPacket : IConnectorPacket
    {
        public void ApplyChange()
        {
            Thundaga.Msg("World exists is");
            Thundaga.Msg(Engine.Current.WorldManager.FocusedWorld);
            var focusedWorld = Engine.Current.WorldManager.FocusedWorld;
            if (focusedWorld != null)
            {
                
                HeadOutputPatch.GlobalPosition = focusedWorld.LocalUserGlobalPosition;
                HeadOutputPatch.ViewPosition = focusedWorld.LocalUserViewPosition;
                HeadOutputPatch.GlobalRotation = focusedWorld.LocalUserGlobalRotation;
                HeadOutputPatch.ViewRotation = focusedWorld.LocalUserViewRotation;
            }
            else
            {
                HeadOutputPatch.GlobalPosition = new Elements.Core.float3(0, 0, 0);
                HeadOutputPatch.ViewPosition = new Elements.Core.float3(0, 2, 0);
                HeadOutputPatch.GlobalRotation = new Elements.Core.floatQ(0, 0, 0, 1);
                HeadOutputPatch.ViewRotation = new Elements.Core.floatQ(0, 0, 0, 1);

            }
            
        }
    }
}