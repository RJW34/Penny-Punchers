using Godot;
using StrikeLedger.Presentation;

/// <summary>Explicit synthetic presentation fixtures, never combat or gameplay evidence.</summary>
public partial class Review : Node2D
{
    public override async void _Ready()
    {
        var arena=new ArenaView();AddChild(arena);arena.SetVolumes(0,0,0);
        var overlay=new CanvasLayer();AddChild(overlay);
        var heading=new Label{Position=new Vector2(32,25),Text="STRIKE / LEDGER     •     PRESENTATION FIXTURE"};
        heading.AddThemeFontSizeOverride("font_size",27);overlay.AddChild(heading);
        var caption=new Label{Position=new Vector2(33,68)};caption.AddThemeFontSizeOverride("font_size",15);overlay.AddChild(caption);
        var footer=new Label{Position=new Vector2(33,636),Text="Synthetic render review — does not establish combat, input, networking or end-to-end gameplay."};
        footer.AddThemeFontSizeOverride("font_size",13);overlay.AddChild(footer);
        var tests=new(string Name,string State,string Move,int Frame,bool Grid)[]{
            ("01_idle","idle","",0,false),
            ("02_heavy_punch","attack","s_hp",8,false),
            ("03_heavy_kick","attack","s_hk",8,false),
            ("04_crouch_low","attack","c_mk",8,true),
            ("05_rising_special","attack","rise_h",8,false),
            ("06_guard","block","",0,false),
            ("07_dizzy","dizzy","",0,false),
            ("08_knockdown","knockdown","",0,false),
            ("09_mirror","idle","",0,false),
            ("10_air_attack","jump","j_hk",8,false),
            ("11_parry","parry","",0,false),
        };
        string output=ProjectSettings.GlobalizePath("res://output");System.IO.Directory.CreateDirectory(output);
        foreach(var test in tests)
        {
            var state=new ArenaRenderState{TrainingGrid=test.Grid,Fighters=[
                new(){Id="rook",X=305000,Facing=1,State=test.State,MoveId=test.Move,ActionFrame=test.Frame,Startup=7,Active=4,Recovery=15,Palette=0,Grounded=!test.Name.Contains("air"),Y=test.Name.Contains("air")?30000:0},
                new(){Id=test.Name.Contains("mirror")?"rook":"vale",X=463000,Facing=-1,State=test.State,MoveId=test.Move,ActionFrame=test.Frame,Startup=7,Active=4,Recovery=15,Palette=test.Name.Contains("mirror")?1:0,Grounded=!test.Name.Contains("air"),Y=test.Name.Contains("air")?30000:0}
            ]};
            arena.SetState(state);caption.Text=test.Name.Replace('_',' ').ToUpperInvariant()+"   /   "+test.Move.ToUpperInvariant();
            if(test.Name.Contains("parry")){arena.TriggerEffect("parry",340000,60000);arena.TriggerEffect("block",432000,60000);}
            for(int i=0;i<3;i++)await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            var png=GetViewport().GetTexture().GetImage();
            var err=png.SavePng(System.IO.Path.Combine(output,test.Name+".png"));
            if(err!=Error.Ok){GD.PushError("PRESENTATION_CAPTURE_FAILED: "+err);GetTree().Quit(1);return;}
            GD.Print("PRESENTATION_CAPTURE "+test.Name);
        }
        GetTree().Quit();
    }
}
