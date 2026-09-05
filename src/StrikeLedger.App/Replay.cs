using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using StrikeLedger.Core;

namespace StrikeLedger.App;

public sealed record ReplayHeader(int Version,string Build,string ContentHash,MatchConfig Config,uint Seed,bool Assist);
public sealed record ReplayDebit(long Tick,int Seat,string Move,int Amount);
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
    public ReplayDebit[] Debits {get;set;}=[];
}
public sealed record ReplayRecord(ReplayHeader Header,List<ReplayCommand> Commands);
public static class ReplayFormat
{
    public static string Build {get;}="strike-ledger-native-1/"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(typeof(ReplayFormat).Assembly.ManifestModule.ModuleVersionId+"/"+typeof(Simulation).Assembly.ManifestModule.ModuleVersionId))).ToLowerInvariant();
    public const int Version=2,MaxBytes=64*1024*1024,MaxCommands=120000;
    internal static readonly JsonSerializerOptions Json=new(){WriteIndented=false,MaxDepth=16,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
    public static void Save(ReplayRecord replay,string path)
    {
        byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(replay,Json);
        if(bytes.Length>MaxBytes)throw new InvalidDataException("Replay exceeds 64 MiB limit");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);File.WriteAllBytes(path,bytes);
    }
    public static ReplayRecord Load(string path,string expectedContentHash)
    {
        if(new FileInfo(path).Length>MaxBytes)throw new InvalidDataException("Replay exceeds 64 MiB limit");
        ReplayRecord r;
        try{r=JsonSerializer.Deserialize<ReplayRecord>(File.ReadAllBytes(path),Json)??throw new InvalidDataException("Empty replay");}
        catch(JsonException ex){throw new InvalidDataException("Malformed replay",ex);}
        Validate(r,expectedContentHash);return r;
    }
    public static void Validate(ReplayRecord r,string hash)
    {
        if(r.Header is null || r.Commands is null || r.Header.Config is null || r.Header.Version!=Version || r.Header.Build!=Build || r.Header.ContentHash!=hash)throw new InvalidDataException("Replay build, rules or content hash is incompatible");
        if(r.Header.Config.Training || r.Header.Config.Assist || r.Header.Assist)throw new InvalidDataException("Training/assist replays cannot be imported as competitive records");
        if(r.Commands.Count>MaxCommands)throw new InvalidDataException("Replay command count exceeded");
        long tick=-1;
        foreach(var c in r.Commands)
        {
            if(c is null || c.Tick<tick || c.Tick>100000 || c.Key is null || c.Key.Length>128 || c.Hash is null || c.Hash.Length>64 || c.Debits is null || c.Debits.Length>8 || c.Debits.Any(d=>d is null || d.Tick!=c.Tick || d.Seat is <0 or >1 || d.Amount is <1 or >3600 || d.Move is null || d.Move.Length>64) || c.Kind is not ("step" or "preparation" or "fight" or "settlement" or "next"))throw new InvalidDataException("Invalid replay command");
            tick=c.Tick;
            if(c.Kind=="step"){_ =new InputFrame(0,c.Tick,c.Direction0,c.Buttons0);_ =new InputFrame(1,c.Tick,c.Direction1,c.Buttons1);}
            if(c.Kind=="preparation")foreach(var p in new[]{c.Plan0,c.Plan1})
            {if(p is null || p.ItemIds is null || p.ItemIds.Length>3 || p.ItemIds.Any(x=>x is null || x.Length>64) || p.ReserveFloor<0)throw new InvalidDataException("Invalid replay preparation");}
        }
    }
    public static void ExportEconomy(ReplayRecord replay,string path)
    {
        var economy=replay.Commands.Where(c=>c.Kind is "preparation" or "settlement" || (c.Kind=="step" && (c.Hash.Length>0 || c.Debits.Length>0))).Select(c=>new {c.Tick,c.Kind,c.Key,c.Wallet0,c.Wallet1,c.Hash,c.Debits}).ToArray();
        var rounds=new List<object>();var debits=new List<ReplayDebit>();int round=1;
        foreach(var c in replay.Commands)
        {debits.AddRange(c.Debits);if(c.Kind=="settlement"){rounds.Add(new{round=round++,terminalTick=c.Tick,closingWallets=new[]{c.Wallet0,c.Wallet1},combatSpend=new[]{debits.Where(d=>d.Seat==0).Sum(d=>d.Amount),debits.Where(d=>d.Seat==1).Sum(d=>d.Amount)},receipts=debits.ToArray()});debits.Clear();}}
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
        long t=s.Tick;var result=s.Step(a,b);Add(new(){Kind="step",Tick=t,Direction0=a.Direction,Direction1=b.Direction,Buttons0=a.Held,Buttons1=b.Held,Hash=s.Tick%60==0?s.Hash():"",Debits=result.Events.Where(e=>e.Kind==CombatEventKind.Spend).Select(e=>new ReplayDebit(t,e.Seat,e.MoveId,e.Value)).ToArray()},s);return result;
    }
    public void CommitPreparation(Simulation s,PreparationPlan a,PreparationPlan b,string key="")
    {long t=s.Tick;if(key.Length==0)key=$"replay:prep:{t}";s.CommitPreparation(a,b,key);Add(new(){Kind="preparation",Tick=t,Plan0=a,Plan1=b,Key=key,Hash=s.Hash()},s);}
    public void BeginFight(Simulation s){long t=s.Tick;s.BeginFight();Add(new(){Kind="fight",Tick=t,Hash=s.Hash()},s);}
    public void SettleRound(Simulation s,long confirmedTick,string key="")
    {long t=s.Tick;if(key.Length==0)key=$"replay:settle:{t}";s.SettleRound(confirmedTick,key);Add(new(){Kind="settlement",Tick=t,Key=key,Hash=s.Hash()},s);}
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
    void Apply(ReplayCommand c)
    {
        if(c.Tick!=Simulation.Tick)throw new InvalidDataException($"Replay tick mismatch at command {index-1}");
        switch(c.Kind)
        {
            case "step":
                LastStepResult=Simulation.Step(new(0,c.Tick,c.Direction0,c.Buttons0),new(1,c.Tick,c.Direction1,c.Buttons1));
                var debits=LastStepResult.Events.Where(e=>e.Kind==CombatEventKind.Spend).Select(e=>new ReplayDebit(c.Tick,e.Seat,e.MoveId,e.Value));
                if(!debits.SequenceEqual(c.Debits))throw new InvalidDataException($"Replay debit receipt mismatch at tick {c.Tick}");break;
            case "preparation":Simulation.CommitPreparation(c.Plan0!,c.Plan1!,c.Key);break;
            case "fight":Simulation.BeginFight();break;
            case "settlement":Simulation.SettleRound(Simulation.Tick,c.Key);break;
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
