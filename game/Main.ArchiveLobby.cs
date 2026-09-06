using Godot;
using StrikeLedger.Core;
using StrikeLedger.App;
using System.Text.Json;
public partial class Main
{
    ReplaySaveResult lastReplaySave=new(false,"","No recording to save");
    int replayPage;
    ReplaySaveResult SaveReplay()
    {
        ReplayRecord? record;try{record=peer?.CaptureReplay()??recorder?.Record;}catch(Exception error){return lastReplaySave=new(false,"",error.Message);}
        if(record==null)return lastReplaySave=new(false,"","No new recording is available");
        string directory=ProjectSettings.GlobalizePath("user://replays");
        lastReplaySave=ReplayStore.SaveUnique(record,directory,peer==null?"match":"private");
        if(lastReplaySave.Success)
        {
            try{ExportEconomyFiles(lastReplaySave.Path,record);if(smoke)ReplayFormat.Save(record,System.IO.Path.Combine(evidenceDir,"full-match.replay.json"));}
            catch(Exception error){Toast("Replay saved; extra report could not be written: "+error.Message);}
        }
        else Toast(lastReplaySave.Message+". The recording is retained in memory; retry when storage is available.");
        return lastReplaySave;
    }
    void ReplayMenu()
    {
        paused=true;Clear("replays");Heading("Match archive","RUN THE TAPE.","Newest recordings first. Playback verifies actual inputs, transactions and state.");
        try
        {
            string directory=ProjectSettings.GlobalizePath("user://replays");
            var entries=ReplayStore.List(directory,content.ContentHash);
            int pages=Math.Max(1,(entries.Count+7)/8);replayPage=Math.Clamp(replayPage,0,pages-1);
            if(entries.Count==0)Text("Completed and saved matches appear here.",56,206,24,Muted);
            int n=0;
            foreach(var entry in entries.Skip(replayPage*8).Take(8))
            {
                string friendly=System.Text.RegularExpressions.Regex.Replace(entry.Label,@"\b(rook|vale)\b",match=>FighterName(match.Value));
                string label=$"{entry.CreatedUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss} / {friendly}";
                if(entry.Error.Length>0)label+=" / unavailable: "+entry.Error;
                string path=entry.Path;
                var replayButton=Button(label,56,190+n*48,1165,()=>
                {
                    try{StartReplay(ReplayFormat.Load(path,content.ContentHash));}
                    catch(Exception error){Toast("Replay rejected: "+error.Message);}
                },n++==0,16);
                replayButton.SetMeta("replay_path",path);
            }
            if(replayPage>0)Button("← Newer",56,598,320,()=>{replayPage--;ReplayMenu();});
            Text($"PAGE {replayPage+1} / {pages} · {entries.Count} RECORDINGS",404,607,17,Muted);
            if(replayPage+1<pages)Button("Older →",854,598,366,()=>{replayPage++;ReplayMenu();});
        }
        catch(Exception error){Text("Archive unavailable: "+error.Message,56,213,23,Muted,1120);}
        Back();
    }
    void NetworkMenu(){Clear("network_menu");Heading("Direct connection","PRIVATE 1V1.","Trusted LAN or existing private link. Each player chooses their own fighter in the shared lobby.");Text("Opponent IP",56,199,22);var address=new LineEdit{Text=networkAddress,Position=new(340,190),Size=new(596,48)};address.TextChanged+=v=>networkAddress=v;ui.AddChild(address);Button("Edit with pad",965,190,260,()=>PadTextEditor("Opponent IP",networkAddress,v=>networkAddress=v,true),false,18);Text("UDP base port",56,266,22);var port=new LineEdit{Text=networkPort,Position=new(340,257),Size=new(250,48)};port.TextChanged+=v=>networkPort=v;ui.AddChild(port);Button("Edit with pad",965,257,260,()=>PadTextEditor("UDP base port",networkPort,v=>networkPort=v,true),false,18);Text("Match phrase",56,333,22);var code=new LineEdit{Text=networkCode,Position=new(340,324),Size=new(596,48)};code.TextChanged+=v=>networkCode=v;ui.AddChild(code);Button("Edit with pad",965,324,260,()=>PadTextEditor("Match phrase",networkCode,v=>networkCode=v),false,18);Button("Host / P1 →",56,490,418,()=>StartNetwork(true),true);Button("Join / P2 →",502,490,418,()=>StartNetwork(false));Text("Two-frame input delay with rollback. No account needed.\nHost listens on the base port; join listens on base port + 1.\nBoth players enter their opponent’s IP and the same phrase.",56,550,17,Muted);Back();}
}
