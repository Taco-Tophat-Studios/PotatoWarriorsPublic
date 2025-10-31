using Godot;
using System;
using System.Collections.Generic;

//This class is used to hold various methods that may need to be synced across the network. 
//Because Callables can't be serialized (for some reason), I'm storing them here to be referenced by name
public partial class SyncedCallables : Node
{
    public enum Names
    {
        BilliardFunc,
    }
    //to make absolutely sure the string is correct
    public static Dictionary<Names, string> NameLookup = new Dictionary<Names, string>()
    {
        { Names.BilliardFunc, nameof(BilliardFunc) },
    };
    //all must have form: (Node2D caller, string modifier, Vector2 effectVelocity, bool includeEffectVelocityBase) => Vector2
    public Func<Node2D, string, Vector2, bool, Vector2> BilliardFunc = (Node2D c, string _, Vector2 eV, bool _) =>
    {
        return eV - 2 * c.Position.Normalized().Dot(eV) * c.Position.Normalized(); //billiard ball bounce (Reverse component of eV in direction of c.Position)
    };
}
