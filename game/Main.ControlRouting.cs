using Godot;

public partial class Main
{
    private int _padRemapTarget=-1;
    private readonly int[] _preparationFocus=[0,0];

    /// <summary>Call before UI actions so a remapped B/Enter cannot also dismiss a screen.</summary>
    bool RouteControlInput(InputEvent e)
    {
        if(input==null||ui==null)return false;
        if(captureBinding)
        {
            if(e is InputEventKey cancel&&cancel.Pressed&&InputRouter.BindingKey(cancel)==Key.Escape||e is InputEventJoypadButton stop&&stop.Pressed&&stop.ButtonIndex==JoyButton.Start)
            {
                captureBinding=false;ControlsMenu();Toast("Binding unchanged.");GetViewport().SetInputAsHandled();return true;
            }
            bool captured=false,valid=false;
            if(e is InputEventKey key&&key.Pressed&&!key.Echo&&remapDevice==-1){captured=true;valid=settings.BindKey(remapIndex,(long)InputRouter.BindingKey(key));if(valid)settings.Save();}
            else if(e is InputEventJoypadButton button&&button.Pressed&&button.Device==remapDevice){captured=true;valid=input.BindPad(button.Device,remapIndex,(int)button.ButtonIndex);}
            if(captured)
            {
                if(valid){captureBinding=false;input.SuppressHeldButtons();ControlsMenu();Toast(settings.LastSaveError.Length>0?"Binding active; save failed: "+settings.LastSaveError:"Binding saved. Conflicting binding was swapped.");}
                else Toast("That control is reserved for navigation. Choose another, or Start / Esc to cancel.");
            }
            GetViewport().SetInputAsHandled();return true;
        }
        if(screen!="fight"&&e.IsActionPressed("ui_accept"))input.SuppressHeldButtons();
        return screen=="prep"&&RoutePreparationInput(e);
    }

    void BuildPadRemapControls(string[] names)
    {
        var pads=input.ConnectedDevices.ToList();
        if(pads.Count==0){Text("No controller detected. Keyboard input test is available below.",55,467,20,Muted);return;}
        if(!pads.Contains(_padRemapTarget))_padRemapTarget=pads[0];
        int target=_padRemapTarget;
        var selector=Button($"REMAPPING PAD {target+1} / {input.PadName(target)}  ·  select to switch device ↔",55,415,1161,()=>
        {
            int index=pads.IndexOf(_padRemapTarget);_padRemapTarget=pads[(index+1)%pads.Count];ControlsMenu();
        },false,15);selector.Size=new Vector2(1161,28);
        for(int i=0;i<8;i++)
        {
            int index=i;int button=input.Mapping(target)[i];
            var control=Button($"PAD {names[i+4]} / {(JoyButton)button}",55+(i%4)*294,451+(i/4)*51,279,()=>
            {
                remapIndex=index;remapDevice=target;captureBinding=true;Toast($"Pad {target+1}: press {names[index+4]} binding. Start cancels.");
            },false,16);control.Size=new Vector2(279,42);
        }
    }

    private bool RoutePreparationInput(InputEvent e)
    {
        int seat=-1,command=0;
        if(e is InputEventKey key&&key.Pressed&&!key.Echo)
        {
            seat=Array.IndexOf(input.Devices,-1);
            command=InputRouter.BindingKey(key) switch{Key.Up=>-1,Key.Down=>1,Key.Left=>-2,Key.Right=>2,Key.Enter=>3,Key.Space=>3,_=>0};
        }
        else if(e is InputEventJoypadButton pad&&pad.Pressed)
        {
            seat=Array.IndexOf(input.Devices,pad.Device);
            command=pad.ButtonIndex switch{JoyButton.DpadUp=>-1,JoyButton.DpadDown=>1,JoyButton.DpadLeft=>-2,JoyButton.DpadRight=>2,JoyButton.A=>3,_=>0};
        }
        if(command==0)return false;
        // Shared-screen drafts are visible, but each assigned input controls its own panel.
        GetViewport().SetInputAsHandled();
        if(seat<0||ready[seat])return true;
        if(Math.Abs(command)==1)_preparationFocus[seat]=(_preparationFocus[seat]+command+5)%5;
        else
        {
            int selected=_preparationFocus[seat];
            if(selected<3)
            {
                int delta=command==-2?-1:1;
                draft[seat][selected]=((draft[seat][selected]+1+delta+3)%3)-1;
                Preparation(false);
            }
            else if(selected==3){CycleAffordableReserve(seat,command==-2?-1:1);Preparation(false);}
            else if(command==3){input.SuppressHeldButtons();ReadyPlan(seat);}
        }
        if(screen=="prep")RefreshPreparationFocus();
        arena.PlayCue("select");return true;
    }

    void CycleAffordableReserve(int seat,int direction=1)
    {
        if(sim==null)return;int available=Math.Max(0,sim.Players[seat].Credits-DraftCost(seat));
        var choices=Enumerable.Range(0,available/300+1).Select(i=>i*300).ToList();
        if(choices[^1]!=available)choices.Add(available);
        int current=choices.IndexOf(floor[seat]);
        floor[seat]=choices[(current+(direction<0?-1:1)+choices.Count)%choices.Count];
    }

    void RefreshPreparationFocus()
    {
        foreach(var b in ui.GetChildren().OfType<Button>().Where(b=>!b.IsQueuedForDeletion()))
        {
            if(b.Position.Y<240)continue;
            int seat=b.Position.X>640?1:0;
            int row=b.Position.Y>=640?4:b.Position.Y>=500?3:(int)Math.Round((b.Position.Y-247)/87);
            if(row is <0 or >4)continue;
            bool active=row==_preparationFocus[seat]&&!ready[seat];
            b.AddThemeStyleboxOverride("normal",new StyleBoxFlat
            {
                BgColor=active?new Color(.11f,.17f,.19f,.99f):new Color(.065f,.1f,.13f,.96f),
                BorderColor=seat==0?Gold:Cyan,BorderWidthLeft=active?3:0,BorderWidthBottom=1,ContentMarginLeft=16,ContentMarginRight=12
            });
        }
        UiRefreshPreparationSkin();
    }
}
