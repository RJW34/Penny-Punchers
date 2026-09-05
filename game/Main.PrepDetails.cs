using Godot;
using StrikeLedger.Core;
public partial class Main
{
    void SetCpuDraft(){if(sim==null||bot1==null)return;if(freeKitEvidence){floor[0]=sim.Players[0].Credits;floor[1]=sim.Players[1].Credits;return;}var plan=bot1.ChoosePreparation(sim);floor[1]=plan.ReserveFloor;var list=ItemsFor(1);for(int slot=0;slot<3;slot++)for(int option=0;option<2;option++)if(plan.ItemIds.Contains(list[slot*2+option].GetProperty("id").GetString()))draft[1][slot]=option;}
    void DrawLeaseSummary(){if(sim==null)return;for(int i=0;i<2;i++){float x=i==0?42:736;string leases=sim.Players[i].Leases.Count==0?"BASE KIT":string.Join(" / ",sim.Players[i].Leases.Select(id=>content.Items[id].Name));DrawString(font,new(x,145),leases.ToUpperInvariant(),HorizontalAlignment.Left,502,11,Muted);}}
    string LeasePreview(int seat,int slot,int selected){if(selected<0)return "No lease · all ordinary attacks remain available";var item=ItemsFor(seat)[slot*2+selected];var m=MoveJson(fighter[seat],item.GetProperty("move_id").GetString()!);return (m?.GetProperty("command").GetString()??"")+" · one round · free activation";}
}
