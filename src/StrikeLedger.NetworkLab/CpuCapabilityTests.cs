using System.Text.Json;
using StrikeLedger.App;
using StrikeLedger.Core;
internal static class CpuCapabilityTests
{
    static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    public static void Run(GameContent content,string output)
    {
        var traces=new List<object>();
        foreach(var fighter in content.Fighters.Values)
        {
            var moves=content.Items.Values.Where(i=>i.EligibleFighters.Contains(fighter.Id)&&(i.Slot=="super"||BotController.SupportsRental(fighter.Move(i.MoveId)))).Select(i=>(Move:fighter.Move(i.MoveId),Item:i));
            foreach(var entry in moves)
            {
                bool witnessed=false;
                for(uint seed=1;seed<=96&&!witnessed;seed++)
                {
                    int initial=0;
                    var simulation=new Simulation(content,new MatchConfig{Fighter0=fighter.Id,Fighter1=fighter.Id,Training=true});simulation.TrainingReset(initial);
                    simulation.SetTrainingState(0,x:330000);simulation.SetTrainingState(1,x:330000+(entry.Move.Throw!=null?32000:entry.Move.Projectile!=null?90000:56000));simulation.SetTrainingLoadout(0,new([entry.Item.Id]));
                    var bot=new BotController(0,BotMode.Adaptive,seed);var samples=new List<InputFrame>();var events=new List<CombatEvent>();
                    for(int tick=0;tick<180&&simulation.Phase==MatchPhase.Fight;tick++)
                    {
                        var input=bot.Next(simulation);samples.Add(input);var step=simulation.Step(input,new(1,simulation.Tick,5,Buttons.None));events.AddRange(step.Events);
                        if(step.Events.Any(e=>e.Kind==CombatEventKind.ActionStarted&&e.Seat==0&&e.MoveId==entry.Move.Id)){witnessed=true;break;}
                    }
                    Require(samples.Take(12).All(i=>i.Direction==5&&i.Held==Buttons.None),"CPU acted before delayed observation");
                    Require(simulation.Players[0].Credits==initial,"CPU gained hidden funding");
                    if(witnessed)
                    {
                        Require(events.Count(e=>e.Seat==0&&e.Kind==CombatEventKind.SuperUseConsumed&&e.MoveId==entry.Move.Id)==(entry.Item.Slot=="super"?1:0),"CPU prepaid super use differs");
                        traces.Add(new{fighter=fighter.Id,move=entry.Move.Id,seed,initial,ending=simulation.Players[0].Credits,input=samples,events,hash=simulation.Hash()});
                    }
                }
                Require(witnessed,"No seeded intentional CPU execution "+fighter.Id+"/"+entry.Move.Id);
            }
            foreach(int seat in new[]{0,1})foreach(int credits in new[]{600,900,1500,content.Economy.Cap})
            {
                var simulation=new Simulation(content,new MatchConfig{Fighter0=fighter.Id,Fighter1=fighter.Id,Training=true});simulation.TrainingReset(credits);var bot=new BotController(seat);var plan=bot.ChoosePreparation(simulation);
                Require(plan.ItemIds.All(id=>content.Items[id].Slot=="super"||BotController.SupportsRental(fighter.Move(content.Items[id].MoveId))),"CPU purchased unsupported rental");
                Require(PurchasePlanning.Preview(content,fighter.Id,credits,plan.ItemIds).Valid,"CPU purchased invalid plan");
            }
        }
        File.WriteAllText(Path.Combine(output,"cpu-capability-traces.json"),JsonSerializer.Serialize(new{build=ReplayFormat.Build,contentHash=content.ContentHash,checks=traces.Count,traces},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("PASS CPU intentional current-art/rental witnesses "+traces.Count);
    }
}
