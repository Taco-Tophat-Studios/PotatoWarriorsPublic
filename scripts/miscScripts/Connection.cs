using Godot;
using System;

public partial class Connection : Node
{
    public static ulong lastActivatedFrame = 0; /*When a connection event is triggered such that it activates a neuron with a
    //connection that uses a condition, the second one might call immediately after, because Godot sends signals 
    //BEFORE the process function(s) (or at the very end of the frame, after MoveAndSlide), so when checking connections 
    //in there, it will activate in this case. Therefore, this is used in the activate next function such that it will
    only activate another neuron if the current frame is greater than what had previously been stored. (BTW, there's 
    absolutely no need to worry about the frame count overflowing, because it would take 8.5e13 hours, which is more 
    than double the age of the universe. Only absolute cretins [Cough CoMAXugh Cough] would basement dwell that long)*/
    public Func<bool> logic = () => {return false; }; //by default so no null errors
    public bool condition
    {
        get { return logic(); }
        //no set because it will result in infinite recursion or something idk
    }
    public Neuron source;
    public Neuron destination;
    public enemyBase enemy;
    /// <summary>
    /// Assigns Connection details (for EVENT Connections). Not in constructor so connections and neurons can be 
    /// made at any time in any order without worry about reference variables. This particular Construct method 
    /// makes the connection be activated by an event. 
    /// </summary>
    /// <param name="d">the destination Neuron.</param>
    /// <param name="adder">Anonymous function for linking an event to this connection, because events cannot be 
    /// passed in as parameters, an anonymous. For example, use something like 
    /// "(Action a) => { CharSprite.AnimationFinished += a; }" 
    /// as the parameter, with "CharSprite.AnimationFinished" as the event. Action a can be any Action variable.</param>
    public void ConstructConnection(Neuron d, Action<Action> adder)  //DO NOT just change the last parameter to be an action, and add the connection's ActivateNext wherever it is being constructed. It will add the ActivateNext of the PREVIOUSLY DEFINED connection, so it will not work (e. g., connection c = new connection; c = new connection(c.activatenext) will give activatenext of an empty c, and throw an error. Also, congratulations on reading this far to the side, your reward is this: Nothing! Yay!
    {
        destination = d;
        adder(ActivateNext); //this will be a function adding the destination neutron's method to the action of desire, since you can't pass an event as a parameter for literally no reason
    }

    public void ConstructConnection(Neuron d, Action triggerEvent) {
        destination = d;
        triggerEvent += ActivateNext;
    }
    /// <summary>
    /// Assigns Connection details (for CONDITIONAL Connections). Not in constructor so connections and neurons can be 
    /// made at any time in any order without worry about reference variables. This particular Construct method 
    /// makes the connection be activated by an event. 
    /// </summary>
    /// <param name="d">the destination Neuron.</param>
    /// <param name="l">The function represnting the logic to activate this connection, and thefore the next Neuron.
    /// For example, use something like "() => { return xDist < 512; }" as the parameter, with "xDist < 512" as the logic.</param>
    public void ConstructConnection(Neuron d, Func<bool> l)
    {
        logic = l;
        destination = d;
    }
    /// <summary>
    /// Connection class, used to connect Neurons. There are Conditional Connections, and Event Connections. Conditional
    /// Connections use logic which is checked for each one in the source Neuron's CheckConnections() method, meant to
    /// be called every frame, with a particular connection activating if the logic is true. Event Connections will 
    /// activate when the event they are linked to is risen. NOTE: this means the event should trigger methods with no
    /// parameters. If this is untrue, try to make a custom event triggered by the original, such that these can be
    /// triggered.
    /// </summary>
    public Connection() //to create a sole one (temporarily)
    {

    } 
    /// <summary>
    /// Executes when the Connection is activated, either when the condition is evaluated to true in the source Neuron's
    /// CheckConnections() method, or when the event linked to this connection is raised. NOTE: Simply getting the condition
    /// does NOT trigger this method is true, because getters/setters break Godot, and because somebody may want to check
    /// it without triggering the next Neuron.
    /// </summary>
    public void ActivateNext()
    {
        if (enemy.currentNeuron == source && !enemy.dead && lastActivatedFrame + 1 < Engine.GetPhysicsFrames())  //WARNING: when enemy dies and is deleted, currentNeuron may try to access said enemy, which would be Null! Do something about it!
        {
            GD.Print("Activating Neuron with param " + destination.parameter + " On frame " + Engine.GetPhysicsFrames() + " With Special Flag " + destination.specialDebugFlag);
            lastActivatedFrame = Engine.GetPhysicsFrames();
            enemy.currentNeuron = destination;
            destination.ExecuteMethod();
        }
    }
}
