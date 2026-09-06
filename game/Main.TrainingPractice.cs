using Godot;
using StrikeLedger.App;
using StrikeLedger.Core;

public partial class Main
{
    string practiceMoveId="",practiceBaseId="";bool practiceFacingLeft;
    TrainingConfiguration? practiceConfiguration;
    void ClearPracticeSelection(){practiceConfiguration=null;practiceMoveId=practiceBaseId="";practiceFacingLeft=false;}
    void TryMoveInLab(int seat,string moveId)
    {
        if(peer!=null){Toast("Leave the private match before opening a training fixture.");return;}
        try
        {
            string actor=fighter[seat],other=fighter[1-seat];var definition=content.Fighters[actor];var move=definition.Move(moveId);
            var rental=content.Items.Values.SingleOrDefault(i=>i.EligibleFighters.Contains(actor)&&i.MoveId==moveId);
            if(move.Availability!="base"&&rental==null)throw new InvalidOperationException("Open this branch through its rental entry action.");
            practiceConfiguration=new(){Fighter0=actor,Fighter1=other,Credits0=content.Economy.Cap,Credits1=content.Economy.Cap,Loadout0=new(rental==null?[]:[rental.Id]),StageId=stageId};
            lab=new(content,practiceConfiguration,77);lab.DummyMode=BotMode.Passive;practiceMoveId=moveId;
            var basics=PracticeBaselines();practiceBaseId=(basics.FirstOrDefault(m=>m.Id==rental?.Replaces)
                ??basics.FirstOrDefault(m=>m.Id==move.Mimic?.SourceMoveId)
                ??(move.Throw!=null?basics.FirstOrDefault(m=>m.Throw!=null):null)
                ??(move.Projectile!=null||move.ObjectRules!=null?basics.FirstOrDefault(m=>m.Projectile!=null):null)
                ??basics.FirstOrDefault(m=>m.Kind=="special"&&move.Command.Split('+')[0]==m.Command.Split('+')[0])
                ??(move.Movement.Any(m=>m.Vy>0)?basics.FirstOrDefault(m=>m.Movement.Any(a=>a.Vy>0)):null)
                ??basics.First(m=>m.Id=="s_mp")).Id;
            BeginLab();PracticeMoveMenu();
        }
        catch(Exception error){Toast("Training fixture: "+error.Message);}
    }
    MoveDefinition[] PracticeBaselines()=>content.Fighters[practiceConfiguration!.Fighter0].Moves.Where(m=>m.Availability=="base"&&m.CreditCost==0&&m.DerivedFrom.Length==0&&m.Kind is "normal" or "command_normal" or "special" or "throw").OrderBy(m=>m.Id,StringComparer.Ordinal).ToArray();
    void PracticeMoveMenu()
    {
        if(lab==null||practiceConfiguration==null||practiceMoveId.Length==0){LabMenu();return;}
        paused=true;Clear("lab_comparison");var definition=content.Fighters[practiceConfiguration.Fighter0];var selected=definition.Move(practiceMoveId);var baseline=definition.Move(practiceBaseId);
        Heading("Move practice",selected.Name.ToUpperInvariant(),"Run ordinary inputs, inspect the result, then compare from the same positions, credits and dummy mode.");
        Panel(55,181,1164,231);Text(selected.Command+"   /   "+(selected.AccessPolicy=="prepaid_super"?"ONE PREPAID USE":selected.AccessPolicy=="round_license"?"ROUND LICENSE / REPEATABLE":"FREE BASE KIT"),77,205,25,Gold,1112);
        Text("Each run resets the training fixture and equips the chosen products.\nOwned EX repeats at zero bank; a super permit supplies one startup.\nBranch inputs follow valid windows after the entry command.",77,251,20,Cream,1094);
        Text("Free comparison: "+baseline.Name+"  /  "+baseline.Command+"  /  0 CR",77,355,21,Cyan,1100);
        void Run(bool free)
        {
            try
            {
                var dummy=lab.DummyMode;var cfg=free?practiceConfiguration with{Loadout0=new([])}:practiceConfiguration;
                lab.Configure(cfg);lab.DummyMode=dummy;
                if(practiceFacingLeft){lab.SwapSides();lab.FrameAdvance(5,Buttons.None);}
                lab.BeginDemonstration(0,free?practiceBaseId:practiceMoveId,false);BeginLab();
            }
            catch(Exception error){Toast("Demonstration: "+error.Message);}
        }
        Button("Demonstrate selected move →",55,420,568,()=>Run(false),true,21);Button("Demonstrate free comparison →",651,420,568,()=>Run(true),false,21);
        Button("Choose next free comparison",55,480,568,()=>{var all=PracticeBaselines();practiceBaseId=all[(Array.FindIndex(all,m=>m.Id==practiceBaseId)+1)%all.Length].Id;PracticeMoveMenu();},false,20);
        Button("Dummy: "+lab.DummyMode,651,480,568,()=>{var modes=new[]{BotMode.Passive,BotMode.Guard,BotMode.PerfectParryTraining,BotMode.Jump,BotMode.PokeTraining,BotMode.ProjectileTraining};lab.DummyMode=modes[(Array.IndexOf(modes,lab.DummyMode)+1)%modes.Length];PracticeMoveMenu();},false,20);
        Button("Practice with manual input →",55,540,568,()=>{lab.Configure(practiceConfiguration);BeginLab();},false,21);Button("Export actual input trace",651,540,568,()=>{lab.ExportRecording(ProjectSettings.GlobalizePath("user://training/comparison.json"));Toast("Actual input, events and canonical hashes saved.");},false,20);
        Button(practiceFacingLeft?"Facing left / change →":"Facing right / change →",55,600,568,()=>{practiceFacingLeft=!practiceFacingLeft;PracticeMoveMenu();},false,18);
        Button("Item counter / guidance",651,600,568,()=>{var rental=content.Items.Values.FirstOrDefault(i=>i.EligibleFighters.Contains(definition.Id)&&i.MoveId==selected.Id);lab.DummyMode=selected.Throw!=null?BotMode.Jump:selected.ObjectRules?.Kind is "intercept" or "reflect"?BotMode.ProjectileTraining:BotMode.PokeTraining;PracticeMoveMenu();Toast(rental?.Tradeoff??"Change spacing and defense; compare actual contacts with the free kit.");},false,18);Back(LabMenu);
    }
}
