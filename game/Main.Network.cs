using Godot;
using StrikeLedger.Core;
using StrikeLedger.App;
using System.Net;
public partial class Main
{
    int submittedRound=-1,networkResultRound=-1,networkDraftRound=-1;string lastLobbyView="";
    void LeavePrivateMatch(string reason="Left match")
    {
        var leaving=peer;peer=null;if(leaving==null)return;
        try{leaving.Disconnect(reason);leaving.Dispose();}catch(Exception error){GD.PrintErr("PRIVATE_LEAVE "+error.Message);}
        submittedRound=networkResultRound=networkDraftRound=-1;lastLobbyView="";
    }
    void RequestPrivateRematch()
    {
        if(peer==null){NetworkMenu();return;}try{peer.RequestRematch();submittedRound=networkResultRound=-1;Clear("network_rematch");Heading("Same connection","REMATCH REQUESTED.","Waiting for both players to choose Run it back. Fresh wallets begin only after both agree.");Back(Title);}catch(Exception error){Toast(error.Message);}
    }
    void StartNetwork(bool host)
    {
        try
        {
            if(!int.TryParse(networkPort,out int port)||port<1024||port>65534)throw new Exception("Choose a base port from 1024 to 65534.");var remote=IPAddress.Parse(networkAddress);LeavePrivateMatch("Starting another connection");networkHost=host;mode="network";submittedRound=networkResultRound=-1;
            var cfg=new MatchConfig{Fighter0=fighter[0],Fighter1=fighter[1],SessionId=networkCode,StageId=stageId};
            peer=new(content,cfg,host?0:1,new(IPAddress.IsLoopback(remote)?IPAddress.Loopback:IPAddress.Any,host?port:port+1),new(remote,host?port+1:port),negotiateLineup:!networkEvidence);peer.Notice+=Toast;sim=peer.Simulation;recorder=null;replay=null;paused=false;ResetPresentationTimeline();Clear("network");Heading("Direct connection","CONNECTING…");statusLabel=Text("Waiting for the other peer",56,212,24,Gold);Back();
        }catch(Exception error){LeavePrivateMatch("Connection failed");Toast(error.Message);}
    }
    void NetworkLobby()
    {
        if(peer==null)return;Clear("network_lobby");Heading("Connected lobby","CHOOSE YOUR SIDE.","Each player controls their own fighter. The host selects the stage. EX licenses and optional super are chosen in each round’s shop.");
        for(int seat=0;seat<2;seat++)
        {
            int s=seat;float x=55+610*s;var selection=s==(networkHost?0:1)?peer.LocalSelection:peer.RemoteSelection;Text($"P{s+1} / "+(s==(networkHost?0:1)?"YOU":"PEER"),x,196,27,s==0?Gold:Cyan);
            if(selection==null){Text("Waiting for selection",x,254,24,Muted);continue;}
            if(s==(networkHost?0:1))
            {
                Button(FighterName(selection.Fighter)+" ↔",x,245,555,()=>{peer.SelectLineup(selection.Fighter=="rook"?"vale":"rook");NetworkLobby();},true);
                Text("Super: choose a permit in the next shop",x,322,21,Muted);
            }
            else{Text(FighterName(selection.Fighter),x,256,28);Text("Super: purchased each round",x,322,21,Muted);}
        }
        if(networkHost)Button("Stage: "+content.Stages[peer.LobbyStage].Name+" ↔",55,393,1165,()=>{var ids=content.Stages.Keys.Order(StringComparer.Ordinal).ToArray();string next=ids[(Array.IndexOf(ids,peer.LobbyStage)+1)%ids.Length];var own=peer.LocalSelection!;peer.SelectLineup(own.Fighter,next);NetworkLobby();});
        else Text("HOST STAGE / "+content.Stages[peer.LobbyStage].Name,55,409,25,Gold);
        Button(peer.LocalReady?"READY / waiting for peer":"Ready lineup →",55,475,555,()=>{try{peer.ReadyLineup();NetworkLobby();}catch(Exception error){Toast(error.Message);}});
        Text("Peer: "+(peer.RemoteReady?"READY":"choosing"),665,491,24,Muted);Text("Changing either selection clears readiness. Shared content, rules and build hashes were verified before this lobby opened.",55,565,21,Muted,1150);Back(Title);
    }
    void NetworkPreparation(bool reset=true)
    {
        if(peer==null||sim==null)return;int seat=networkHost?0:1;if(reset){ResetShopDrafts();networkDraftRound=peer.Round;}
        Clear("network_prep");Heading("Private preparation",$"ROUND {peer.Round} / LOCK YOUR PLAN","Both exact carts reveal together after lock. Timeout keeps your last valid cart.");purchaseButtons.Clear();purchaseProductIds.Clear();DrawPurchasePanel(seat,54,166,true);
        Text("COMMIT / REVEAL\n\nThree technique slots, up to two EX licenses\nand one prepaid super use.\n\nAll money is spent here. Owned EX repeats\nafter recovery, even with zero bank.\n\nSkill awards deposit only at settlement.",674,199,24,Muted,526);
        Button("Leave private match",674,648,526,Title);
    }
    void SubmitNetworkPlan(int seat){if(peer==null||sim==null)return;try{peer.SubmitPreparation(PurchasePlanning.QuotedPlan(content,fighter[seat],sim.Players[seat].Credits,LastValidDraftItems(seat)));RememberShopPlan(seat);submittedRound=peer.Round;Clear("network_wait");Heading("Commitment sent","WAITING FOR REVEAL.","Your plan is locked. Both opponents reveal after commitments match.");statusLabel=Text(peer.Diagnostic,56,229,24,Gold);Back(Title);}catch(Exception error){Toast(error.Message);}}
    void TickNetwork()
    {
        if(peer==null)return;if(peer.Status==PeerStatus.Preparation&&networkDraftRound==peer.Round)peer.SetPreparationDraft(new(LastValidDraftItems(networkHost?0:1)));peer.Poll();sim=peer.Simulation;
        // The final remote input may confirm rewards and settle the round during
        // Poll. Drain that timeline before result/shop transitions can reset it.
        ObserveChanges();
        if(peer.LocalPlanSubmitted&&submittedRound!=peer.Round){if(networkDraftRound==peer.Round)RememberShopPlan(networkHost?0:1);submittedRound=peer.Round;}if(statusLabel!=null)statusLabel.Text=peer.Status+" / "+peer.Diagnostic;
        if(peer.Status is PeerStatus.Aborted or PeerStatus.Disconnected)
        {
            string diagnostic=peer.Diagnostic;string? dump=peer.DesyncDump;if(dump!=null)try{string path=ProjectSettings.GlobalizePath("user://network/desync.json");System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);System.IO.File.WriteAllText(path,dump);}catch(Exception error){GD.PrintErr(error.Message);}
            LeavePrivateMatch(diagnostic);Clear("network_failed");Heading("Connection ended","MATCH STOPPED.",diagnostic);Text("Incomplete rounds do not pay out. Return to the connection screen to start again.",56,221,24,Muted,1120);Button("Reconnect",56,338,420,NetworkMenu,true);Back();return;
        }
        if(peer.Status==PeerStatus.Lobby)
        {
            string view=$"{peer.LocalSelection}/{peer.RemoteSelection}/{peer.LocalReady}/{peer.RemoteReady}";if(view!=lastLobbyView||screen!="network_lobby"){lastLobbyView=view;NetworkLobby();}return;
        }
        fighter=new[]{sim.Config.Fighter0,sim.Config.Fighter1};art=new[]{0,0};stageId=sim.Config.StageId;
        if(peer.Status==PeerStatus.Preparation&&submittedRound!=peer.Round&&screen!="network_prep")NetworkPreparation();
        if(peer.Status==PeerStatus.Paused&&screen is not("pause" or "settings" or "controls" or "moves"))PauseMenu();
        if(peer.Status==PeerStatus.Playing){if(screen!="fight"){paused=false;Clear("fight");}var sample=networkEvidence?networkEvidenceBot!.Next(sim):input.Sample(0,sim.Tick);lastInputs[networkHost?0:1]=sample;peer.Advance(sample.Direction,sample.Held);ObserveChanges();}
        if(peer.Status==PeerStatus.RoundResult){if(networkResultRound!=peer.Round){networkResultRound=peer.Round;screenTime=0;}if(screenTime>2&&networkHost)peer.ContinueMatch();}
        if(peer.Status==PeerStatus.MatchOver&&screen is not("results" or "network_rematch")){FinishMatch();return;}UpdateArena();
    }
}
