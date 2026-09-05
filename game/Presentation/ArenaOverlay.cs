using Godot;
namespace StrikeLedger.Presentation;
public partial class ArenaOverlay : Node2D
{
    public ArenaView? Arena {get;set;}
    public override void _Ready()=>TextureFilter=TextureFilterEnum.Nearest;
    public override void _Draw()=>Arena?.DrawOverlay(this);
}
