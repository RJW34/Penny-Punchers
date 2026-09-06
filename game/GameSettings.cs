using Godot;
using System.Text.Json;
using System.Text.Json.Serialization;
using StrikeLedger.Core;

public sealed record SavedShopQuote(int Cost,string ContentHash);

public sealed class GameSettings
{
    public float Master {get;set;}=.75f;
    public float Music {get;set;}=.35f;
    public float Sfx {get;set;}=.8f;
    public float Deadzone {get;set;}=.35f;
    public bool ReducedFlashes {get;set;}
    public bool Shake {get;set;}=true;
    public bool Fullscreen {get;set;}
    public bool Vsync {get;set;}=true;
    public string KeyboardLayoutMode {get;set;}="Physical";
    public int Width {get;set;}=1280;
    public Dictionary<string,int[]> PadMappings {get;set;}=new();
    public Dictionary<string,string[]> BudgetPlans {get;set;}=new();
    public Dictionary<string,SavedShopQuote> BudgetPlanQuotes {get;set;}=new();
    public string[] SeatProfiles {get;set;}=["keyboard","auto"];
    public long[] Keys {get;set;}=DefaultKeys();
    [JsonIgnore] public string LastSaveError {get;private set;}="";
    [JsonIgnore] public bool PersistenceEnabled {get;set;}=true;
    public static string Path=>ProjectSettings.GlobalizePath("user://settings.json");
    public static long[] DefaultKeys()=>[(long)Key.A,(long)Key.D,(long)Key.W,(long)Key.S,(long)Key.U,(long)Key.I,(long)Key.O,(long)Key.J,(long)Key.K,(long)Key.L,(long)Key.P,(long)Key.Semicolon];
    public static int[] DefaultPad()=>[(int)JoyButton.X,(int)JoyButton.Y,(int)JoyButton.RightShoulder,(int)JoyButton.A,(int)JoyButton.B,(int)JoyButton.LeftShoulder,(int)JoyButton.LeftStick,(int)JoyButton.RightStick];

