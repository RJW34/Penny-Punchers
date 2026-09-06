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
            if(e is InputEventKey key&&key.Pressed&&!key.Echo&&remapDevice==-1){captured=true;valid=settings.BindKey(remapIndex,(long)input.CaptureKey(key));if(valid)settings.Save();}
            else if(e is InputEventJoypadButton button&&button.Pressed&&button.Device==remapDevice){captured=true;valid=input.BindPad(button.Device,remapIndex,(int)button.ButtonIndex);}
            else if(e is InputEventJoypadMotion axis&&axis.Device==remapDevice&&axis.AxisValue>=.6f&&axis.Axis is JoyAxis.TriggerLeft or JoyAxis.TriggerRight){captured=true;valid=input.BindPad(axis.Device,remapIndex,axis.Axis==JoyAxis.TriggerLeft?GameSettings.LeftTriggerBinding:GameSettings.RightTriggerBinding);}
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
            var control=Button($"PAD {names[i+4]} / {GameSettings.PadBindingName(button)}",55+(i%4)*294,451+(i/4)*51,279,()=>
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
        if(prepPopupSeat>=0&&ui.GetChildren().OfType<PopupMenu>().Any(p=>GodotObject.IsInstanceValid(p)&&!p.IsQueuedForDeletion()&&p.Visible))
        {
            int popupDevice=e is InputEventJoypadButton jb?Array.IndexOf(input.Devices,jb.Device):e is InputEventKey?Array.IndexOf(input.Devices,-1):prepPopupSeat;
            if(popupDevice==prepPopupSeat)return false;
            GetViewport().SetInputAsHandled();return true;
        }
        if(command==0)return false;GetViewport().SetInputAsHandled();
        if(seat<0||ready[seat])return true;var rows=PrepRows(seat);if(rows.Length==0)return true;
        int current=Array.IndexOf(rows,_preparationFocus[seat]);if(current<0)current=0;
        if(Math.Abs(command)==1)_preparationFocus[seat]=rows[(current+command+rows.Length)%rows.Length];
        else
        {
            int selected=_preparationFocus[seat];
            if(command==3&&purchaseButtons.TryGetValue((seat,selected),out var button))button.EmitSignal(Godot.Button.SignalName.Pressed);
            else if(selected<3)CycleDraft(seat,selected,command==-2?-1:1);
            else if(purchaseProductIds.TryGetValue((seat,selected),out string? id)&&id.Length>0)ToggleShopProduct(seat,id,false);
        }
        if(screen=="prep")UpdatePurchaseDetail(seat,false);arena.PlayCue("select");return true;
    }

    void RefreshPreparationFocus()
    {
        foreach(var entry in purchaseButtons)
        {
            var b=entry.Value;if(!GodotObject.IsInstanceValid(b)||b.IsQueuedForDeletion())continue;
            int seat=entry.Key.Seat,row=entry.Key.Row;
            bool active=row==_preparationFocus[seat]&&!ready[seat];
            b.AddThemeStyleboxOverride("normal",UiArtFrame(active?(seat==0?"active-p1":"active-p2"):"normal"));
        }
        UiRefreshPreparationSkin();
    }
}
