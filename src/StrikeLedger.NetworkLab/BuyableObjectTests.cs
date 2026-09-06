using System.Text.Json;
using StrikeLedger.App;
using StrikeLedger.Core;
internal static class BuyableObjectTests
{
    static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    public static void Run(GameContent content,string output)
    {
        using var trace=new StreamWriter(Path.Combine(output,"buyable-object-traces.jsonl"));string activeCheck="";
        var results=new List<object>();var entries=content.Fighters.Values.SelectMany(f=>f.Moves.Select(m=>(Fighter:f,Move:m))).ToArray();
        (FighterDefinition Fighter,MoveDefinition Move) Entry(string kind)=>entries.Single(e=>e.Move.ObjectRules?.Kind==kind);
        void Check(string name,Action run){activeCheck=name;run();results.Add(new{name,passed=true});Console.WriteLine("PASS object "+name);File.WriteAllText(Path.Combine(output,"buyable-object-tests.json"),JsonSerializer.Serialize(new{build=ReplayFormat.Build,contentHash=content.ContentHash,results},new JsonSerializerOptions{WriteIndented=true}));}
        Simulation Fixture((FighterDefinition Fighter,MoveDefinition Move) entry,int credits=3600)
        {
            string art=entry.Fighter.SuperArts.FirstOrDefault(a=>a.MoveId==entry.Move.Id)?.Id??entry.Fighter.DefaultSuper;
            var simulation=new Simulation(content,new MatchConfig{Fighter0=entry.Fighter.Id,Fighter1="rook",Super0=art,Training=true,StageId="grid"});simulation.TrainingReset(credits);simulation.SetTrainingState(0,x:200000);simulation.SetTrainingState(1,x:500000);
            var item=content.Items.Values.FirstOrDefault(i=>i.MoveId==entry.Move.Id&&i.EligibleFighters.Contains(entry.Fighter.Id));if(item!=null)simulation.SetTrainingLoadout(0,new([item.Id]));return simulation;
        }
        StepResult Step(Simulation simulation,byte a=5,Buttons ba=Buttons.None,byte b=5,Buttons bb=Buttons.None)
        {
            var result=simulation.Step(new(0,simulation.Tick,a,ba),new(1,simulation.Tick,b,bb));trace.WriteLine(JsonSerializer.Serialize(new{scenario=activeCheck,result.Tick,input0=new{direction=a,buttons=ba},input1=new{direction=b,buttons=bb},hash=simulation.Hash(),events=result.Events,wallets=simulation.Players.Select(p=>p.Credits),health=simulation.Players.Select(p=>p.Health),objects=simulation.Projectiles.Select(p=>new{p.Id,p.Owner,p.MoveId,p.IsField,p.X,p.Y,p.Vx,p.Vy,p.Age,p.LifeTicks,p.HitsRemaining,p.ReflectionDepth,p.OriginalFighter,p.OriginalOwner,p.Armed,p.TriggerTicks})}));return result;
        }
        void Wait(Simulation simulation,int ticks){for(int n=0;n<ticks;n++)Step(simulation);}
        CombatEvent[] Play(Simulation simulation,int seat,MoveDefinition move)
        {
            var events=new List<CombatEvent>();foreach(var input in CommandEncoder.Encode(content,move,simulation.Players[seat].Facing))events.AddRange(seat==0?Step(simulation,input.Direction,input.Held).Events:Step(simulation,b:input.Direction,bb:input.Held).Events);return events.ToArray();
        }
        void Until(Simulation simulation,Func<bool> condition,int max=240){for(int n=0;n<max&&!condition();n++)Step(simulation);Require(condition(),"Object fixture condition timed out");}
        Check("prism_owned_zero_bank_occupancy_snapshot_expiry",()=>
        {
            var entry=Entry("prism");var insufficient=Fixture(entry,3600);insufficient.SetTrainingLoadout(0,new([]));var rejected=Play(insufficient,0,entry.Move);Require(rejected.Any(e=>e.Kind==CombatEventKind.Rejected)&&insufficient.Players[0].SuperUseReceipts.Count==0&&insufficient.Fields.Count==0,"Unowned field consumed use or spawned");
            var s=Fixture(entry,0);Play(s,0,entry.Move);Until(s,()=>s.Fields.Count==1);Require(s.Players[0].SuperUseReceipts.Count==1&&s.Players[0].Credits==0,"Field prepaid startup not exact");var field=s.Fields[0];Require(field.LifeTicks==entry.Move.ObjectRules!.LifeTicks&&field.Age==0,"Field lifetime began before placement");
            var snapshot=s.Capture();long tick=s.Tick;Wait(s,30);string final=s.Hash();s.Restore(snapshot);Wait(s,30);Require(s.Hash()==final,"Active field snapshot did not resimulate exactly");
            Until(s,()=>s.Players[0].Actionable);int receipts=s.Players[0].SuperUseReceipts.Count;Play(s,0,entry.Move);Require(s.Players[0].Credits==0&&s.Players[0].SuperUseReceipts.Count==receipts&&s.Fields.Count==1,"Active field refresh debited or duplicated");
            Until(s,()=>s.Fields.Count==0,240);Require(s.Tick-tick>=entry.Move.ObjectRules.LifeTicks,"Field lifetime expired early");
        });
        Check("prism_reflection_retains_source_and_snapshot",()=>
        {
            var entry=Entry("prism");var s=Fixture(entry);Play(s,0,entry.Move);Until(s,()=>s.Fields.Count==1);var normal=content.Fighters["rook"].Moves.First(m=>m.Projectile!=null&&m.Kind=="special"&&m.Availability=="base");Play(s,1,normal);Until(s,()=>s.Projectiles.Any(p=>!p.IsField));var saved=s.Capture();
            var events=new List<CombatEvent>();for(int n=0;n<120;n++)events.AddRange(Step(s).Events);string expected=s.Hash();Require(events.Any(e=>e.Kind==CombatEventKind.ProjectileClash&&e.Detail=="reflected"),"Prism did not reflect normal shot");
            s.Restore(saved);bool observed=false;for(int n=0;n<120;n++){Step(s);var reflected=s.Projectiles.FirstOrDefault(p=>!p.IsField&&p.ReflectionDepth==1);if(reflected!=null){observed=true;Require(reflected.Owner==0&&reflected.OriginalOwner==1&&reflected.OriginalFighter=="rook"&&reflected.OriginalMoveId==normal.Id,"Reflection rewrote source or receipt ownership");var branch=new Simulation(content,s.Config);branch.Restore(s.Capture());Require(branch.Hash()==s.Hash(),"Cross-fighter reflected projectile restore failed");}}
            Require(observed&&s.Hash()==expected,"Reflected timeline did not reproduce");
        });
        Check("reflection_occupied_owner_dissipates_without_refund",()=>
        {
            var entry=Entry("prism");var s=Fixture(entry);var arc=entries.Single(e=>e.Move.Projectile is {Vy:>0});var arcItem=content.Items.Values.Single(i=>i.MoveId==arc.Move.Id);s.SetTrainingLoadout(0,new([arcItem.Id,content.Items.Values.Single(i=>i.MoveId==entry.Move.Id).Id]));
            Play(s,0,entry.Move);Until(s,()=>s.Fields.Count==1);Until(s,()=>s.Players[0].Actionable);
            // An upward shot remains alive above the incoming horizontal shot. This
            // avoids ordinary projectile clashes masking the occupied-owner rule.
            Play(s,0,arc.Move);Until(s,()=>s.Projectiles.Any(p=>!p.IsField&&p.Owner==0));s.SetTrainingState(1,x:360000);
            var opponent=content.Fighters["rook"].Moves.First(m=>m.Projectile!=null&&m.Kind=="special"&&m.Availability=="base");Play(s,1,opponent);
            var snapshot=s.Capture();int[] wallets=s.Players.Select(p=>p.Credits).ToArray();int[] receipts=s.Players.Select(p=>p.SuperUseReceipts.Count).ToArray();var events=new List<CombatEvent>();
            for(int n=0;n<100;n++){events.AddRange(Step(s).Events);Require(s.Projectiles.Count(p=>!p.IsField&&p.Owner==0)<=1,"Reflection exceeded owner projectile limit");}
            Require(events.Any(e=>e.Detail=="intercepted"&&e.MoveId==opponent.Id)&&!events.Any(e=>e.Detail=="reflected"),"Occupied owner did not dissipate the incoming shot at the field");
            Require(s.Players.Select(p=>p.Credits).SequenceEqual(wallets)&&s.Players.Select(p=>p.SuperUseReceipts.Count).SequenceEqual(receipts),"Interception altered credit receipts");s.Restore(snapshot);Require(s.Projectiles.Any(p=>!p.IsField&&p.Owner==0),"Occupied-owner snapshot lost existing shot");
        });
        Check("interceptors_exclude_paid_projectile_tiers",()=>
        {
            foreach(string kind in new[]{"intercept","reflect"})
            {
                var entry=Entry(kind);var s=Fixture(entry);var paid=content.Fighters["rook"].Moves.Single(m=>m.Kind=="ex_special"&&m.Projectile!=null);s.SetTrainingLoadout(1,new([content.Items.Values.Single(i=>i.EligibleFighters.Contains("rook")&&i.MoveId==paid.Id).Id]));Play(s,1,paid);Until(s,()=>s.Projectiles.Any(p=>Math.Abs(p.X-s.Players[0].X)<74000));var events=new List<CombatEvent>();events.AddRange(Play(s,0,entry.Move));for(int n=0;n<40;n++)events.AddRange(Step(s).Events);
                Require(!events.Any(e=>e.Detail is "reflected" or "intercepted"),"Paid projectile was eligible for ordinary-shot interception");Require(s.Players[1].OwnedEx.Contains(paid.Id)&&s.Players[1].Credits==content.Economy.Cap,"EX license changed frozen bank");
            }
        });
        Check("prism_parry_consumes_contact_and_recast_requires_new_round",()=>
        {
            var entry=Entry("prism");var s=Fixture(entry,0);Play(s,0,entry.Move);Until(s,()=>s.Fields.Count==1);var field=s.Fields[0];int contacts=field.HitsRemaining;int life=field.LifeTicks;
            s.SetTrainingState(1,x:field.X+18000);var parry=Step(s,b:4);Require(parry.Events.Any(e=>e.Kind==CombatEventKind.Parry&&e.Seat==1)&&s.Fields[0].HitsRemaining==contacts-1,"Parry did not consume field contact");Require(s.Fields[0].LifeTicks==life-1,"Parry replenished field lifetime");
            s.SetTrainingState(1,x:500000);Until(s,()=>s.Fields.Count==0,240);Until(s,()=>s.Players[0].Actionable);var denied=Play(s,0,entry.Move);Require(denied.Any(e=>e.Kind==CombatEventKind.Rejected)&&s.Players[0].SuperUseReceipts.Count==1&&s.Players[0].Credits==0&&s.Fields.Count==0,"Consumed super permit allowed field recast");
        });
        Check("field_destroyed_before_contact",()=>
        {
            foreach(string kind in new[]{"anchor","prism"})
            {
                var entry=Entry(kind);var s=Fixture(entry);Play(s,0,entry.Move);Until(s,()=>s.Fields.Count==1);var field=s.Fields[0];s.SetTrainingState(1,x:field.X+45000);var events=new List<CombatEvent>();events.AddRange(Play(s,1,content.Fighters["rook"].Move(kind=="anchor"?"c_mk":"s_hp")));for(int n=0;n<30;n++)events.AddRange(Step(s).Events);
                Require(events.Any(e=>e.Kind==CombatEventKind.ProjectileClash&&e.Detail=="field-destroyed")&&s.Fields.Count==0,kind+" field not destroyed by actual direct strike");
            }
        });
        Check("anchor_arming_windup_block_consumption",()=>
        {
            var entry=Entry("anchor");var s=Fixture(entry);Play(s,0,entry.Move);Until(s,()=>s.Fields.Count==1);var field=s.Fields[0];Require(!field.Armed&&!field.Triggered,"Anchor armed before its delay");
            Wait(s,entry.Move.ObjectRules!.ArmingTicks);field=s.Fields[0];Require(field.Armed&&!field.Triggered,"Anchor arming clock failed");s.SetTrainingState(1,x:field.X+22000);Step(s);field=s.Fields[0];Require(field.Triggered&&field.TriggerTicks==entry.Move.ObjectRules.TriggerWindup,"Anchor skipped visible trigger windup");
            var events=new List<CombatEvent>();for(int n=0;n<30;n++)events.AddRange(Step(s,b:6).Events);Require(events.Any(e=>e.Kind==CombatEventKind.Block&&e.Seat==1)&&s.Fields.Count==0,"Anchor block did not consume one contact");
        });
        Check("interception_and_reflection_are_finite",()=>
        {
            foreach(string kind in new[]{"intercept","reflect"})
            {
                var entry=Entry(kind);var s=Fixture(entry);var shot=content.Fighters["rook"].Moves.First(m=>m.Projectile!=null&&m.Kind=="special"&&m.Availability=="base");Play(s,1,shot);Until(s,()=>s.Projectiles.Any(p=>Math.Abs(p.X-s.Players[0].X)<74000));
                var events=new List<CombatEvent>();events.AddRange(Play(s,0,entry.Move));for(int n=0;n<40;n++)events.AddRange(Step(s).Events);Require(events.Any(e=>e.Kind==CombatEventKind.ProjectileClash&&e.Detail==(kind=="reflect"?"reflected":"intercepted")),kind+" did not interact in its authored window");Require(s.Players.All(p=>p.SuperUseReceipts.Count==0),"Rental interception manufactured a spend/refund");
            }
        });
        Check("simultaneous_anchor_pulses_are_seat_symmetric",()=>
        {
            var entry=Entry("anchor");var s=new Simulation(content,new MatchConfig{Fighter0=entry.Fighter.Id,Fighter1=entry.Fighter.Id,Training=true,StageId="grid"});s.TrainingReset();s.SetTrainingState(0,x:280000);s.SetTrainingState(1,x:420000);
            var item=content.Items.Values.Single(i=>i.MoveId==entry.Move.Id);s.SetTrainingLoadout(0,new([item.Id]));s.SetTrainingLoadout(1,new([item.Id]));var inputs=CommandEncoder.Encode(content,entry.Move);
            foreach(var input in inputs)Step(s,input.Direction,input.Held,CoreMath.RelativeDirection(input.Direction,-1),input.Held);
            Until(s,()=>s.Fields.Count==2);Until(s,()=>s.Players.All(p=>p.Actionable));var fields=s.Fields.OrderBy(p=>p.Owner).ToArray();s.SetTrainingState(0,x:fields[1].X);s.SetTrainingState(1,x:fields[0].X);
            Wait(s,entry.Move.ObjectRules!.TriggerWindup+2);Require(s.Players[0].Health==s.Players[1].Health&&s.Players[0].Health<s.Players[0].MaxHealth,"Simultaneous field pulses favored a seat");Require(s.Fields.Count==0,"Owner-hit anchors survived their simultaneous trade");
        });
        Check("actual_reflected_origin_precision_parry_reward_boundary",()=>
        {
            var entry=Entry("prism");var normal=content.Fighters["rook"].Moves.First(m=>m.Projectile!=null&&m.Kind=="special"&&m.Availability=="base");var boundaries=new List<object>();
            void Verify(Simulation s,int defender,int projectileId,RootAttackId original,bool earns)
            {
                var snapshot=s.Capture();var scout=new Simulation(content,s.Config);scout.Restore(snapshot);long contact=-1;
                for(int n=0;n<180&&contact<0;n++){var step=Step(scout);if(step.Events.Any(e=>e.Kind==CombatEventKind.Hit&&e.Target==defender&&e.ProjectileId==projectileId))contact=step.Tick;}
                Require(contact>=0,"Reflected-origin fixture shot never reached defender");StepResult? result=null;
                while(s.Tick<=contact)result=defender==0?Step(s,a:s.Tick==contact?(byte)6:(byte)5):Step(s,b:s.Tick==contact?(byte)4:(byte)5);
                Require(result!.Events.Any(e=>e.Kind==CombatEventKind.Parry&&e.Seat==defender&&e.ProjectileId==projectileId),"Authored projectile was not manually parried");var fact=s.LastResolvedContacts.Single(f=>f.Outcome==ContactOutcome.Parry&&f.EarnerSeat==defender);
                Require(fact.Root==original&&fact.ParryEligibleAge is 0 or 1&&fact.FreshManualParry&&!fact.BonusIneligibleFrozenEdge,"Projectile parry lost original root or precise eligible timing");
                bool opportunity=result.Events.Any(e=>e.Kind==CombatEventKind.SkillOpportunity&&e.Seat==defender&&e.Value==content.SkillRewards.PerfectParry);Require(opportunity==earns,"Own-origin reflection reward exclusion or enemy-origin positive control failed");Require(s.Players.All(p=>p.PendingSkillCredits==0&&p.Credits==content.Economy.Cap),"Practice precision changed competitive bank/ledger");
                boundaries.Add(new{defender,earns,opportunity,initialSnapshot=Convert.ToBase64String(snapshot.Bytes),contact,fact,events=result.Events,finalHash=s.Hash()});
            }
            var reflected=Fixture(entry);Play(reflected,0,entry.Move);Until(reflected,()=>reflected.Fields.Count==1);Play(reflected,1,normal);Until(reflected,()=>reflected.Projectiles.Any(p=>!p.IsField));var shot=reflected.Projectiles.Single(p=>!p.IsField);var original=shot.RootAttack!;int id=shot.Id;
            Until(reflected,()=>reflected.Projectiles.Any(p=>p.Id==id&&p.ReflectionDepth==1));shot=reflected.Projectiles.Single(p=>p.Id==id);Require(shot.RootAttack==original&&shot.Owner==0&&original.OriginSeat==1,"Reflection changed authoritative attack origin");Verify(reflected,1,id,original,false);
            var enemy=Fixture(entry);Play(enemy,1,normal);Until(enemy,()=>enemy.Projectiles.Any(p=>!p.IsField));shot=enemy.Projectiles.Single(p=>!p.IsField);Verify(enemy,0,shot.Id,shot.RootAttack!,true);
            File.WriteAllText(Path.Combine(output,"reflected-origin-precision.json"),JsonSerializer.Serialize(new{build=ReplayFormat.Build,contentHash=content.ContentHash,trainingOpportunityOnly=true,boundaries},new JsonSerializerOptions{WriteIndented=true}));
        });
        Check("arc_projectile_moves_vertically_and_restores",()=>
        {
            var entry=entries.Single(e=>e.Move.Projectile is {Vy:>0});var s=Fixture(entry);Play(s,0,entry.Move);Until(s,()=>s.Projectiles.Count>0);var shot=s.Projectiles[0];int y=shot.Y;Wait(s,3);Require(s.Projectiles.Count>0&&s.Projectiles[0].Y==y+3*entry.Move.Projectile!.Vy,"Arc projectile vertical motion wrong");var snapshot=s.Capture();Wait(s,8);string hash=s.Hash();s.Restore(snapshot);Wait(s,8);Require(s.Hash()==hash,"Arc projectile snapshot lost vertical motion");
        });
    }
}
