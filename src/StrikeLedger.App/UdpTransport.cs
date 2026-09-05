using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace StrikeLedger.App;

public enum PacketKind : byte { Hello=1, Commit=2, Reveal=3, Start=4, Inputs=5, Hash=6, Settlement=7, NextRound=8, Pause=9, Disconnect=10, Ack=11, Resume=12, Ping=13 }
public sealed record FaultProfile(int RoundTripMilliseconds=0,int JitterMilliseconds=0,int LossPercent=0,int DuplicatePercent=0,int ReorderPercent=0,int Seed=1);
public sealed record ReceivedPacket(PacketKind Kind,byte[] Payload);

/// <summary>Bounded two-endpoint UDP transport. Reliable control messages are acknowledged,
/// retransmitted, and deduplicated; gameplay inputs carry their own redundant history.</summary>
public sealed class UdpTransport : IDisposable
{
    const int Header=32, MaximumDatagram=4096;
    readonly Socket socket;
    readonly IPEndPoint remote;
    readonly byte[] session;
    readonly FaultProfile faults;
    readonly Random random;
    readonly Dictionary<uint,(byte[] Data,long Last,int Attempts)> pending=new();
    readonly HashSet<uint> seen=new();
    readonly Queue<uint> seenOrder=new();
    readonly List<(long Due,byte[] Data)> scheduled=new();
    uint reliableSequence=1,unreliableSequence=1,receiveSequence=1;
    readonly SortedDictionary<uint,ReceivedPacket> reorderBuffer=new();
    public int MalformedPackets { get; private set; }
    public int RejectedEndpoints { get; private set; }
    public int Retransmissions { get; private set; }
    public int PendingReliable => pending.Count;
    public long LastReceiveMilliseconds { get; private set; }=Environment.TickCount64;
    public event Action<ReceivedPacket>? Packet;
    public UdpTransport(IPEndPoint bind,IPEndPoint remoteEndpoint,string sessionId,FaultProfile? profile=null)
    {
        if(bind.Port is <=0 or >65535 || remoteEndpoint.Port is <=0 or >65535 || sessionId.Length is <1 or >128)throw new ArgumentException("Invalid private endpoint/session");
        remote=remoteEndpoint;
        session=SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)).Take(16).ToArray();
        faults=profile??new();
        if(faults.RoundTripMilliseconds is <0 or >2000 || faults.JitterMilliseconds is <0 or >500 || faults.LossPercent is <0 or >30 || faults.DuplicatePercent is <0 or >30 || faults.ReorderPercent is <0 or >50)throw new ArgumentException("Fault profile outside bounded laboratory range");
        random=new Random(faults.Seed);
        socket=new Socket(bind.AddressFamily,SocketType.Dgram,ProtocolType.Udp){Blocking=false};
        socket.Bind(bind);
    }
    public void Send(PacketKind kind,ReadOnlySpan<byte> payload,bool reliable=true)
    {
        if(kind==PacketKind.Ack || payload.Length>MaximumDatagram-Header || (kind==PacketKind.Inputs && payload.Length>992))throw new ArgumentException("Invalid bounded packet");
        if(reliable && pending.Count>=128)throw new InvalidOperationException("Reliable control queue full");
        uint id=reliable?reliableSequence++:0x80000000|unreliableSequence++;
        byte[] datagram=Encode(kind,payload,reliable,id);
        if(reliable)pending[id]=(datagram,Environment.TickCount64,1);
        Schedule(datagram);
    }
    byte[] Encode(PacketKind kind,ReadOnlySpan<byte> payload,bool reliable,uint id)
    {
        byte[] bytes=new byte[Header+payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes,0x314c5355);
        session.CopyTo(bytes,4); bytes[20]=(byte)kind; bytes[21]=reliable?(byte)1:(byte)0;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24),id);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28),payload.Length);
        payload.CopyTo(bytes.AsSpan(Header)); return bytes;
    }
    void Schedule(byte[] bytes)
    {
        if(scheduled.Count>=4096)throw new InvalidOperationException("UDP schedule exceeded bound");
        if(random.Next(100)<faults.LossPercent)return;
        int delay=Math.Max(0,faults.RoundTripMilliseconds/2+random.Next(-faults.JitterMilliseconds,faults.JitterMilliseconds+1));
        if(random.Next(100)<faults.ReorderPercent)delay+=random.Next(10,61);
        scheduled.Add((Environment.TickCount64+delay,bytes));
        if(random.Next(100)<faults.DuplicatePercent)scheduled.Add((Environment.TickCount64+delay+3,bytes));
    }
    public void Poll()
    {
        long now=Environment.TickCount64;
        foreach(var entry in pending.ToArray())
        {
            if(now-entry.Value.Last<Math.Max(100,faults.RoundTripMilliseconds*2+40))continue;
            if(entry.Value.Attempts>=100)throw new IOException("Private peer did not acknowledge reliable control");
            Retransmissions++;
            Schedule(entry.Value.Data); pending[entry.Key]=(entry.Value.Data,now,entry.Value.Attempts+1);
        }
        for(int n=scheduled.Count-1;n>=0;n--)
        {
            if(scheduled[n].Due>now)continue;
            try{socket.SendTo(scheduled[n].Data,remote);}catch(SocketException ex)when(ex.SocketErrorCode is SocketError.WouldBlock or SocketError.NoBufferSpaceAvailable){continue;}
            scheduled.RemoveAt(n);
        }
        byte[] buffer=new byte[65536];
        for(int n=0;n<256;n++)
        {
            EndPoint source=new IPEndPoint(remote.Address,0); int length;
            try{length=socket.ReceiveFrom(buffer,ref source);}catch(SocketException ex)when(ex.SocketErrorCode is SocketError.WouldBlock or SocketError.ConnectionReset){break;}
            if(!source.Equals(remote)){RejectedEndpoints++;continue;}
            if(length<Header || length>MaximumDatagram || BinaryPrimitives.ReadUInt32LittleEndian(buffer)!=0x314c5355 || !CryptographicOperations.FixedTimeEquals(buffer.AsSpan(4,16),session) || buffer[22]!=0 || buffer[23]!=0 || buffer[21]>1 || BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(28))!=length-Header || !Enum.IsDefined(typeof(PacketKind),buffer[20])){MalformedPackets++;continue;}
            var kind=(PacketKind)buffer[20];uint id=BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(24));
            if(kind==PacketKind.Ack){if(length!=Header){MalformedPackets++;continue;}pending.Remove(id);LastReceiveMilliseconds=now;continue;}
            if(id==0 || (kind==PacketKind.Inputs && length>1024)){MalformedPackets++;continue;}
            LastReceiveMilliseconds=now;
            if(buffer[21]==1)
            {
                Schedule(Encode(PacketKind.Ack,Array.Empty<byte>(),false,id));
                if(!seen.Add(id))continue;
                seenOrder.Enqueue(id);if(seenOrder.Count>4096)seen.Remove(seenOrder.Dequeue());
                if(id<receiveSequence)continue;
                if(id>receiveSequence+128 || reorderBuffer.Count>=128){MalformedPackets++;continue;}
                reorderBuffer[id]=new(kind,buffer.AsSpan(Header,length-Header).ToArray());
                while(reorderBuffer.Remove(receiveSequence,out var ordered)){receiveSequence++;Packet?.Invoke(ordered);}
                continue;
            }
            Packet?.Invoke(new(kind,buffer.AsSpan(Header,length-Header).ToArray()));
        }
    }
    public void Dispose()=>socket.Dispose();
}