    public static GameSettings Load()
    {
        try
        {
            string source=Path;
            if(!System.IO.File.Exists(source))
            {
                string? parent=System.IO.Path.GetDirectoryName(OS.GetUserDataDir());
                if(parent!=null)
                {
                    string legacy=System.IO.Path.Combine(parent,"Strike Ledger","settings.json");
                    if(System.IO.File.Exists(legacy))source=legacy;
                }
            }
            var info=new System.IO.FileInfo(source);
            if(!info.Exists||info.Length>131072)return new();
            return (JsonSerializer.Deserialize<GameSettings>(System.IO.File.ReadAllText(source))??new()).Normalize();
        }
        catch{return new();}
    }
    /// <summary>Pure validation, also exercised outside the Godot process by the input checks.</summary>
    public GameSettings Normalize()
    {
        static float Clamp(float value,float min,float max,float fallback)=>float.IsFinite(value)?Math.Clamp(value,min,max):fallback;
        Master=Clamp(Master,0,1,.75f);Music=Clamp(Music,0,1,.35f);Sfx=Clamp(Sfx,0,1,.8f);Deadzone=Clamp(Deadzone,.1f,.8f,.35f);
        if(KeyboardLayoutMode is not("Physical" or "Logical"))KeyboardLayoutMode="Physical";
        if(Width is not(1280 or 1600 or 1920))Width=1280;
        Keys=NormalizeKeys(Keys);
        var maps=new Dictionary<string,int[]>(StringComparer.Ordinal);
        foreach(var entry in (PadMappings??new()).Take(64))if(!string.IsNullOrWhiteSpace(entry.Key)&&entry.Key.Length<=256)maps[entry.Key]=NormalizePad(entry.Value);
        PadMappings=maps;
        BudgetPlans=(BudgetPlans??new()).Where(e=>!string.IsNullOrWhiteSpace(e.Key)&&e.Key.Length<=64&&e.Value!=null&&e.Value.Length<=6&&e.Value.All(id=>!string.IsNullOrWhiteSpace(id)&&id.Length<=128)).Take(64).ToDictionary(e=>e.Key,e=>e.Value.ToArray());
        BudgetPlanQuotes=(BudgetPlanQuotes??new()).Where(e=>BudgetPlans.ContainsKey(e.Key)&&e.Value is {} q&&q.Cost is >=0 and <=1000000&&q.ContentHash!=null&&q.ContentHash.Length==64&&q.ContentHash.All(Uri.IsHexDigit)).Take(64).ToDictionary(e=>e.Key,e=>e.Value);
        if(SeatProfiles==null||SeatProfiles.Length!=2)SeatProfiles=["keyboard","auto"];
        for(int i=0;i<2;i++)if(string.IsNullOrWhiteSpace(SeatProfiles[i])||SeatProfiles[i].Length>256)SeatProfiles[i]=i==0?"keyboard":"auto";
        if(SeatProfiles[0]==SeatProfiles[1]&&SeatProfiles[0] is not("none" or "auto"))SeatProfiles[1]="none";
        return this;
    }
    public static bool ValidKey(long value)=>value!=(long)Key.Escape&&Enum.IsDefined((Key)value)&&value!=(long)Key.None;
    // Negative values retain the legacy integer settings format while explicitly encoding axes.
    public const int LeftTriggerBinding=-100,RightTriggerBinding=-101;
    public static bool ValidPadButton(int value)=>value is LeftTriggerBinding or RightTriggerBinding || value>=0&&value<(int)JoyButton.Max&&value is not((int)JoyButton.Start or (int)JoyButton.Back or (int)JoyButton.Guide or (int)JoyButton.DpadLeft or (int)JoyButton.DpadRight or (int)JoyButton.DpadUp or (int)JoyButton.DpadDown);
    public static string PadBindingName(int value)=>value switch{LeftTriggerBinding=>"Left trigger",RightTriggerBinding=>"Right trigger",_=>((JoyButton)value).ToString()};
    public static long[] NormalizeKeys(long[]? input)
    {
        var defaults=DefaultKeys();var output=new long[12];var used=new HashSet<long>();
        for(int i=0;i<output.Length;i++)
        {
            long value=input!=null&&i<input.Length?input[i]:defaults[i];
            if(!ValidKey(value)||used.Contains(value))value=defaults.First(k=>!used.Contains(k));
            output[i]=value;used.Add(value);
        }
        return output;
    }
    public static int[] NormalizePad(int[]? input)
    {
        var defaults=DefaultPad();var output=new int[8];var used=new HashSet<int>();
        for(int i=0;i<output.Length;i++)
        {
            int value=input!=null&&i<input.Length?input[i]:defaults[i];
            if(!ValidPadButton(value)||used.Contains(value))value=defaults.First(k=>!used.Contains(k));
            output[i]=value;used.Add(value);
        }
        return output;
    }
    public bool BindKey(int index,long value)
    {
        if(index is <0 or >=12||!ValidKey(value))return false;
        Keys=NormalizeKeys(Keys);int other=Array.IndexOf(Keys,value);long prior=Keys[index];Keys[index]=value;if(other>=0&&other!=index)Keys[other]=prior;return true;
    }
    public static bool BindPad(int[] mapping,int index,int value)
    {
        if(mapping.Length!=8||index is <0 or >=8||!ValidPadButton(value))return false;
        int other=Array.IndexOf(mapping,value);int prior=mapping[index];mapping[index]=value;if(other>=0&&other!=index)mapping[other]=prior;return true;
    }
    public void Save()
    {
        if(!PersistenceEnabled)return;
        try
        {
            Normalize();System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            string temporary=Path+".tmp";System.IO.File.WriteAllText(temporary,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));
            System.IO.File.Move(temporary,Path,true);LastSaveError="";
        }
        catch(Exception ex){LastSaveError=ex.Message;GD.PushWarning("Settings could not be saved: "+ex.Message);}
    }
    public void Apply()
    {
        Normalize();DisplayServer.WindowSetMode(Fullscreen?DisplayServer.WindowMode.Fullscreen:DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetVsyncMode(Vsync?DisplayServer.VSyncMode.Enabled:DisplayServer.VSyncMode.Disabled);
        if(!Fullscreen)DisplayServer.WindowSetSize(new Vector2I(Width,Width*9/16));
    }
}

