using Godot;
using StrikeLedger.Core;
using StrikeLedger.App;
using System.Net;

public partial class Main
{
    int submittedRound=-1,networkResultRound=-1;
    void StartNetwork(bool host)
    {
        try{if(!int.TryParse(networkPort,out int port)||port<1024||port>65534)throw new Exception("Choose a base port from 1024 to 65534.");var remote=IPAddress.Parse(networkAddress);networkHost=host;mode="network";submittedRound=-1;networkResultRound=-1;var cfg=new MatchConfig{Fighter0=fighter[0],Fighter1=fighter[1],Super0=$"art_{art[0]+1}",Super1=$"art_{art[1]+1}",SessionId=networkCode};peer=new PrivateMatchPeer(content,cfg,host?0:1,new IPEndPoint(IPAddress.IsLoopback(remote)?IPAddress.Loopback:IPAddress.Any,host?port:port+1),new IPEndPoint(remote,host?port+1:port));peer.Notice+=Toast;sim=peer.Simulation;recorder=null;paused=false;Clear("network");Heading("Direct connection","CONNECTING…");statusLabel=Text("Waiting for the other peer",56,212,24,Gold);Back();}catch(Exception e){peer?.Dispose();peer=null;Toast(e.Message);}
    }
    void NetworkPreparation(bool reset=true)
    {
        if(peer==null||sim==null)return;int seat=networkHost?0:1;float x=54;if(reset){draft=new[]{new[]{-1,-1,-1},new[]{-1,-1,-1}};floor=new[]{0,0};}Clear("network_prep");Heading("Private preparation",$"ROUND {peer.Round} / LOCK YOUR PLAN","Your opponent sees the commitment first; both plans reveal together. Empty plan on timeout.");Panel(x,178,570,432);Text($"{fighter[seat].ToUpperInvariant()} / {sim.Players[seat].Credits} CR",76,192,28,networkHost?Gold:Cyan);var list=ItemsFor(seat);for(int slot=0;slot<3;slot++){int sl=slot;int choice=draft[seat][slot];string label=choice<0?"No lease":list[slot*2+choice].GetProperty("name").GetString()!;Button(new[]{"SIGNATURE","TECHNIQUE","GAMBIT"}[slot]+" / "+label+" ↔",76,252+slot*62,526,()=>{draft[seat][sl]=(draft[seat][sl]+2)%3-1;NetworkPreparation(false);},slot==0,18);}Button($"Reserve {floor[seat]} CR ↔",76,447,526,()=>{CycleAffordableReserve(seat);NetworkPreparation(false);},false,18);Text($"After lock: {sim.Players[seat].Credits-DraftCost(seat)} CR",76,515,23,Gold);Text("COMMIT / REVEAL\n\nThree optional slots, one wallet.\n\nA lease adds a technique for this round.\nEX and your selected super debit at startup.\n\nThe reserve is a spending floor,\nnot another pool of credits.",674,199,24,Muted,526);Button("Lock plan →",76,551,526,()=>SubmitNetworkPlan(seat));Back();
    }
    void SubmitNetworkPlan(int seat){if(peer==null||sim==null)return;if(sim.Players[seat].Credits-DraftCost(seat)<floor[seat]){Toast("Plan exceeds wallet or reserve.");return;}try{peer.SubmitPreparation(new PreparationPlan(DraftItems(seat),floor[seat]));submittedRound=peer.Round;Clear("network_wait");Heading("Commitment sent","WAITING FOR REVEAL.","Your plan is locked. Both opponents reveal after commitments match.");statusLabel=Text(peer.Diagnostic,56,229,24,Gold);Back();}catch(Exception e){Toast(e.Message);}}
    void TickNetwork()
    {
        if(peer==null)return;peer.Poll();sim=peer.Simulation;if(statusLabel!=null)statusLabel.Text=peer.Status+" / "+peer.Diagnostic;
        if(peer.Status is PeerStatus.Aborted or PeerStatus.Disconnected){string diagnostic=peer.Diagnostic;string? dump=peer.DesyncDump;if(dump!=null){string path=ProjectSettings.GlobalizePath("user://network/desync.json");System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);System.IO.File.WriteAllText(path,dump);}peer.Dispose();peer=null;Clear("network_failed");Heading("Connection ended","MATCH STOPPED.",diagnostic);Text("Incomplete rounds do not pay out. You can return to the connection screen and start again.",56,221,24,Muted,1120);Button("Reconnect",56,338,420,NetworkMenu,true);Back();return;}
        if(peer.Status==PeerStatus.Preparation&&submittedRound!=peer.Round&&screen!="network_prep")NetworkPreparation();
        if(peer.Status==PeerStatus.Paused&&screen is not("pause" or "settings" or "controls" or "moves"))PauseMenu();
        if(peer.Status==PeerStatus.Playing){if(screen!="fight")Clear("fight");var sample=networkEvidence?networkEvidenceBot!.Next(sim):input.Sample(0,sim.Tick);lastInputs[networkHost?0:1]=sample;peer.Advance(sample.Direction,sample.Held);ObserveChanges();}
        if(peer.Status==PeerStatus.RoundResult){if(networkResultRound!=peer.Round){networkResultRound=peer.Round;screenTime=0;}if(screenTime>2&&networkHost)peer.ContinueMatch();}
        if(peer.Status==PeerStatus.MatchOver&&screen!="results"){FinishMatch();return;}UpdateArena();
    }
}
