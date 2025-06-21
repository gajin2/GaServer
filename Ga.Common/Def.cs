namespace Ga.Common;

public static class Def
{
    public const string AdoInvariant = "Npgsql";

    public const int GameStateLogin = 0;
    public const int GameStateGame = 1;
    public const int GameStateClose = 2;

    public const int ProtoByteLen = 4;
    public const int WorldProtoMax = 200; // [0, 200)
}