using Godot;
using StrikeLedger.Core;
using StrikeLedger.App;

public partial class Main
{
    // These use the exported shell and Godot's input state. They are software events,
    // not a claim about any connected controller's physical switches or scan codes.
    async Task UiCombatInputBoundaries()
    {
        UiStep("audit combat input boundaries");
        var priorSim=sim;var priorRecorder=recorder;var priorMode=mode;
        void Fresh()
        {
            sim=new Simulation(content,new MatchConfig{Fighter0="rook",Fighter1="vale",StageId=stageId,SessionId="native-input-regression"});
            sim.CommitPreparation(new([]),new([]));sim.BeginFight();recorder=null;mode="local";paused=false;inputGrace=0;Clear("fight");
        }
        for(int seat=0;seat<2;seat++)
        {
            int device=seat==0?UiPad0:UiPad1;var mapping=input.Mapping(device);
            for(int control=0;control<8;control++)
            {
                Fresh();await UiFrames(3);
                var button=(JoyButton)mapping[control];
                Input.ParseInputEvent(new InputEventJoypadButton{Device=device,ButtonIndex=button,Pressed=true});await UiFrames(3);
                Buttons expected=control<6?(Buttons)(1<<control):control==6?Buttons.LP|Buttons.MP:Buttons.LK|Buttons.MK;
                UiRequire(screen=="fight"&&!paused,$"P{seat+1} {GameSettings.PadBindingName(mapping[control])} does not open a menu in combat");
                UiRequire((lastInputs[seat].Held&expected)==expected&&sim!.Players[seat].ActionId.Length>0,$"P{seat+1} attack/chord {expected} reaches the production recognizer");
                Input.ParseInputEvent(new InputEventJoypadButton{Device=device,ButtonIndex=button,Pressed=false});await UiFrames(3);
            }
            var original=mapping.ToArray();input.BindPad(device,0,(int)JoyButton.B);Fresh();await UiFrames(3);
            Input.ParseInputEvent(new InputEventJoypadButton{Device=device,ButtonIndex=JoyButton.B,Pressed=true});await UiFrames(3);
            UiRequire(screen=="fight"&&!paused&&lastInputs[seat].Held.HasFlag(Buttons.LP)&&sim!.Players[seat].ActionId.EndsWith("lp",StringComparison.Ordinal),$"P{seat+1} rebound B starts LP without pausing");
            Input.ParseInputEvent(new InputEventJoypadButton{Device=device,ButtonIndex=JoyButton.B,Pressed=false});await UiFrames(3);
            settings.PadMappings[input.ProfileKey(device)]=original.ToArray();
            foreach(var axis in new[]{JoyAxis.TriggerLeft,JoyAxis.TriggerRight})
            {
                input.BindPad(device,0,axis==JoyAxis.TriggerLeft?GameSettings.LeftTriggerBinding:GameSettings.RightTriggerBinding);Fresh();await UiFrames(3);
                foreach(var point in new[]{(.59f,false),(.61f,true),(.5f,true),(.39f,false)})
                {
                    Input.ParseInputEvent(new InputEventJoypadMotion{Device=device,Axis=axis,AxisValue=point.Item1});await UiFrames(3);
                    UiRequire(input.Sample(seat,sim!.Tick).Held.HasFlag(Buttons.LP)==point.Item2,$"P{seat+1} {axis} hysteresis at {point.Item1}");
                }
                Input.ParseInputEvent(new InputEventJoypadMotion{Device=device,Axis=axis,AxisValue=0});await UiFrames(2);
                settings.PadMappings[input.ProfileKey(device)]=original.ToArray();
            }
        }
        var oldKeys=settings.Keys.ToArray();input.Assign(0,-1);settings.Keys=GameSettings.DefaultKeys();settings.BindKey(4,(long)Key.Q);
        foreach(string layout in new[]{"Physical","Logical"})
        {
            settings.KeyboardLayoutMode=layout;Fresh();await UiFrames(3);
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.A,Keycode=Key.Q,Pressed=true});await UiFrames(3);
            var sample=input.Sample(0,sim!.Tick);
            UiRequire(layout=="Physical"?sample.Direction==4&&!sample.Held.HasFlag(Buttons.LP):sample.Direction==5&&sample.Held.HasFlag(Buttons.LP),$"{layout} keyboard mode uses one source for an alternate-layout event");
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.A,Keycode=Key.Q,Pressed=false});await UiFrames(3);
        }
        settings.KeyboardLayoutMode="Physical";Fresh();await UiFrames(3);
        Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.None,Keycode=Key.Q,Pressed=true});await UiFrames(3);
        UiRequire(input.Sample(0,sim!.Tick).Held.HasFlag(Buttons.LP),"Missing scan code uses the explicit accessible logical fallback");
        Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.None,Keycode=Key.Q,Pressed=false});await UiFrames(3);
        UiRequire(!input.Sample(0,sim!.Tick).Held.HasFlag(Buttons.LP),"Accessible fallback clears on release");
        settings.Keys=oldKeys;input.Assign(0,UiPad0);
        Fresh();await UiFrames(3);await UiTap(UiPad0,JoyButton.Start);UiRequire(screen=="pause"&&paused,"Start remains the dedicated combat pause");
        await UiTap(UiPad0,JoyButton.B);UiRequire(screen=="fight"&&!paused,"B remains menu Cancel and resumes a paused game");
        Input.ParseInputEvent(new InputEventKey{Keycode=Key.Escape,PhysicalKeycode=Key.Escape,Pressed=true});await UiFrames(3);
        UiRequire(screen=="pause"&&paused,"Escape remains the dedicated keyboard pause");
        Input.ParseInputEvent(new InputEventKey{Keycode=Key.Escape,PhysicalKeycode=Key.Escape,Pressed=false});await UiFrames(2);
        sim=priorSim;recorder=priorRecorder;mode=priorMode;paused=false;Clear("fight");UpdateArena();
    }
}
