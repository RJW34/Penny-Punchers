using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using StrikeLedger.Core;

namespace StrikeLedger.App;

public sealed record ReplayHeader(int Version,string Build,string ContentHash,MatchConfig Config,uint Seed,bool Assist);
public sealed class ReplayCommand
{
    public string Kind {get;set;}="";
    public long Tick {get;set;}
    public byte Direction0 {get;set;}=5;
    public byte Direction1 {get;set;}=5;
    public Buttons Buttons0 {get;set;}
    public Buttons Buttons1 {get;set;}
    public PreparationPlan? Plan0 {get;set;}
    public PreparationPlan? Plan1 {get;set;}
    public string Key {get;set;}="";
    public string Hash {get;set;}="";
    public int Wallet0 {get;set;}
    public int Wallet1 {get;set;}
    public CombatEvent[] Events {get;set;}=[];
    public PreparationReceipt? Preparation {get;set;}
    public SettlementReceipt? Settlement {get;set;}
    public SkillRewardReceipt[] Skills {get;set;}=[];
    public SuperUseReceipt[] Uses0 {get;set;}=[];
    public SuperUseReceipt[] Uses1 {get;set;}=[];
}
public sealed record ReplayRecord(ReplayHeader Header,List<ReplayCommand> Commands);
public static class ReplayFormat
{
    public static string Build {get;}="pp-shop-only-v2/"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(typeof(ReplayFormat).Assembly.ManifestModule.ModuleVersionId+"/"+typeof(Simulation).Assembly.ManifestModule.ModuleVersionId))).ToLowerInvariant();
    public const int Version=4,MaxBytes=64*1024*1024,MaxCommands=120000;
    internal static readonly JsonSerializerOptions Json=new(){WriteIndented=false,MaxDepth=16,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
    public static void Save(ReplayRecord replay,string path)
    {
        Validate(replay,replay.Header.ContentHash);
        byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(replay,Json);
        if(bytes.Length>MaxBytes)throw new InvalidDataException("Replay exceeds 64 MiB limit");
        ReplayStore.WriteAtomically(path,bytes);
    }
    public static ReplayRecord Load(string path,string expectedContentHash)
    {
        using var input=File.OpenRead(path);
        if(input.Length>MaxBytes)throw new InvalidDataException("Replay exceeds 64 MiB limit");
        using var buffer=new MemoryStream();byte[] chunk=new byte[32768];int count;
        while((count=input.Read(chunk))>0)
        {if(buffer.Length+count>MaxBytes)throw new InvalidDataException("Replay exceeds 64 MiB limit");buffer.Write(chunk,0,count);}
        ReplayRecord r;
        try
        {
            using(var document=JsonDocument.Parse(buffer.GetBuffer().AsMemory(0,(int)buffer.Length),new JsonDocumentOptions{MaxDepth=16}))
            {
                if(document.RootElement.TryGetProperty("Header",out var header)&&header.TryGetProperty("Version",out var version)&&version.TryGetInt32(out int value)&&value<Version)
                    throw new InvalidDataException("Legacy direct-spend replay refused: this build uses shop-only licenses and skill settlement. The original file is unchanged.");
            }
            r=JsonSerializer.Deserialize<ReplayRecord>(buffer.GetBuffer().AsSpan(0,(int)buffer.Length),Json)??throw new InvalidDataException("Empty replay");}
        catch(JsonException ex){throw new InvalidDataException("Malformed replay",ex);}
        Validate(r,expectedContentHash);return r;
    }
    public static void Validate(ReplayRecord r,string hash)
    {
        if(r is null||r.Header is null || r.Commands is null || r.Header.Config is null || r.Header.Version!=Version || r.Header.Build!=Build || r.Header.ContentHash!=hash)throw new InvalidDataException("Replay version, build or shop-only content is incompatible; legacy direct-spend recordings cannot be reinterpreted.");
        if(r.Header.Config.Training || r.Header.Config.Assist || r.Header.Assist)throw new InvalidDataException("Training/assist replays cannot be imported as competitive records");
        if(r.Commands.Count>MaxCommands)throw new InvalidDataException("Replay command count exceeded");
        long tick=-1;
        foreach(var c in r.Commands)
        {
            if(c is null || c.Tick<tick || c.Tick>100000 || c.Key is null || c.Key.Length>128 || c.Hash is null || c.Hash.Length>64 || c.Events is null || c.Events.Length>256 || c.Events.Any(e=>e is null || e.Tick!=c.Tick || e.Seat is < -1 or >1 || !Enum.IsDefined(e.Kind) || e.MoveId is null || e.MoveId.Length>80 || e.Detail is null || e.Detail.Length>8192||e.SourceFighterId is null||e.SourceFighterId.Length>80) || c.Skills is null||c.Skills.Length>64||c.Uses0 is null||c.Uses1 is null||c.Uses0.Length>1||c.Uses1.Length>1||c.Skills.Any(x=>x is null||x.Root is null||x.Id is null||x.Id.Length>512||x.Root.SessionId is null||x.Root.SessionId.Length>128||x.EarnerSeat is <0 or >1||x.Allowed<0||x.Capped<0||x.Nominal!=x.Allowed+x.Capped||!Enum.IsDefined(x.Category))||c.Uses0.Concat(c.Uses1).Any(x=>x is null||x.Key is null||x.Key.Length>256||x.MoveId is null||x.MoveId.Length>80||x.Tick<0||x.ActionOrdinal<1)||c.Kind is not ("step" or "preparation" or "fight" or "settlement" or "next"))throw new InvalidDataException("Invalid replay command");
            tick=c.Tick;
            if(c.Kind=="step"){_ =new InputFrame(0,c.Tick,c.Direction0,c.Buttons0);_ =new InputFrame(1,c.Tick,c.Direction1,c.Buttons1);}
            if(c.Kind=="preparation")foreach(var p in new[]{c.Plan0,c.Plan1})
            {if(p is null || p.ItemIds is null || p.ItemIds.Length>6 || p.ItemIds.Any(x=>x is null || x.Length is <1 or >80) || p.ReserveFloor!=0||p.ContentHash!=hash||p.QuotedCost is null||p.ItemIds.Distinct(StringComparer.Ordinal).Count()!=p.ItemIds.Length)throw new InvalidDataException("Invalid replay preparation");}
        }
    }
    public static void ExportEconomy(ReplayRecord replay,string path)
    {
        var economy=replay.Commands.Where(c=>c.Kind is "preparation" or "settlement" || (c.Kind=="step" && (c.Hash.Length>0 || c.Events.Length>0))).Select(c=>new {c.Tick,c.Kind,c.Key,c.Wallet0,c.Wallet1,c.Hash,c.Events}).ToArray();
        var rounds=RoundEconomyLedger.From(replay);
        File.WriteAllText(path,JsonSerializer.Serialize(new{header=replay.Header,rounds,timeline=economy},new JsonSerializerOptions{WriteIndented=true}));
        string csv="tick,event,wallet0,wallet1,transaction\n"+string.Join('\n',economy.Select(c=>$"{c.Tick},{c.Kind},{c.Wallet0},{c.Wallet1},{c.Key}"));
        File.WriteAllText(Path.ChangeExtension(path,"csv"),csv);
    }
}
public sealed class ReplayRecorder
{
    public ReplayRecord Record {get;}
    public ReplayRecorder(GameContent content,MatchConfig config,uint seed=1)
    {if(config.Training || config.Assist)throw new ArgumentException("Training/assisted recordings are not competitive replays");Record=new(new(ReplayFormat.Version,ReplayFormat.Build,content.ContentHash,config,seed,false),new());}
    void Add(ReplayCommand c,Simulation s)
    {
        if(Record.Commands.Count>=ReplayFormat.MaxCommands)throw new InvalidOperationException("Replay recording full");
        c.Wallet0=s.Players[0].Credits;c.Wallet1=s.Players[1].Credits;Record.Commands.Add(c);
    }
    public StepResult Step(Simulation s,InputFrame a,InputFrame b)
    {
        long t=s.Tick;int bank0=s.Players[0].Credits,bank1=s.Players[1].Credits;var result=s.Step(a,b);
        if(s.Players[0].Credits!=bank0||s.Players[1].Credits!=bank1)throw new InvalidDataException("Combat changed the frozen shop-only bank");
        Add(new(){Kind="step",Tick=t,Direction0=a.Direction,Direction1=b.Direction,Buttons0=a.Held,Buttons1=b.Held,Hash=s.Tick%60==0?s.Hash():"",Events=result.Events.ToArray()},s);return result;
    }
    public void CommitPreparation(Simulation s,PreparationPlan a,PreparationPlan b,string key="")
    {long t=s.Tick;if(key.Length==0)key=$"replay:prep:{t}";if(!s.Content.QuotePreparation(s.Players[0].FighterId,s.Players[0].Credits,a).Valid||!s.Content.QuotePreparation(s.Players[1].FighterId,s.Players[1].Credits,b).Valid)throw new InvalidDataException("Invalid or stale recorded shop cart");a=PurchasePlanning.QuotedPlan(s.Content,s.Players[0].FighterId,s.Players[0].Credits,a.ItemIds);b=PurchasePlanning.QuotedPlan(s.Content,s.Players[1].FighterId,s.Players[1].Credits,b.ItemIds);var receipt=s.CommitPreparation(a,b,key);Add(new(){Kind="preparation",Tick=t,Plan0=a,Plan1=b,Key=key,Hash=s.Hash(),Preparation=receipt},s);}
    public void BeginFight(Simulation s){long t=s.Tick;s.BeginFight();Add(new(){Kind="fight",Tick=t,Hash=s.Hash()},s);}
    public void SettleRound(Simulation s,long confirmedTick,string key="")
    {long t=s.Tick;if(key.Length==0)key=$"replay:settle:{t}";var skills=s.Players.SelectMany(p=>p.SkillReceipts).ToArray();var uses0=s.Players[0].SuperUseReceipts.ToArray();var uses1=s.Players[1].SuperUseReceipts.ToArray();var receipt=s.SettleRound(confirmedTick,key);Add(new(){Kind="settlement",Tick=t,Key=key,Hash=s.Hash(),Settlement=receipt,Skills=skills,Uses0=uses0,Uses1=uses1},s);}
    public void NextRound(Simulation s){long t=s.Tick;s.NextRound();Add(new(){Kind="next",Tick=t,Hash=s.Hash()},s);}
    public void Save(string path)=>ReplayFormat.Save(Record,path);
    public static ReplayRecord Load(string path,string expectedContentHash)=>ReplayFormat.Load(path,expectedContentHash);
}
public sealed class ReplayPlayer
{
    readonly ReplayRecord replay;
    readonly SortedDictionary<int,SimulationSnapshot> checkpoints=new();
    int index;
    public Simulation Simulation {get;}
    public StepResult? LastStepResult {get;private set;}
    public bool Paused {get;set;}=true;
    public double Speed {get;set;}=1;
    public bool Finished=>index>=replay.Commands.Count;
    public int CommandIndex=>index;
    public IReadOnlyList<(long Tick,int Seat0,int Seat1)> WalletGraph=>replay.Commands.Where(c=>c.Hash.Length>0).Select(c=>(c.Tick,c.Wallet0,c.Wallet1)).ToArray();
    public ReplayPlayer(GameContent content,ReplayRecord record)
    {ReplayFormat.Validate(record,content.ContentHash);replay=record;Simulation=new Simulation(content,record.Header.Config);checkpoints[0]=Simulation.Capture();}
    public bool FrameAdvance()
    {
        LastStepResult=null;
        if(Finished)return false;
        do{var c=replay.Commands[index++];Apply(c);if(index%240==0)checkpoints[index]=Simulation.Capture();if(c.Kind=="step")break;}while(!Finished);
        return true;
    }
    public bool Step()=>FrameAdvance();
    static bool ReceiptEqual<T>(T actual,T expected)=>JsonSerializer.SerializeToUtf8Bytes(actual,ReplayFormat.Json).AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(expected,ReplayFormat.Json));
    void Apply(ReplayCommand c)
    {
        if(c.Tick!=Simulation.Tick)throw new InvalidDataException($"Replay tick mismatch at command {index-1}");
        switch(c.Kind)
        {
            case "step":
                int bank0=Simulation.Players[0].Credits,bank1=Simulation.Players[1].Credits;
                LastStepResult=Simulation.Step(new(0,c.Tick,c.Direction0,c.Buttons0),new(1,c.Tick,c.Direction1,c.Buttons1));
                if(Simulation.Players[0].Credits!=bank0||Simulation.Players[1].Credits!=bank1)throw new InvalidDataException("Replay combat mutated frozen bank");
                if(!LastStepResult.Events.SequenceEqual(c.Events))throw new InvalidDataException($"Replay activation/contact/skill receipt mismatch at tick {c.Tick}");break;
            case "preparation":if(!ReceiptEqual(Simulation.CommitPreparation(c.Plan0!,c.Plan1!,c.Key),c.Preparation))throw new InvalidDataException("Replay preparation receipt mismatch");break;
            case "fight":Simulation.BeginFight();break;
            case "settlement":
                if(!Simulation.Players.SelectMany(p=>p.SkillReceipts).SequenceEqual(c.Skills)||!Simulation.Players[0].SuperUseReceipts.SequenceEqual(c.Uses0)||!Simulation.Players[1].SuperUseReceipts.SequenceEqual(c.Uses1))throw new InvalidDataException("Replay skill or prepaid-use receipt mismatch");
                if(!ReceiptEqual(Simulation.SettleRound(Simulation.Tick,c.Key),c.Settlement))throw new InvalidDataException("Replay settlement receipt mismatch");break;
            case "next":Simulation.NextRound();break;
            default:throw new InvalidDataException("Unknown replay command");
        }
        if(c.Hash.Length>0 && !string.Equals(c.Hash,Simulation.Hash(),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException($"Replay diverged at tick {c.Tick}; content/build identity matched but canonical state did not");
        if(c.Wallet0!=Simulation.Players[0].Credits || c.Wallet1!=Simulation.Players[1].Credits)throw new InvalidDataException($"Replay wallet mismatch at tick {c.Tick}");
    }
    public void Seek(long tick)
    {
        if(tick<0)throw new ArgumentOutOfRangeException(nameof(tick));
        int target=replay.Commands.FindIndex(c=>c.Tick>=tick);if(target<0)target=replay.Commands.Count;
        var checkpoint=checkpoints.Last(p=>p.Key<=target);Simulation.Restore(checkpoint.Value);index=checkpoint.Key;
        while(index<target){Apply(replay.Commands[index++]);if(index%240==0)checkpoints[index]=Simulation.Capture();}
        LastStepResult=null;
    }
}
