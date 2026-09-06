using Godot;
using StrikeLedger.Core;
public partial class Main
{
    void DrawReplayDetails()
    {
        if(activeReplayRecord==null||replay==null)return;
        int idx=Math.Min(replay.CommandIndex-1,activeReplayRecord.Commands.Count-1);
        while(idx>=0&&activeReplayRecord.Commands[idx].Kind!="step")idx--;
        if(showReplayInputs&&idx>=0){var c=activeReplayRecord.Commands[idx];DrawRect(new Rect2(26,177,470,64),new Color(.025f,.045f,.06f,.94f));DrawString(font,new(40,202),$"P1 / {c.Direction0}  {c.Buttons0}",HorizontalAlignment.Left,-1,16,Gold);DrawString(font,new(40,226),$"P2 / {c.Direction1}  {c.Buttons1}",HorizontalAlignment.Left,-1,16,Cyan);}
        if(showReplayReceipts){var receipts=activeReplayRecord.Commands.Take(Math.Max(0,replay.CommandIndex)).SelectMany(c=>c.Events).Where(e=>e.Kind is CombatEventKind.SuperUseConsumed or CombatEventKind.SkillAward).TakeLast(4).ToArray();if(receipts.Length>0){DrawRect(new Rect2(26,462,490,137),new Color(.025f,.045f,.06f,.94f));DrawString(font,new(40,486),"CONFIRMED SUPER USE / SKILL",HorizontalAlignment.Left,-1,13,Cream);for(int i=0;i<receipts.Length;i++){var d=receipts[i];DrawString(font,new(40,511+i*24),$"{d.Tick} / P{d.Seat+1} {d.MoveId} {d.Kind} {d.Value}",HorizontalAlignment.Left,-1,14,d.Seat==0?Gold:Cyan);}}}
    }
}
