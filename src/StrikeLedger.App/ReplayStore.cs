using System.Text.Json;
using StrikeLedger.Core;

namespace StrikeLedger.App;

public sealed record ReplaySaveResult(bool Success,string Path,string Error)
{
    public string Message=>Success?"Replay saved: "+System.IO.Path.GetFileName(Path):"Replay not saved: "+Error;
}
public sealed record ReplayArchiveEntry(string Path,DateTime CreatedUtc,string Label,bool Complete,string Error);

/// <summary>Archive metadata is outside deterministic competitive input records.</summary>
public static class ReplayStore
{
    sealed class HeaderOnly { public ReplayHeader? Header {get;set;} }
    sealed record Metadata(string RecordingId,DateTime CreatedUtc,string Label,bool Complete);

    public static void WriteAtomically(string path,ReadOnlySpan<byte> bytes)
    {
        string full=System.IO.Path.GetFullPath(path),directory=System.IO.Path.GetDirectoryName(full)!;
        Directory.CreateDirectory(directory);
        string temporary=System.IO.Path.Combine(directory,"."+System.IO.Path.GetFileName(full)+"."+Guid.NewGuid().ToString("N")+".tmp");
        try
        {
            using(var file=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {file.Write(bytes);file.Flush(true);}
            // The temporary lives in the destination directory, so publication is
            // a same-filesystem rename. Existing bytes survive any earlier failure.
            File.Move(temporary,full,true);
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }

    public static ReplaySaveResult SaveUnique(ReplayRecord record,string directory,string prefix="match")
    {
        string path="";
        try
        {
            if(prefix.Length is <1 or >24||prefix.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c!='-'))throw new ArgumentException("Invalid recording prefix");
            string id=Guid.NewGuid().ToString("N");DateTime now=DateTime.UtcNow;
            path=System.IO.Path.Combine(directory,$"{prefix}-{now:yyyyMMdd-HHmmss-fffffff}-{id}.json");
            ReplayFormat.Save(record,path);
            var last=record.Commands.LastOrDefault();
            bool complete=last?.Settlement is {MatchDecision: not "CONTINUE"};
            string label=$"{record.Header.Config.Fighter0} / {record.Header.Config.Fighter1} · {prefix}";
            try{WriteAtomically(System.IO.Path.ChangeExtension(path,"meta.json"),JsonSerializer.SerializeToUtf8Bytes(new Metadata(id,now,label,complete)));}
            catch(IOException){/* The immutable replay remains valid without optional archive metadata. */}
            catch(UnauthorizedAccessException){ }
            return new(true,path,"");
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException or JsonException)
        {return new(false,path,ex.Message);}
    }

    public static IReadOnlyList<ReplayArchiveEntry> List(string directory,string contentHash)
    {
        if(!Directory.Exists(directory))return [];
        var entries=new List<ReplayArchiveEntry>();
        foreach(string path in Directory.EnumerateFiles(directory,"*.json"))
        {
            if(path.EndsWith(".economy.json",StringComparison.OrdinalIgnoreCase)||path.EndsWith(".meta.json",StringComparison.OrdinalIgnoreCase)||System.IO.Path.GetFileName(path)=="economy-export.json")continue;
            DateTime created=File.GetLastWriteTimeUtc(path);string label=System.IO.Path.GetFileNameWithoutExtension(path),error="";bool complete=false;
            try
            {
                var info=new FileInfo(path);if(info.Length>ReplayFormat.MaxBytes)throw new InvalidDataException("Replay exceeds size limit");
                using var stream=File.OpenRead(path);
                // Stream-skip commands for listing instead of allocating their full history.
                var header=JsonSerializer.Deserialize<HeaderOnly>(stream,new JsonSerializerOptions{MaxDepth=16})?.Header;
                if(header is null||header.Config is null)throw new InvalidDataException("Missing replay header");
                if(header.Version<ReplayFormat.Version)throw new InvalidDataException("Legacy direct-spend replay: incompatible with shop-only v2; original file retained");
                if(header.Version!=ReplayFormat.Version||header.Build!=ReplayFormat.Build||header.ContentHash!=contentHash)throw new InvalidDataException("Different build or trial content; choose the recording's trial");
                label=$"{header.Config.Fighter0} / {header.Config.Fighter1}";
                string metadata=System.IO.Path.ChangeExtension(path,"meta.json");
                if(File.Exists(metadata)&&new FileInfo(metadata).Length<16384)
                {var m=JsonSerializer.Deserialize<Metadata>(File.ReadAllText(metadata));if(m is not null){created=m.CreatedUtc;complete=m.Complete;label=m.Label;}}
            }
            catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or ArgumentException){error=ex.Message;}
            entries.Add(new(path,created,label,complete,error));
        }
        return entries.OrderByDescending(e=>e.CreatedUtc).ThenBy(e=>e.Path,StringComparer.Ordinal).ToArray();
    }
}
