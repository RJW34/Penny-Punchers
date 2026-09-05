using Godot;
using StrikeLedger.Core;
using System.Text.Json;

int assertions=0;
void Check(bool result,string name){assertions++;if(!result)throw new Exception("FAIL: "+name);}
byte[] directions=[5,4,6,5,8,7,9,8,2,1,3,2,5,4,6,5];
for(int mask=0;mask<16;mask++)Check(InputRouter.Normalize((mask&1)>0,(mask&2)>0,(mask&4)>0,(mask&8)>0)==directions[mask],"SOCD combination "+mask);
var settings=new GameSettings{Master=float.NaN,Music=-1,Sfx=3,Deadzone=float.PositiveInfinity,Width=int.MaxValue,Keys=[-77,0,(long)Key.A,(long)Key.A],SeatProfiles=["keyboard","keyboard"]}.Normalize();
Check(settings.Master==.75f&&settings.Music==0&&settings.Sfx==1&&settings.Deadzone==.35f,"finite bounded preferences");
Check(settings.Width==1280,"supported render resolution");
Check(settings.Keys.Length==12&&settings.Keys.Distinct().Count()==12&&settings.Keys.All(GameSettings.ValidKey),"invalid and duplicate keyboard recovery");
Check(settings.SeatProfiles[1]=="none","one physical input cannot occupy both seats");
var nullSettings=JsonSerializer.Deserialize<GameSettings>("{\"Keys\":null,\"PadMappings\":null,\"SeatProfiles\":null}")!.Normalize();
Check(nullSettings.Keys.Length==12&&nullSettings.PadMappings.Count==0&&nullSettings.SeatProfiles.Length==2,"null file fields recovered");
var keys=new GameSettings();
Check(keys.BindKey(4,(long)Key.I)&&keys.Keys[4]==(long)Key.I&&keys.Keys[5]==(long)Key.U,"keyboard binding conflict swaps existing slot");
Check(!keys.BindKey(4,(long)Key.Escape)&&!keys.BindKey(30,(long)Key.A),"reserved and out-of-range key binding refused");
var padA=GameSettings.DefaultPad();var padB=GameSettings.DefaultPad();
Check(GameSettings.BindPad(padA,0,(int)JoyButton.B)&&padA[0]==(int)JoyButton.B&&padA[4]==(int)JoyButton.X,"pad binding conflict swaps");
Check(padB[0]==(int)JoyButton.X&&padB[4]==(int)JoyButton.B,"separate pad arrays remain independent");
Check(!GameSettings.BindPad(padA,0,(int)JoyButton.Start)&&!GameSettings.BindPad(padA,0,(int)JoyButton.DpadDown),"pause and directional buttons reserved");
var repaired=GameSettings.NormalizePad([2,2,-1,900,(int)JoyButton.Start]);
Check(repaired.Length==8&&repaired.Distinct().Count()==8&&repaired.All(GameSettings.ValidPadButton),"malformed pad mapping repaired");
Check(!InputRouter.IsAssignmentAvailable(1,-1,[-1,-2],[0,1]),"duplicate keyboard assignment rejected");
Check(!InputRouter.IsAssignmentAvailable(1,0,[0,-2],[0,1]),"duplicate controller assignment rejected");
Check(InputRouter.IsAssignmentAvailable(1,1,[0,-2],[0,1]),"two independent controllers accepted");
Check(!InputRouter.IsAssignmentAvailable(1,2,[0,-2],[0,1]),"disconnected controller assignment rejected");
Check(InputRouter.ResolveBindingKey(Key.Pause,Key.Enter)==Key.Enter,"accessible injected key uses logical fallback");
Check(InputRouter.ResolveBindingKey(Key.A,Key.Q)==Key.A,"physical layout key retained");
Check(InputRouter.ResolveBindingKey(Key.Pause,Key.Pause)==Key.Pause,"actual Pause remains Pause");
Buttons quarantine=Buttons.LK;
Check(InputRouter.FilterSuppressed(Buttons.LK,ref quarantine)==Buttons.None,"menu confirm quarantined");
Check(InputRouter.FilterSuppressed(Buttons.LK|Buttons.MP,ref quarantine)==Buttons.MP,"fresh independent input remains usable");
Check(InputRouter.FilterSuppressed(Buttons.None,ref quarantine)==Buttons.None&&quarantine==Buttons.None,"release ends quarantine");
Check(InputRouter.FilterSuppressed(Buttons.LK,ref quarantine)==Buttons.LK,"new press after release permitted");
Console.WriteLine($"BASELINE PASS {assertions}: production-source SOCD, binding validation, assignment eligibility and held-confirm quarantine.");
var persisted=new GameSettings{Master=.63f,Music=.22f,Sfx=.81f,Deadzone=.48f,Width=1920,Fullscreen=true,Vsync=false,ReducedFlashes=true,Shake=false,Keys=keys.Keys.ToArray(),PadMappings=new(){["joy:test:a"]=padA.ToArray(),["joy:test:b"]=padB.ToArray()},SeatProfiles=["joy:test:b","joy:test:a"],PersistenceEnabled=false}.Normalize();
string scratch=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"strike-ledger-settings-check-"+Guid.NewGuid().ToString("N")+".json");
try
{
    string serialized=JsonSerializer.Serialize(persisted);System.IO.File.WriteAllText(scratch,serialized);
    var restored=JsonSerializer.Deserialize<GameSettings>(System.IO.File.ReadAllText(scratch))!.Normalize();
    Check(restored.Master==.63f&&restored.Music==.22f&&restored.Sfx==.81f&&restored.Deadzone==.48f,"settings volume/deadzone serialization and temporary-disk round trip");
    Check(restored.Width==1920&&restored.Fullscreen&&!restored.Vsync&&restored.ReducedFlashes&&!restored.Shake,"display/accessibility preference round trip");
    Check(restored.Keys.SequenceEqual(persisted.Keys),"remapped keyboard serialization round trip");
    Check(restored.PadMappings["joy:test:a"].SequenceEqual(padA)&&restored.PadMappings["joy:test:b"].SequenceEqual(padB),"two independent pad maps serialization round trip");
    Check(restored.SeatProfiles.SequenceEqual(persisted.SeatProfiles),"assigned device profiles serialization round trip");
    Check(!serialized.Contains("PersistenceEnabled")&&!serialized.Contains("LastSaveError")&&restored.PersistenceEnabled,"transient isolation/save-error fields are not persisted");
}
finally{if(System.IO.File.Exists(scratch))System.IO.File.Delete(scratch);}
Console.WriteLine($"PASS {assertions} input/settings assertions against the production GameSettings.cs source. Physical controller hardware was not exercised by this test.");
Console.WriteLine("Scope: round trip uses the production JSON properties/normalizer and an isolated temporary file; it does not overwrite the live user settings or claim a physical unplug test.");
