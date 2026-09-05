using Godot;
using StrikeLedger.Core;
using StrikeLedger.App;
using System.Text.Json;

public partial class Main
{
    bool networkEvidence,networkEvidenceDone;double networkEvidenceSeconds;long networkCaptureTick;
    BotController? networkEvidenceBot;
    void BeginNetworkEvidence(string[] args)
    {
        int n=Array.IndexOf(args,"--seat");int seat=n>=0&&n+1<args.Length?int.Parse(args[n+1]):0;
        if(seat is <0 or >1){QuitGame(2);return;}
        networkEvidence=true;System.IO.Directory.CreateDirectory(evidenceDir);
        evidenceLog=new(System.IO.Path.Combine(evidenceDir,"native-peer.log"));
        networkEvidenceBot=new(seat,BotMode.Adaptive,(uint)(17+seat*14));
        networkCode="native-evidence";networkAddress="127.0.0.1";networkPort="28371";
        StartNetwork(seat==0);
        evidenceLog.WriteLine($"START UTC={DateTime.UtcNow:O} seat={seat} build={ReplayFormat.Build} content={content.ContentHash} OS={OS.GetName()} renderer={DisplayServer.GetName()}");
    }
    void TickNetworkEvidence(double delta)
    {
        if(!networkEvidence||networkEvidenceDone)return;networkEvidenceSeconds+=delta;
        if(peer==null||networkEvidenceSeconds>900){networkEvidenceDone=true;GD.PushError("Native network evidence failed or timed out");QuitGame(9);return;}
        if(peer.Status==PeerStatus.Preparation&&submittedRound!=peer.Round)
        {
            peer.SubmitPreparation(networkEvidenceBot!.ChoosePreparation(peer.Simulation));submittedRound=peer.Round;
            evidenceLog?.WriteLine($"COMMIT round={peer.Round}");
        }
        if(sim!=null&&sim.Tick>=networkCaptureTick+300){networkCaptureTick=sim.Tick;Capture($"peer-{(networkHost?0:1)}-{sim.Tick:000000}");evidenceLog?.WriteLine($"TICK {sim.Tick} {peer.Status} {sim.Hash()} rollbacks={peer.Rollback.RollbackCount}");}
        if(peer.Status==PeerStatus.MatchOver)
        {
            networkEvidenceDone=true;string path=System.IO.Path.Combine(evidenceDir,"native-network.replay.json");peer.SaveReplay(path);
            var record=ReplayFormat.Load(path,content.ContentHash);var player=new ReplayPlayer(content,record);while(player.FrameAdvance()){}
            bool passed=player.Simulation.Hash()==sim!.Hash();
            System.IO.File.WriteAllText(System.IO.Path.Combine(evidenceDir,"native-network-result.json"),JsonSerializer.Serialize(new{passed,seat=networkHost?0:1,build=ReplayFormat.Build,content=content.ContentHash,utc=DateTime.UtcNow,elapsedSeconds=networkEvidenceSeconds,finalHash=sim.Hash(),replayHash=player.Simulation.Hash(),wallets=sim.Players.Select(p=>p.Credits),scores=sim.Players.Select(p=>p.ScoreHalfPoints),rounds=sim.CompletedRounds,rollbacks=peer.Rollback.RollbackCount,physicalSecondMachine=false},new JsonSerializerOptions{WriteIndented=true}));
            ExportEconomyFiles(path,record);Capture("native-network-result");evidenceLog?.WriteLine("MATCH COMPLETE "+sim.Hash());
            if(passed)CallDeferred(nameof(CompleteSmoke));else QuitGame(9);
        }
    }
}
