using Godot;

namespace StrikeLedger.Presentation;

public partial class ArenaView
{
    // Original vector markers complement the existing effect atlas. Their phase,
    // position and pips are copied from Core; this code never invents contacts.
    void DrawFields(Node2D canvas)
    {
        foreach(var field in _state.Fields)
        {
            Color color=field.Owner==0?_amber:_cyan;
            var ground=WorldToScreen(field.X,0);
            var center=WorldToScreen(field.X,field.Y);
            float zoom=_framing.Zoom;
            if(field.Kind=="anchor")
            {
                float radius=Math.Max(12,field.MarkerWidth*CameraFrame.BaseUnits*.6f)*zoom;
                float arm=field.ArmingTicks==0?1:Math.Clamp(field.Age/(float)field.ArmingTicks,0,1);
                var marker=new Rect2(ground-new Vector2(radius,Math.Max(6,field.MarkerHeight*Units)),new Vector2(radius*2,Math.Max(6,field.MarkerHeight*Units)));
                canvas.DrawRect(marker,new Color(_ink,.9f));canvas.DrawRect(marker,new Color(color,field.Armed?1:.5f),false,2);
                canvas.DrawLine(marker.Position+new Vector2(3,3),marker.Position+new Vector2(3+(marker.Size.X-6)*arm,3),color,2);
                float r=field.TriggerRadius*Units;
                if(field.Armed)
                    for(int i=0;i<12;i++)canvas.DrawArc(ground,r,i*Mathf.Tau/12,(i+.45f)*Mathf.Tau/12,3,new Color(color,.4f),1.3f);
                if(field.Triggered)
                {
                    float trigger=1-Math.Clamp(field.TriggerTicks/(float)Math.Max(1,field.TriggerWindup),0,1);
                    canvas.DrawArc(ground,r*(.3f+.7f*trigger),Mathf.Pi,Mathf.Tau,18,new Color(color,.9f),2.5f);
                    canvas.DrawLine(ground+new Vector2(-r,-5),ground+new Vector2(r,-5),new Color(color,.7f),2);
                }
                ObjectLabel(canvas,ground+new Vector2(-48,-marker.Size.Y-10),field.Triggered?$"P{field.Owner+1} / TRIGGER {field.TriggerTicks}":field.Armed?$"P{field.Owner+1} / ARMED":$"P{field.Owner+1} / ARMING",color,110);
            }
            else if(field.Kind=="prism")
            {
                float w=Math.Max(12,field.Width*Units),h=Math.Max(18,field.Height*Units);
                var rect=new Rect2(center-new Vector2(w,h)/2,new Vector2(w,h));
                Color stroke=new(color,field.ContactCooldown>0?.42f:.95f);
                Vector2[] diamond=[new(center.X,rect.Position.Y),new(rect.End.X,center.Y),new(center.X,rect.End.Y),new(rect.Position.X,center.Y)];
                canvas.DrawColoredPolygon(diamond,new Color(color,_state.ReducedFlashes?.11f:.2f));
                canvas.DrawPolyline([..diamond,diamond[0]],stroke,2);
                canvas.DrawLine(new(center.X,rect.Position.Y),new(center.X,rect.End.Y),new Color(color,.45f),1);
                for(int pip=0;pip<field.HitsRemaining;pip++)
                    canvas.DrawRect(new Rect2(center.X-7+(pip-(field.HitsRemaining-1)/2f)*16,rect.Position.Y-19,12,8),color);
                ObjectLabel(canvas,new(center.X-50,rect.Position.Y-25),$"P{field.Owner+1} / PRISM",color,105);
            }
            if(_state.DebugBoxes)
            {
                bool contact=field.Kind!="anchor"||field.Triggered&&field.TriggerTicks==0;
                int w=contact?field.Width:field.MarkerWidth,h=contact?field.Height:field.MarkerHeight;
                var pos=contact?center:WorldToScreen(field.X,h/2);
                canvas.DrawRect(new Rect2(pos-new Vector2(w,h)*Units/2,new Vector2(w,h)*Units),new Color(contact?C("ef7261"):color,.9f),false,1.2f);
            }
        }
    }
    void DrawActorStatus(Node2D canvas,FighterRenderState fighter,int seat)
    {
        Color color=seat==0?_amber:_cyan;var p=WorldToScreen(fighter.X,fighter.Y+50000);
        if(fighter.ArmorActive)
        {
            float r=45*_framing.Zoom;
            canvas.DrawPolyline([p+new Vector2(-r,-r),p+new Vector2(-r,r),p+new Vector2(r,r),p+new Vector2(r,-r)],new Color(color,.7f),2);
        }
        if(fighter.CounterActive)
        {
            float angle=fighter.Facing<0?Mathf.Pi:0;
            canvas.DrawArc(p,49*_framing.Zoom,angle-1.1f,angle+1.1f,10,new Color(color,.7f),2);
        }
        if(fighter.DestinationX is int destination)
        {
            var end=WorldToScreen(destination,0);
            canvas.DrawArc(end,20*_framing.Zoom,Mathf.Pi,Mathf.Tau,12,new Color(color,.8f),2);
            canvas.DrawLine(end+new Vector2(0,-12),end,new Color(color,.8f),2);
            if(fighter.MoveId=="buy_v_g4_slipgate"&&fighter.ActionFrame>=fighter.Startup&&fighter.ActionFrame<fighter.Startup+fighter.Active)
            {
                int direction=Math.Sign(destination-fighter.X);if(direction==0)direction=-fighter.Facing;
                for(int line=0;line<3;line++)
                {
                    var center=p+new Vector2(0,(line-1)*15*_framing.Zoom);
                    canvas.DrawLine(center-new Vector2(direction*22*_framing.Zoom,0),center-new Vector2(direction*(52+line*7)*_framing.Zoom,0),new Color(color,.25f+line*.12f),2);
                }
            }
        }
    }
    void ObjectLabel(Node2D canvas,Vector2 position,string text,Color color,float width)
    {
        canvas.DrawRect(new Rect2(position+new Vector2(-4,-13),new Vector2(width+8,18)),new Color(_ink,.86f));
        canvas.DrawString(Font,position,text,HorizontalAlignment.Center,width,11,color);
    }
}
