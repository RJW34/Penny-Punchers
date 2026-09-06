using Godot;
using StrikeLedger.App;
public partial class Main
{
    double replayAccumulator;bool showWalletGraph,showReplayInputs=true,showReplayReceipts=true;ReplayRecord? activeReplayRecord;
    string replayFailure="";
    void StartReplay(ReplayRecord record)
    {
        var next=new ReplayPlayer(content,record);
        LeavePrivateMatch("Opened replay");activeReplayRecord=record;replay=next;sim=next.Simulation;recorder=null;lab=null;mode="replay";
        fighter=new[]{sim.Config.Fighter0,sim.Config.Fighter1};art=new[]{0,0};
        stageId=sim.Config.StageId;replayAccumulator=0;frameStep=false;inputGrace=0;paused=false;replayFailure="";
        foreach(var history in inputHistory)history.Clear();ResetPresentationTimeline();Clear("fight");
    }
    bool ReplayOperation(Action operation,bool resetTimeline=false)
    {
        if(replay==null||replayFailure.Length>0)return false;
        try{operation();if(resetTimeline){replayAccumulator=0;ResetPresentationTimeline();foreach(var history in inputHistory)history.Clear();}return true;}
        catch(Exception error)
        {
            paused=true;frameStep=false;replayAccumulator=0;replayFailure=error.Message;ResetPresentationTimeline();
            Clear("replay_failed");Heading("Replay verification stopped","RECORDING REJECTED.","A later command failed integrity verification. Playback is stopped at the first failure.");
            Text($"Command {replay.CommandIndex} / tick {sim?.Tick}\n{error.Message}",56,235,22,Muted,1150);
            Button("Replay archive →",56,375,540,ReplayMenu,true);Button("Restart verification",666,375,555,()=>{if(activeReplayRecord!=null)StartReplay(activeReplayRecord);});Back(Title);
            GD.PrintErr("REPLAY_REJECTED "+error.Message);return false;
        }
    }
    void AdvanceReplay()
    {
        if(replay==null||replayFailure.Length>0)return;replayAccumulator+=replay.Speed;
        while(replayAccumulator>=1){replayAccumulator--;bool advanced=false;if(!ReplayOperation(()=>advanced=replay.FrameAdvance()))return;if(!advanced){paused=true;Toast("End of verified replay. Open playback controls to seek.");break;}ObserveChanges();}
    }
    void SeekReplay(long tick){if(ReplayOperation(()=>replay!.Seek(tick),true))Resume();}
    void ReplayControls()
    {
        if(replayFailure.Length>0){ReplayMenu();return;}paused=true;Clear("replay_controls");Heading("Playback controls","FOLLOW THE MONEY.","Seek restores the simulation, wallet, and confirmed transactions together.");
        Button("Resume →",55,191,555,Resume,true);Button("Frame advance",665,191,555,()=>{if(ReplayOperation(()=>replay!.FrameAdvance())){ObserveChanges();Clear("fight");paused=true;}});
        Button("Back 5 seconds",55,251,555,()=>SeekReplay(Math.Max(0,sim!.Tick-300)));Button("Forward 5 seconds",665,251,555,()=>SeekReplay(sim!.Tick+300));Button("Restart replay",55,311,555,()=>SeekReplay(0));
        Button($"Speed {replay?.Speed:0.0}× ↔",665,311,555,()=>{replay!.Speed=replay.Speed==.5?1:replay.Speed==1?2:.5;ReplayControls();});
        Button("Wallet graph: "+(showWalletGraph?"on":"off"),55,371,555,()=>{showWalletGraph=!showWalletGraph;ReplayControls();});Button("Replay archive",665,371,555,ReplayMenu);
        Button("Inputs: "+(showReplayInputs?"on":"off"),55,431,555,()=>{showReplayInputs=!showReplayInputs;ReplayControls();});Button("Hitboxes: "+(debugBoxes?"on":"off"),665,431,555,()=>{debugBoxes=!debugBoxes;ReplayControls();});
        Button("Use / skill receipts: "+(showReplayReceipts?"on":"off"),55,491,555,()=>{showReplayReceipts=!showReplayReceipts;ReplayControls();});Button("Export round economy",665,491,555,()=>{try{if(activeReplayRecord!=null){ExportEconomyFiles(ProjectSettings.GlobalizePath("user://replays/economy-export.json"),activeReplayRecord);Toast("Economy JSON, CSV and round table exported.");}}catch(Exception error){Toast("Economy export failed: "+error.Message);}});
        Button("Practice this moment",55,551,555,()=>{if(sim==null)return;try{ClearPracticeSelection();lab=TrainingSession.FromReplay(sim,77);BeginLab();Toast("Detached training branch. Competitive replay remains unchanged.");}catch(Exception error){Toast(error.Message);}});Text("Replay practice is separate and cannot export as a competitive match.",665,569,17,Muted,555);Back(Title);
    }
    void DrawReplayOverlay(){if(mode!="replay"||replay==null)return;DrawString(font,new(38,653),$"REPLAY / {replay.Speed:0.0}× / TICK {sim?.Tick} / {(paused?"PAUSED":"VERIFIED PLAYBACK")}",HorizontalAlignment.Left,-1,15,Gold);DrawReplayDetails();if(!showWalletGraph)return;var graph=replay.WalletGraph;if(graph.Count<2)return;DrawRect(new Rect2(740,452,500,183),new Color(.025f,.045f,.06f,.92f));long max=Math.Max(1,graph[^1].Tick);int cap=content.Economy.Cap;for(int seat=0;seat<2;seat++){for(int i=1;i<graph.Count;i++){var a=graph[i-1];var b=graph[i];int ca=seat==0?a.Seat0:a.Seat1,cb=seat==0?b.Seat0:b.Seat1;DrawLine(new Vector2(756+(float)a.Tick/max*464,612-(float)ca/cap*130),new Vector2(756+(float)b.Tick/max*464,612-(float)cb/cap*130),seat==0?Gold:Cyan,2);}}DrawString(font,new(756,478),$"RECORDED WALLET / 0–{cap} CR",HorizontalAlignment.Left,-1,13,Cream);}
}
