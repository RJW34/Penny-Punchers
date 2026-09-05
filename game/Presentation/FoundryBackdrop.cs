using Godot;

namespace StrikeLedger.Presentation;

/// <summary>
/// Original foundry primitives rendered once into ArenaView's owned SubViewport.
/// Background camera/time are fixed cosmetic values; gameplay geometry is unchanged.
/// </summary>
internal partial class FoundryBackdrop : Node2D
{
    private const float _camera=384000, _time=0, Ground=604f, Units=1280f/480000f;
    private readonly Color _cream=C("ece7d4"), _amber=C("efa94c");
    private Font Font=>ThemeDB.FallbackFont;
    public override void _Draw()=>DrawFoundry();
    private Vector2 WorldToScreen(int x,int y)=>new(640+(x-_camera)*Units,Ground-y*Units);
    private void Poly(Vector2[] points,Color color)=>DrawColoredPolygon(points,color);
    private static Color C(string html)=>Color.FromHtml(html);

    private void DrawFoundry()
    {
        DrawRect(new Rect2(0, 0, 1280, 720), C("101b23"));
        float parallax = (_camera - 384000) * Units * .24f;
        // Six shallow colour fields keep the artwork crisp without a shader dependency.
        for (int i = 0; i < 12; i++) DrawRect(new Rect2(0, 140 + i * 30, 1280, 31), new Color(.072f + i*.003f, .12f + i*.002f, .147f + i*.001f));
        // High steel ceiling, cable races and warm clerestory windows.
        for (int bay = -1; bay < 7; bay++)
        {
            float x = bay * 248 - parallax;
            DrawRect(new Rect2(x + 20, 145, 188, 196), C("0c141b"));
            DrawRect(new Rect2(x + 28, 153, 172, 181), C("665344"));
            for (int col = 0; col < 4; col++)
            for (int row = 0; row < 3; row++)
            {
                float light = .65f + .15f * MathF.Sin(bay * 13 + col * 4 + row);
                DrawRect(new Rect2(x + 33 + col * 41, 157 + row * 57, 34, 51), new Color(.56f * light, .36f * light, .19f * light));
                DrawLine(new(x+34+col*41,158+row*57),new(x+65+col*41,158+row*57),C("c59153"),1);
            }
            Poly([new(x+55, 334),new(x+130,334),new(x+288,510),new(x+120,510)],new Color(.78f,.54f,.25f,.035f));
            DrawRect(new Rect2(x + 215, 97, 21, 421), C("24343a"));
            DrawRect(new Rect2(x + 217, 97, 4, 421), C("526163"));
            DrawRect(new Rect2(x + 230, 97, 8, 421), C("0b151b"));
            for (int bolt = 0; bolt < 5; bolt++) DrawCircle(new(x + 223, 167 + bolt*74),2,C("82908b"));
            DrawLine(new(x+6, 142),new(x+231,338),C("26383e"),7);
            DrawLine(new(x+231,142),new(x+6,338),C("26383e"),7);
        }
        DrawRect(new Rect2(0, 113, 1280, 27), C("0a131a"));
        DrawLine(new(0, 137), new(1280, 137), C("405058"),2);
        DrawLine(new(0, 107), new(1280, 107), C("4b5758"),2);
        for(int i=0;i<13;i++) DrawLine(new(i*110-parallax*.5f,113),new(i*110+45-parallax*.5f,139),C("344348"),3);
        // Exhibition gantry and a hanging original arena insignia.
        float signX = 640-parallax*.75f;
        DrawLine(new(signX-68,138),new(signX-68,177),C("798078"),2);
        DrawLine(new(signX+68,138),new(signX+68,177),C("798078"),2);
        Poly([new(signX-160,176),new(signX+160,176),new(signX+149,271),new(signX-149,271)],C("101c23"));
        DrawLine(new(signX-159,177),new(signX+159,177),_amber,3);
        DrawLine(new(signX-148,271),new(signX+148,271),C("73817b"),1);
        DrawString(Font,new(signX-123,211),"FOUNDRY RING",HorizontalAlignment.Left,-1,25,_cream);
        DrawString(Font,new(signX-121,239),"EXHIBITION   /   EST. 2086",HorizontalAlignment.Left,-1,12,C("8c9a94"));
        for(int i=0;i<5;i++) DrawRect(new Rect2(signX+93+i*7,250,3,9),_amber);
        DrawMachinery(116 - parallax*.7f, false);
        DrawMachinery(1152 - parallax*.7f, true);
        // Haze and the gallery: deliberately low contrast behind the combat plane.
        DrawRect(new Rect2(0,397,1280,134),new Color(.08f,.14f,.17f,.76f));
        for(int i=-1;i<42;i++)
        {
            float x=i*35-parallax*.42f;
            float h=14+(i*17%13);
            float bob=MathF.Sin(_time*.8f+i*2.7f)*.9f;
            DrawCircle(new(x,462-h+bob),6,C("17292f"));
            Poly([new(x-9,470-h),new(x+7,470-h),new(x+12,501),new(x-14,501)],C("17292f"));
            if(i%5==0) DrawLine(new(x-6,470-h),new(x-15,451-h),C("20363a"),4);
        }
        DrawRect(new Rect2(0,489,1280,41),C("0e1c23"));
        DrawRect(new Rect2(0,488,1280,4),C("70807a"));
        for(int i=-1;i<12;i++)
        {
            float x=i*134-parallax*.6f;
            DrawRect(new Rect2(x,490,4,37),C("4a5e5f"));
            DrawLine(new(x+4,501),new(x+114,501),C("31484d"),1);
            DrawLine(new(x+4,521),new(x+114,521),C("31484d"),1);
        }
        DrawRect(new Rect2(0,530,1280,190),C("29383b"));
        Poly([new(0,530),new(1280,530),new(1280,604),new(0,604)],C("48504a"));
        DrawLine(new(0,533),new(1280,533),C("71807a"),2);
        // The floor grid is in world space; camera motion never alters geometry.
        for(int i=-8;i<22;i++)
        {
            float x=640+(i*64000-_camera)*Units;
            DrawLine(new(x*.65f+224,532),new(x,720),new Color(.09f,.16f,.18f,.65f),2);
        }
        foreach(float y in new[]{551f,576f,604f,652f,710f}) DrawLine(new(0,y),new(1280,y),C("1d3035"),2);
        DrawLine(new(0,603),new(1280,603),C("c8b580"),2);
        DrawLine(new(0,609),new(1280,609),C("172b31"),3);
        float center=WorldToScreen(384000,0).X;
        Poly([new(center-194,570),new(center+194,570),new(center+221,599),new(center-221,599)],new Color(.84f,.79f,.59f,.07f));
        DrawLine(new(center-194,570),new(center+194,570),new Color(.87f,.81f,.62f,.22f),1);
        DrawString(Font,new(center-86,592),"S T R I K E   L E D G E R",HorizontalAlignment.Left,-1,12,new Color(.89f,.85f,.68f,.38f));
        // A few slow embers are atmosphere, not projectile hitboxes.
        for(int i=0;i<17;i++)
        {
            float x=(i*173.17f+_time*(4+i%4))%1360-40;
            float y=455-((_time*9+i*43)%260);
            DrawCircle(new(x,y),i%3==0?1.6f:.8f,new Color(.95f,.63f,.29f,.10f+(i%4)*.035f));
        }
        // Decorative footer is outside the fighting plane and reserved HUD region.
        DrawLine(new(30,674),new(134,674),C("61716c"),1);
        DrawString(Font,new(30,694),"07  /  INDUSTRIAL CIRCUIT",HorizontalAlignment.Left,-1,10,C("87948a"));
        DrawString(Font,new(1107,694),"NO HAZARDS",HorizontalAlignment.Left,-1,10,C("87948a"));
    }

    private void DrawMachinery(float x, bool mirrored)
    {
        DrawRect(new Rect2(x-58,339,116,135),C("111e25"));
        DrawRect(new Rect2(x-49,349,98,65),C("855238"));
        DrawRect(new Rect2(x-44,354,88,56),C("b46b3b"));
        for(int i=0;i<7;i++) DrawRect(new Rect2(x-41+i*13,354,5,56),C("432f27"));
        DrawRect(new Rect2(x-62,337,124,13),C("53615b"));
        DrawRect(new Rect2(x-60,413,120,12),C("3c4f51"));
        DrawRect(new Rect2(x-49,428,98,36),C("283b40"));
        for(int i=0;i<4;i++) DrawCircle(new(x-35+i*23,440),2,_amber);
        DrawLine(new(x-31,339),new(x-31,295),C("263b42"),16);
        DrawLine(new(x-31,297),new(x+(mirrored?89:-89),297),C("263b42"),16);
        DrawLine(new(x-33,339),new(x-33,294),C("566363"),3);
        DrawRect(new Rect2(x-40,306,20,7),C("7b8274"));
    }

}
