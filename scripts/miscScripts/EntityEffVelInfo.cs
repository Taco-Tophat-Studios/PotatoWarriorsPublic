using Godot;
using System;
using System.Collections.Generic;

//NOTE: this class is only used for Syncing effect velocities across the network, due to Godot's inability to sync Lists, Tuples, etc. directly

//[GlobalClass]
public partial class EntityEffVelInfo
{
    public Vector2 effVel {get; set; } = new Vector2(0,0);
    public string[] eVC_Names { get; set; } = { };
    public Vector2[] eVC_Velocities { get; set; } = { };
    public bool[] eVC_IncludeInTotals { get; set; } = {};
}
