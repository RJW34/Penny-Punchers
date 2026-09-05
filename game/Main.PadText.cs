using Godot;
public partial class Main
{
    string padText="",padLabel="";Action<string>? padTextDone;bool padNumeric;
    void PadTextEditor(string label,string value,Action<string> done,bool numeric=false){padText=value;padLabel=label;padTextDone=done;padNumeric=numeric;DrawPadText();}
    void DrawPadText(){Clear("pad_text");Heading("Controller text entry",padLabel.ToUpperInvariant(),"Choose characters with the D-pad and A. The keyboard remains available on the connection screen.");Panel(55,174,1166,65);Text(padText+"▌",76,185,30,Gold);string chars=padNumeric?"0123456789.:abcdef":"abcdefghijklmnopqrstuvwxyz0123456789.-_";for(int i=0;i<chars.Length;i++){char c=chars[i];Button(c.ToString(),55+i%10*117,277+i/10*57,103,()=>{if(padText.Length<64)padText+=c;DrawPadText();},i==0,24);}Button("Backspace",55,535,276,()=>{if(padText.Length>0)padText=padText[..^1];DrawPadText();});Button("Clear",352,535,276,()=>{padText="";DrawPadText();});Button("Done →",946,535,276,()=>{padTextDone?.Invoke(padText);NetworkMenu();});Back(NetworkMenu);}
}