public sealed class InputRouter
{
    readonly GameSettings settings;
    readonly Buttons[] suppressed=[Buttons.None,Buttons.None];
    readonly HashSet<Key> logicalOnlyKeys=[];
    readonly Dictionary<(int Device,JoyAxis Axis),bool> triggerHeld=[];
    private int[] _simulatedUiDevices=[];
    public IReadOnlyList<int> ConnectedDevices=>_simulatedUiDevices.Length>0?_simulatedUiDevices:Input.GetConnectedJoypads().ToArray();
    /// <summary>Explicit software-test provider. It never changes Godot's physical device inventory.</summary>
    public void InstallSimulatedUiDevices(params int[] ids){if(settings.PersistenceEnabled)throw new InvalidOperationException("Software devices require isolated test settings");_simulatedUiDevices=ids.Distinct().ToArray();}
    public int[] Devices {get;}=[-1,-2]; // -1 keyboard, -2 unavailable; nonnegative physical pad id
    public InputRouter(GameSettings s)
    {
        ConfigureMenuActions();settings=s.Normalize();var pads=Input.GetConnectedJoypads();
        for(int seat=0;seat<2;seat++)
        {
            string preference=settings.SeatProfiles[seat];
            int candidate=preference=="keyboard"?-1:-2;
            if(preference=="auto")candidate=pads.FirstOrDefault(id=>seat==0||Devices[0]!=id,-2);
            else if(preference.StartsWith("joy:",StringComparison.Ordinal))candidate=pads.FirstOrDefault(id=>ProfileKey(id)==preference,-2);
            if(candidate>=-1&&seat==1&&Devices[0]==candidate)candidate=-2;
            Devices[seat]=candidate;
        }
    }
    public static void ConfigureMenuActions()
    {
        // Explicit any-device bindings avoid editor/default-map differences in exports.
        foreach(var binding in new(string Action,JoyButton Button)[]{("ui_accept",JoyButton.A),("ui_cancel",JoyButton.B),("ui_up",JoyButton.DpadUp),("ui_down",JoyButton.DpadDown),("ui_left",JoyButton.DpadLeft),("ui_right",JoyButton.DpadRight)})
        {
            if(!InputMap.HasAction(binding.Action))InputMap.AddAction(binding.Action);
            if(!InputMap.ActionGetEvents(binding.Action).OfType<InputEventJoypadButton>().Any(e=>e.Device==-1&&e.ButtonIndex==binding.Button))
                InputMap.ActionAddEvent(binding.Action,new InputEventJoypadButton{Device=-1,ButtonIndex=binding.Button});
        }
    }
    public string PadName(int id)=>_simulatedUiDevices.Contains(id)?$"SIMULATED CONTROLLER {Array.IndexOf(_simulatedUiDevices,id)+1}":Input.GetJoyName(id);
    public string DeviceName(int seat)=>Devices[seat]==-1?"Keyboard":Devices[seat]<0?"Unassigned":$"Pad {Devices[seat]+1}: {PadName(Devices[seat])}";
    public bool Assign(int seat,int id)
    {
        if(!IsAssignmentAvailable(seat,id,Devices,ConnectedDevices))return false;
        Devices[seat]=id;settings.SeatProfiles[seat]=id==-1?"keyboard":id==-2?"none":ProfileKey(id);settings.Save();return true;
    }
    public static bool IsAssignmentAvailable(int seat,int id,int[] devices,IReadOnlyList<int> connected)=>seat is >=0 and <=1&&id>=-2&&(id<0||connected.Contains(id))&&(id<-1||devices[1-seat]!=id);
    public string ProfileKey(int id)
    {
        if(_simulatedUiDevices.Contains(id))return $"ui-test:{id}";
        string guid=Input.GetJoyGuid(id),name=Input.GetJoyName(id);
        var info=Input.GetJoyInfo(id);
        // XInput exposes its controller slot. Other drivers use equal-model connection
        // order. No unavailable hardware serial number is invented.
        int slot=info.ContainsKey("xinput_index")?info["xinput_index"].AsInt32():Input.GetConnectedJoypads().Where(p=>Input.GetJoyGuid(p)==guid&&Input.GetJoyName(p)==name).Order().ToList().IndexOf(id);
        string key=$"joy:{guid}|{name}|{slot}";return key.Length<=256?key:key[..256];
    }
    public int[] Mapping(int id)
    {
        string key=ProfileKey(id);
        if(!settings.PadMappings.TryGetValue(key,out var map))
        {
            // Migrate the old model-only setting by copy, never share arrays between pads.
            map=!_simulatedUiDevices.Contains(id)&&settings.PadMappings.TryGetValue(Input.GetJoyGuid(id),out var old)?GameSettings.NormalizePad(old):GameSettings.DefaultPad();
            settings.PadMappings[key]=map;settings.Save();map=settings.PadMappings[key];
        }
        return map;
    }
    public bool BindPad(int device,int index,int value)
    {
        if(!ConnectedDevices.Contains(device))return false;
        if(!GameSettings.BindPad(Mapping(device),index,value))return false;settings.Save();return true;
    }
    public static byte Normalize(bool left,bool right,bool up,bool down)
    {
        int dx=left==right?0:left?-1:1,dy=up==down?0:up?1:-1;return (byte)(5+dx+3*dy);
    }
    public static Key ResolveBindingKey(Key physical,Key logical)=>physical==Key.None||physical==Key.Pause&&logical!=Key.Pause?logical:physical;
    public static Key BindingKey(InputEventKey e)=>ResolveBindingKey(e.PhysicalKeycode,e.Keycode);
    public Key CaptureKey(InputEventKey e)=>settings.KeyboardLayoutMode=="Logical"?e.Keycode:BindingKey(e);
    public void ObserveEvent(InputEvent e)
    {
        // Accessible injected events can lack a usable scan code. Only those events use
        // the logical fallback, so an alternate layout cannot activate two bindings.
        if(e is InputEventKey key && (key.PhysicalKeycode==Key.None || key.PhysicalKeycode==Key.Pause&&key.Keycode!=Key.Pause))
        {
            if(key.Pressed)logicalOnlyKeys.Add(key.Keycode);else logicalOnlyKeys.Remove(key.Keycode);
        }
    }
    public void ClearDeviceState(int device)
    {
        foreach(var key in triggerHeld.Keys.Where(k=>k.Device==device).ToArray())triggerHeld.Remove(key);
        logicalOnlyKeys.Clear();
    }
    public static bool TriggerState(float value,bool wasHeld)=>float.IsFinite(value)&&value>=(wasHeld?.4f:.6f);
    bool ReadBinding(int device,int binding)
    {
        if(binding>=0)return Input.IsJoyButtonPressed(device,(JoyButton)binding);
        JoyAxis axis=binding==GameSettings.LeftTriggerBinding?JoyAxis.TriggerLeft:JoyAxis.TriggerRight;
        var key=(device,axis);bool held=TriggerState(Input.GetJoyAxis(device,axis),triggerHeld.GetValueOrDefault(key));triggerHeld[key]=held;return held;
    }
    public void SuppressHeldButtons()
    {
        for(int seat=0;seat<2;seat++)suppressed[seat]|=ReadRaw(seat,0).Held;
    }
    public InputFrame Sample(int seat,long tick)
    {
        var frame=ReadRaw(seat,tick);return new InputFrame(seat,tick,frame.Direction,FilterSuppressed(frame.Held,ref suppressed[seat]));
    }
    public static Buttons FilterSuppressed(Buttons held,ref Buttons quarantine){quarantine&=held;return held&~quarantine;}
    InputFrame ReadRaw(int seat,long tick)
    {
        int device=Devices[seat];bool l=false,r=false,u=false,d=false;Buttons held=Buttons.None;
        if(device==-1)
        {
            bool K(int i)=>settings.KeyboardLayoutMode=="Logical"?Input.IsKeyPressed((Key)settings.Keys[i]):Input.IsPhysicalKeyPressed((Key)settings.Keys[i])||logicalOnlyKeys.Contains((Key)settings.Keys[i]);l=K(0);r=K(1);u=K(2);d=K(3);
            for(int i=0;i<6;i++)if(K(4+i))held|=(Buttons)(1<<i);if(K(10))held|=Buttons.LP|Buttons.MP;if(K(11))held|=Buttons.LK|Buttons.MK;
        }
        if(device>=0&&ConnectedDevices.Contains(device))
        {
            float x=Input.GetJoyAxis(device,JoyAxis.LeftX),y=Input.GetJoyAxis(device,JoyAxis.LeftY);
            l=Input.IsJoyButtonPressed(device,JoyButton.DpadLeft)||x < -settings.Deadzone;r=Input.IsJoyButtonPressed(device,JoyButton.DpadRight)||x>settings.Deadzone;
            u=Input.IsJoyButtonPressed(device,JoyButton.DpadUp)||y < -settings.Deadzone;d=Input.IsJoyButtonPressed(device,JoyButton.DpadDown)||y>settings.Deadzone;
            var map=Mapping(device);for(int i=0;i<6;i++)if(ReadBinding(device,map[i]))held|=(Buttons)(1<<i);
            if(ReadBinding(device,map[6]))held|=Buttons.LP|Buttons.MP;if(ReadBinding(device,map[7]))held|=Buttons.LK|Buttons.MK;
        }
        return new InputFrame(seat,tick,Normalize(l,r,u,d),held);
    }
}
