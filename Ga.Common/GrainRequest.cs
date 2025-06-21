namespace Ga.Common;

[GenerateSerializer]
public class GrainRequest
{
    [Id(0)]
    public int NetId { get; set; }
    [Id(1)]
    public int ServerId { get; set; }
    [Id(2)]
    public byte[] Payload { get; set; } = [];
    [Id(3)]
    public string OpenId { get; set; } = string.Empty;
    [Id(4)]
    public int Proto { get; set; }
}