using System.Collections.ObjectModel;
namespace StrikeLedger.Core;

public sealed record MatchRules(string Id,int TickHz,int MaxPlayers,int MaxRounds,int TargetHalfPoints,int RoundTicks,int CountdownTicks,string DefaultStage);
public sealed record PreparationRules(int LoadoutCap,IReadOnlyDictionary<string,int> SlotPrices,int DefaultReserveFloor,int ExActivationCost)
{
 public IReadOnlyDictionary<string,int> SlotLimits {get;init;}=new ReadOnlyDictionary<string,int>(new Dictionary<string,int>{{"signature",1},{"technique",1},{"gambit",1},{"ex",2},{"super",1}});
 public int MaxProducts=>SlotLimits.Values.Sum();
}
public sealed record PreparationQuote(bool Valid,string Error,int TotalCost,int Remaining,int ExecutableCredits,int ExCost,int SuperCost,int ExCount,int SuperCount,bool CanSuperPlusEx)
{
 public string[] ExMoveIds {get;init;}=[];public string SelectedSuper {get;init;}="";public string ContentHash {get;init;}="";
}

public sealed partial class GameContent
{
 public MatchRules Rules {get;private init;}=new("",60,2,9,10,3600,120,"foundry");
 public PreparationRules Preparation {get;private init;}=new(2400,new ReadOnlyDictionary<string,int>(new Dictionary<string,int>{{"signature",900},{"technique",600},{"gambit",300},{"ex",600},{"super",900}}),0,0);
 public SkillRewardPolicy SkillRewards {get;private init;}=new();
 public bool IsShopOnly=>true;
 /// <summary>Pure mixed-cart preview. Remaining bank never authorizes a combat action.</summary>
 public PreparationQuote QuotePreparation(string fighterId,int credits,PreparationPlan plan,string? selectedSuper=null)
 {
  if(!Fighters.TryGetValue(fighterId,out var fighter))return new(false,"Unknown fighter",0,credits,0,0,0,0,0,false);
  var ids=plan.ItemIds??[];var slots=new Dictionary<string,int>(StringComparer.Ordinal);var replacements=new HashSet<string>(StringComparer.Ordinal);var owned=new HashSet<string>(StringComparer.Ordinal);var exMoves=new List<string>();long total=0;string error="",artId="";int superPrice=0;
  if(credits<0||credits>Economy.Cap)error="Bank outside current rules";
  else if(plan.ReserveFloor!=0)error="Shop-only rules have no reserve floor";
  else if(plan.ContentHash.Length>0&&plan.ContentHash!=ContentHash)error="Cart content identity changed";
  else if(ids.Length>Preparation.MaxProducts||ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length)error="Duplicate products or too many selections";
  foreach(var id in ids)
  {
   if(id is null||!Items.TryGetValue(id,out var item)||!item.EligibleFighters.Contains(fighterId)||item.ImplementationStatus!="implemented"){error="Product is unavailable for this fighter";continue;}
   slots[item.Slot]=slots.GetValueOrDefault(item.Slot)+1;if(!Preparation.SlotLimits.TryGetValue(item.Slot,out int limit)||slots[item.Slot]>limit)error="Product slot limit exceeded";
   if(item.ConflictsWith.Any(ids.Contains))error="Products conflict";
   if(item.Replaces is not null&&!replacements.Add(item.Replaces))error="Two products replace the same action";
   if(!owned.Add(item.MoveId))error="Duplicate move ownership";
   if(item.Slot=="ex")exMoves.Add(item.MoveId);
   if(item.Slot=="super"){var art=fighter.SuperArts.FirstOrDefault(a=>a.MoveId==item.MoveId);if(art is null)error="Unknown super product";else artId=art.Id;superPrice=item.Price;}
   total+=item.Price;
  }
  int cost=(int)Math.Min(int.MaxValue,total),remaining=credits-cost;
  if(error.Length==0&&cost>Preparation.LoadoutCap)error="Cart exceeds the current shop limit";
  if(error.Length==0&&remaining<0)error=$"Plan needs {-(long)remaining} more credits";
  if(error.Length==0&&plan.QuotedCost is {} quoted&&quoted!=cost)error="Quoted cart price changed";
  return new(error.Length==0,error,cost,remaining,Math.Max(0,remaining),0,superPrice,exMoves.Count,artId.Length>0?1:0,artId.Length>0&&exMoves.Count>0){ExMoveIds=exMoves.Order(StringComparer.Ordinal).ToArray(),SelectedSuper=artId,ContentHash=ContentHash};
 }
}
