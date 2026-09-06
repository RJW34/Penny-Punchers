using System.Text.Json;
using StrikeLedger.App;
using StrikeLedger.Core;
internal static class DemonstrationTests
{
    static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    public static void Run(GameContent content,string output)
    {
        var results=new List<object>();
        foreach(var fighter in content.Fighters.Values)foreach(var move in fighter.Moves.Where(m=>m.DerivedFrom.Length==0))
        {
            var item=content.Items.Values.FirstOrDefault(i=>i.EligibleFighters.Contains(fighter.Id)&&i.MoveId==move.Id);
            if(move.Availability!="base"&&item==null)continue;
            var cfg=new TrainingConfiguration{Fighter0=fighter.Id,Fighter1=fighter.Id,Credits0=content.Economy.Cap,Credits1=content.Economy.Cap,Loadout0=new(item==null?[]:[item.Id])};
            foreach(int facing in new[]{1,-1})
            {
                var training=new TrainingSession(content,cfg);training.DummyMode=BotMode.Passive;
                // Requeue against the mirrored fixture, retaining real jump and command inputs.
                int distance=move.Command.StartsWith("CLOSE+",StringComparison.Ordinal)||move.Throw!=null?32000:56000;training.Simulation.SetTrainingState(0,x:facing==1?330000:330000+distance);training.Simulation.SetTrainingState(1,x:facing==1?330000+distance:330000);training.FrameAdvance(5,Buttons.None);training.BeginDemonstration(0,move.Id,false);
                training.FrameAdvance(5,Buttons.None);training.SaveCheckpoint();
                for(int n=0;n<160;n++)training.FrameAdvance(5,Buttons.None);
                var first=training.Simulation.Hash();var events=training.Recording.SelectMany(f=>f.Events).ToArray();
                Require(events.Any(e=>e.Seat==0&&e.Kind==CombatEventKind.ActionStarted&&e.MoveId==move.Id),"Demo did not enter "+fighter.Id+"/"+move.Id+" facing "+facing);
                Require(events.Count(e=>e.Seat==0&&e.Kind==CombatEventKind.SuperUseConsumed&&e.MoveId==move.Id)==(move.AccessPolicy=="prepaid_super"?1:0)&&training.Simulation.Players[0].Credits==cfg.Credits0,"Demo prepaid-use/frozen-bank mismatch "+move.Id);
                training.RestoreCheckpoint();for(int n=0;n<160;n++)training.FrameAdvance(5,Buttons.None);Require(training.Simulation.Hash()==first,"Demo checkpoint future differs "+move.Id);
                results.Add(new{fighter=fighter.Id,move=move.Id,facing,started=true,checkpoint=true,branches=events.Where(e=>e.Seat==0&&e.Kind==CombatEventKind.ActionStarted&&e.MoveId!=move.Id).Select(e=>e.MoveId).Distinct().ToArray()});
            }
        }
        foreach(string drill in new[]{"high_low_air_parry","red_parry","multihit_parry","quickrise_reversal"})
        {
            var training=new TrainingSession(content);training.Reset(drill);for(int n=0;n<160;n++)training.FrameAdvance(5,Buttons.None);
            Require(training.Recording.SelectMany(f=>f.Events).Any(e=>e.Seat==1&&e.Kind==CombatEventKind.Hit),"Repeatable drill feed whiffed: "+drill);
            if(drill=="quickrise_reversal")Require(training.Recording.SelectMany(f=>f.Events).Any(e=>e.Kind==CombatEventKind.Knockdown&&e.Seat==0),"Quickrise feed did not give soft knockdown");
        }
        foreach(var mode in new[]{BotMode.PokeTraining,BotMode.ProjectileTraining})
        {
            var training=new TrainingSession(content);training.DummyMode=mode;training.SaveCheckpoint();for(int n=0;n<110;n++)training.FrameAdvance(5,Buttons.None);string before=training.Simulation.Hash();Require(training.Recording.SelectMany(f=>f.Events).Any(e=>e.Seat==1&&e.Kind==CombatEventKind.ActionStarted),"Counter dummy did not act");training.RestoreCheckpoint();for(int n=0;n<110;n++)training.FrameAdvance(5,Buttons.None);Require(training.Simulation.Hash()==before,"Counter dummy checkpoint mismatch");
        }
        var source=new Simulation(content,new MatchConfig());source.CommitPreparation(new([]),new([]));source.BeginFight();source.Step(new(0,source.Tick,5,Buttons.None),new(1,source.Tick,5,Buttons.None));string hash=source.Hash();var branch=TrainingSession.FromReplay(source);branch.FrameAdvance(5,Buttons.LP);Require(source.Hash()==hash&&!source.Config.Training&&branch.Simulation.Config.Training,"Practice takeover mutated competition");
        File.WriteAllText(Path.Combine(output,"training-demonstrations.json"),JsonSerializer.Serialize(new{build=ReplayFormat.Build,contentHash=content.ContentHash,neutralAndAirEntrypoints=results.Count,results,detachedCompetitiveBranch=true},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("PASS demonstrated "+results.Count+" entrypoints/facings with checkpoint restore");
    }
}
