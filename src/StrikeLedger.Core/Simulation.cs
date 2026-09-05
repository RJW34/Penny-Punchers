namespace StrikeLedger.Core;

/// <summary>Fixed 60 Hz authoritative integer simulation. Rendering and device polling are external.</summary>
public sealed partial class Simulation
{
 public GameContent Content {get;} public MatchConfig Config {get;} public long Tick {get;private set;} public MatchPhase Phase {get;private set;}=MatchPhase.Preparation;
 public int RoundId {get;private set;}=1; public int CompletedRounds {get;private set;} public int TimerTicks {get;private set;} public int PhaseTicks {get;private set;} public int FullFreeze {get;private set;}
 public IReadOnlyList<PlayerState> Players=>Array.AsReadOnly(_players); public IReadOnlyList<ProjectileState> Projectiles=>_projectiles.AsReadOnly(); public TerminalResult? PendingResult {get;private set;} public string MatchDecision {get;private set;}="CONTINUE";
 public PreparationReceipt? LastPreparation {get;private set;} public SettlementReceipt? LastSettlement {get;private set;}
 PlayerState[] _players; readonly List<ProjectileState> _projectiles=[]; readonly List<CombatEvent> _events=[]; int _nextProjectileId=1; string _preparationPayload="",_settlementPayload="";
 readonly StageDefinition _stage; public StageDefinition Stage=>_stage;
 public Simulation(GameContent content,MatchConfig? config=null)
 {
  Content=content;Config=config??new();if(string.IsNullOrWhiteSpace(Config.SessionId)||Config.SessionId.Length>80)throw new ArgumentException("Invalid session id");
  _stage=content.Stages[Config.StageId];_players=[CreatePlayer(0,Config.Fighter0,Config.Super0),CreatePlayer(1,Config.Fighter1,Config.Super1)];TimerTicks=content.RoundTicks;PhaseTicks=content.PreparationTicks;
 }
 PlayerState CreatePlayer(int seat,string fighter,string super)
 {
  var f=Content.Fighters[fighter];if(!f.SuperArts.Any(x=>x.Id==super))throw new ArgumentException("Unknown super art");
  return new PlayerState{Seat=seat,FighterId=fighter,SelectedSuper=super,Credits=Content.Economy.StartingCredits,Health=f.Health,MaxHealth=f.Health,X=_stage.SpawnX[seat],Facing=seat==0?1:-1,ActionFacing=seat==0?1:-1,JuggleBudget=Content.Combat.Juggle.InitialBudget};
 }
 public PreparationReceipt CommitPreparation(PreparationPlan plan0,PreparationPlan plan1,string key="")
 {
  key=key.Length==0?$"{Config.SessionId}:prep:{RoundId}":key;
  string Payload(PreparationPlan p)=>string.Join(",",p.ItemIds.Order(StringComparer.Ordinal))+":"+p.ReserveFloor;
  var payload=Payload(plan0)+"|"+Payload(plan1);
  if(LastPreparation is {} prior&&prior.Round==RoundId){if(prior.Key==key&&payload==_preparationPayload)return prior;throw new InvalidOperationException("Conflicting preparation commit");}
  if(Phase!=MatchPhase.Preparation)throw new InvalidOperationException("Preparation is closed");
  var plans=new[]{plan0,plan1};var costs=new int[2];
  for(int s=0;s<2;s++)
  {
   var p=plans[s];if(p.ItemIds.Length>3||p.ItemIds.Distinct().Count()!=p.ItemIds.Length)throw new ArgumentException("Duplicate or excessive leases");
   var slots=new HashSet<string>(StringComparer.Ordinal);
   foreach(var id in p.ItemIds){if(!Content.Items.TryGetValue(id,out var item)||!item.EligibleFighters.Contains(_players[s].FighterId)||!slots.Add(item.Slot))throw new ArgumentException("Invalid lease selection");costs[s]=checked(costs[s]+item.Price);}
   if(costs[s]>1800||costs[s]>_players[s].Credits||p.ReserveFloor<0||p.ReserveFloor>_players[s].Credits-costs[s])throw new ArgumentException("Unaffordable preparation or reserve");
  }
  for(int s=0;s<2;s++){_players[s].Credits-=costs[s];_players[s].ReserveFloor=plans[s].ReserveFloor;_players[s].LeaseIds=plans[s].ItemIds.Order(StringComparer.Ordinal).ToList();}
  LastPreparation=new(key,RoundId,costs[0],costs[1],_players[0].Credits,_players[1].Credits);_preparationPayload=payload;Phase=MatchPhase.Reveal;PhaseTicks=Content.RevealTicks;return LastPreparation;
 }
 public void BeginFight()
 {
  if(Phase==MatchPhase.Preparation)CommitPreparation(new([]),new([]));
  if(Phase is not(MatchPhase.Reveal or MatchPhase.Countdown))throw new InvalidOperationException("Cannot begin fight");
  Phase=MatchPhase.Fight;PhaseTicks=0;foreach(var p in _players)ClearInputs(p);
 }
 public SettlementReceipt SettleRound(long confirmedThroughTick,string key="")
 {
  if(PendingResult is not {} result)throw new InvalidOperationException("No terminal result");
  key=key.Length==0?$"{Config.SessionId}:settle:{RoundId}":key;var payload=$"{result.TerminalTick}:{result.WinnerSeat}:{result.Reason}";
  if(LastSettlement is {} prior&&prior.Round==RoundId){if(prior.Key==key&&_settlementPayload==payload)return prior;throw new InvalidOperationException("Conflicting settlement");}
  if(Phase!=MatchPhase.PendingResult||confirmedThroughTick<result.TerminalTick||CompletedRounds>=9)throw new InvalidOperationException("Terminal frame is not confirmed");
  var payouts=new (Wallet Wallet,PayoutReceipt Receipt)[2];
  for(int s=0;s<2;s++)payouts[s]=EconomySeed.Payout(new(_players[s].Credits,_players[s].RecoveryTier,Content.Economy),result.WinnerSeat<0?Outcome.Draw:result.WinnerSeat==s?Outcome.Win:Outcome.Loss,Content.Economy);
  for(int s=0;s<2;s++){_players[s].Credits=payouts[s].Wallet.Credits;_players[s].RecoveryTier=payouts[s].Wallet.RecoveryTier;_players[s].ScoreHalfPoints+=result.WinnerSeat<0?1:result.WinnerSeat==s?2:0;_players[s].LeaseIds.Clear();_players[s].ReserveFloor=0;}
  CompletedRounds++;MatchDecision=EconomySeed.SinglesDecision(_players[0].ScoreHalfPoints,_players[1].ScoreHalfPoints,CompletedRounds);Phase=MatchDecision=="CONTINUE"?MatchPhase.RoundResult:MatchPhase.MatchOver;
  LastSettlement=new(key,RoundId,result.WinnerSeat,payouts[0].Receipt,payouts[1].Receipt,MatchDecision);_settlementPayload=payload;return LastSettlement;
 }
 public void NextRound()
 {
  if(Phase!=MatchPhase.RoundResult)throw new InvalidOperationException("No next round");RoundId++;
  for(int s=0;s<2;s++){var old=_players[s];var p=CreatePlayer(s,old.FighterId,old.SelectedSuper);p.Credits=old.Credits;p.RecoveryTier=old.RecoveryTier;p.ScoreHalfPoints=old.ScoreHalfPoints;_players[s]=p;}
  _projectiles.Clear();FullFreeze=0;PendingResult=null;TimerTicks=Content.RoundTicks;Phase=MatchPhase.Preparation;PhaseTicks=Content.PreparationTicks;_preparationPayload="";_settlementPayload="";
 }
 public void TrainingReset(int credits=3600)
 {
  RequireTraining();if(credits<0||credits>Content.Economy.Cap)throw new ArgumentOutOfRangeException(nameof(credits));
  _players=[CreatePlayer(0,Config.Fighter0,Config.Super0),CreatePlayer(1,Config.Fighter1,Config.Super1)];foreach(var p in _players)p.Credits=credits;
  _projectiles.Clear();FullFreeze=0;PendingResult=null;LastPreparation=null;LastSettlement=null;TimerTicks=Content.RoundTicks;Phase=MatchPhase.Preparation;RoundId=1;CompletedRounds=0;MatchDecision="CONTINUE";_preparationPayload="";_settlementPayload="";BeginFight();
 }
 public void SetTrainingState(int seat,int? x=null,int? y=null,int? health=null,int? credits=null,int? stun=null)
 {
  RequireTraining();var p=_players[seat];if(x is {} px)p.X=Math.Clamp(px,_stage.Left+16000,_stage.Right-16000);if(y is {} py)p.Y=Math.Clamp(py,0,300000);
  if(health is {} hp)p.Health=Math.Clamp(hp,0,p.MaxHealth);if(credits is {} cr){p.Credits=Math.Clamp(cr,0,Content.Economy.Cap);p.ReserveFloor=Math.Min(p.ReserveFloor,p.Credits);}if(stun is {} st)p.Stun=Math.Clamp(st,0,Content.Fighters[p.FighterId].StunLimit);
 }
 void RequireTraining(){if(!Config.Training)throw new InvalidOperationException("Training mutation is disabled in competitive matches");}
 static void ClearInputs(PlayerState p){p.History.Clear();p.LastButtons=Buttons.None;p.LastDirection=5;p.BackCharge=p.DownCharge=0;p.BackReadyUntil=p.DownReadyUntil=-1;p.Parry=ParryKind.None;p.ParryTicks=p.ParryRetry=0;p.BufferedAction="";p.BufferExpires=-1;p.LastForwardTap=p.LastBackTap=-100;p.NeutralTicks=1;p.DeferredParry=ParryKind.None;}
 public StepResult Step(InputFrame seat0,InputFrame seat1)
 {
  if(seat0.SeatId!=0||seat1.SeatId!=1||seat0.Frame!=Tick||seat1.Frame!=Tick)throw new ArgumentException("Inputs must identify both seats and current tick");
  _events.Clear();var frame=Tick;
  if(Phase is MatchPhase.PendingResult or MatchPhase.RoundResult or MatchPhase.MatchOver)return new(Tick,[],Hash());
  if(Phase!=MatchPhase.Fight)
  {
   PhaseTicks--;if(PhaseTicks<=0){if(Phase==MatchPhase.Preparation)CommitPreparation(new([]),new([]));else if(Phase==MatchPhase.Reveal){Phase=MatchPhase.Countdown;PhaseTicks=Content.CountdownTicks;}else BeginFight();}
   Tick++;return new(frame,[],Hash());
  }
  var inputs=new[]{seat0,seat1};var pressed=new Buttons[2];var relative=new byte[2];
  for(int s=0;s<2;s++){UpdateFacing(_players[s],_players[1-s]);var p=_players[s];relative[s]=CoreMath.RelativeDirection(inputs[s].Direction,p.Facing);pressed[s]=inputs[s].Held&~p.LastButtons;Sample(p,inputs[s],relative[s]);}
  // Both startup decisions occur before either player's contact effects or arena freeze.
  var alreadyFrozen=FullFreeze>0;
  for(int s=0;s<2;s++)
  {
   var p=_players[s];var released=p.LastButtons&~inputs[s].Held&~p.SuppressNegativeEdge;p.SuppressNegativeEdge&=inputs[s].Held;
   UpdateParryEdge(p,relative[s],alreadyFrozen);
   var candidate=Recognize(p,relative[s],pressed[s],released);if(candidate?.CreditCost>0)p.SuppressNegativeEdge|=inputs[s].Held;
   if(p.ThrowAttacker>=0&&Has(pressed[s],Buttons.LP|Buttons.LK)&&p.ThrowAge<Content.Combat.Throw.TechWindow)TechThrow(p);
   else if(candidate is not null)
   {
    if(alreadyFrozen||p.Hitstop>0){if(CanTransition(p,candidate,true)){p.BufferedAction=candidate.Id;p.BufferedReversal=false;p.BufferExpires=Tick+FullFreeze+p.Hitstop+Content.Inputs.ReversalBufferTicks;}}
    else if(CanTransition(p,candidate,false)){p.BufferedReversal=false;TryStartAction(s,candidate.Id);}
    else if(EligibleReversal(p)){p.BufferedAction=candidate.Id;p.BufferedReversal=true;p.BufferExpires=Tick+Content.Inputs.ReversalBufferTicks;}
   }
   else if(p.BufferedAction.Length>0&&Tick<=p.BufferExpires&&!alreadyFrozen&&p.Hitstop==0)
   {var buffered=Content.Fighters[p.FighterId].Move(p.BufferedAction);if(CanTransition(p,buffered,false)){p.BufferedAction="";TryStartAction(s,buffered.Id);}}
   if(Tick>p.BufferExpires){p.BufferedAction="";p.BufferedReversal=false;}
   p.LastButtons=inputs[s].Held;p.LastDirection=relative[s];p.NeutralTicks=relative[s]==5?p.NeutralTicks+1:0;
  }
  if(FullFreeze>0){FullFreeze--;Tick++;return new(frame,_events.ToArray(),Hash());}
  var stopped=_players.Select(p=>p.Hitstop>0).ToArray();
  for(int s=0;s<2;s++){var p=_players[s];p.PreviousX=p.X;p.PreviousY=p.Y;if(stopped[s]){p.Hitstop--;continue;}AdvancePlayer(p,relative[s],pressed[s]);}
  AdvanceProjectiles();ResolvePushboxes();ResolveContacts(relative,pressed);
  for(int s=0;s<2;s++)if(!stopped[s])FinishPlayerTick(_players[s]);
  if(!Config.Training)TimerTicks=Math.Max(0,TimerTicks-1);
  if(_players.Any(p=>p.Health==0)||TimerTicks==0)
  {
   var a=_players[0];var b=_players[1];var ko=a.Health==0||b.Health==0;int verdict=ko?(a.Health==0?(b.Health==0?0:-1):1):CoreMath.HealthVerdict(a.Health,a.MaxHealth,b.Health,b.MaxHealth);
   PendingResult=new(verdict==0?-1:verdict>0?0:1,Tick,ko?"KO":"TIME");Phase=MatchPhase.PendingResult;Emit(CombatEventKind.RoundTerminal,PendingResult.WinnerSeat,detail:PendingResult.Reason);
  }
  Tick++;return new(frame,_events.ToArray(),Hash());
 }
 void Emit(CombatEventKind kind,int seat,int target=-1,string move="",int value=0,string detail="")=>_events.Add(new(Tick,kind,seat,target,move,value,detail));
 static bool Has(Buttons input,Buttons chord)=>(input&chord)==chord;
 bool EligibleReversal(PlayerState p)=>p.Hitstun>0&&p.Hitstun<=Content.Inputs.ReversalBufferTicks||p.Blockstun>0&&p.Blockstun<=Content.Inputs.ReversalBufferTicks||p.KnockdownTicks>0&&p.KnockdownTicks<=Content.Inputs.ReversalBufferTicks||p.LandingTicks>0&&p.LandingTicks<=Content.Inputs.ReversalBufferTicks||Config.Assist&&p.ActionId.Length>0&&Content.Fighters[p.FighterId].Move(p.ActionId).TotalTicks-p.ActionFrame<=3;
}
