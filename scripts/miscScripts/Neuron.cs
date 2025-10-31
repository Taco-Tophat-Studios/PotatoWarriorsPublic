using Godot;
using System;
using System.Collections.Generic;

public partial class Neuron : Node
{
    /*NOTE: if weird null errors are being made when setting up stuff, try construction the connections of a neuron,
    then the neuron that is the source of all of them.*/
    public Action<int> MethodToExecute;
    public string specialDebugFlag = "UNDEFINED";
    public int parameter;
    public List<Connection> Connections;
    public enemyBase enemy;
    /// <summary>
    /// Assigns Neutron Details. Not in constructor so connections and neurons can be made at any time in any order 
    /// without worry about reference variables.
    /// </summary>
    /// <param name="method">Method to execute. Needs an integer parameter so different parts of one behavior can be 
    /// grouped into one method.</param>
    /// <param name="par">Integer parameter for the method to be called.</param>
    /// <param name="conns">List of Connections with this Neuron as the SOURCE.</param>
    /// <param name="enemy">The enemy with the Neuron (just use "this"). Included so it can set the enemy's current Neuron.</param>
    public void ConstructNeuron(Action<int> method, int par, List<Connection> conns, enemyBase enemy) //not a constructor to avoid weird ref var stuff with "new" statement
    {
        MethodToExecute = method;
        parameter = par;
        Connections = conns;
        this.enemy = enemy;
        foreach (Connection c in conns)
        {
            c.source = this;
            c.enemy = enemy;
        }
    }
    /// <summary>
    /// Neuron class. Represents a method to be called when activated (NOT every frame), and connections to other neurons, 
    /// that activate when they are triggered. For methods that call every frame, make a connection back to the Neuron.
    /// NOTE: Only one neuron, and its connections, can be activated or will be checked (depending on the type).
    /// </summary>
    public Neuron()
    {

    }
    /// <summary>
    /// Sets the enemy's current Neuron to this one, and Executes this Neuron's method with the specific parameter.
    /// </summary>
    public void ExecuteMethod()
    {
        enemy.currentNeuron = this;
        MethodToExecute(parameter);
    }
    /// <summary>
    /// To be called whenever necessary (usually every frame) in the enemy's Update() or FixedUpdate() method. 
    /// If conditional connections are not being checked, this not being included is likely why (unless the checking 
    /// is meant to be periodic).
    /// </summary>
    public void CheckConnections()
    {
        foreach (Connection c in Connections)
        {
            if (c.condition)
            {
                c.ActivateNext();
                return;
            }
        }
    }
}
