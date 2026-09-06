using Godot;
using System.Diagnostics;
public partial class Main
{
    bool quitting;
    public override void _Notification(int what){if(what==NotificationWMCloseRequest)QuitGame();}
    async void QuitGame(int code=0)
    {
        if(quitting)return;quitting=true;paused=true;LeavePrivateMatch("Game closed");
        StrikeLedger.Presentation.RuntimeProfiler.Save(evidenceDir);
        arena?.ShutdownAudio();
        // Audio playback is asynchronous even with accelerated fixed-fps simulation.
        // Give the audio server real time to retire stopped WAV playback before teardown.
        var grace=Stopwatch.StartNew();
        while(grace.Elapsed.TotalMilliseconds<350)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        GC.Collect();GC.WaitForPendingFinalizers();
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(code);
    }
}
