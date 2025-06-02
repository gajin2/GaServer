using Ga.Common;

namespace Ga.Center;

public sealed class CenterMsg
{
    public CenterMsgId MsgId;
    public object? Body;
    public NetSession? Session;
}