using Godot;
using System;
using System.Collections.Generic;

public partial class Timer : Node
{
    Timer AlmightyTimer; //seeing as it is omnipresent as an autoload node which directs all other timers, yeah,
                         //this name seems about right
    bool alertIfNoTimerFinishedMethod;
    public Timer(float t, bool a, Node p, bool aINTFM = true)
    {
        threshHold = t;
        active = a;
        if (this.Name != "AlmightyTimer")
        {
            AlmightyTimer = (Timer)p.GetTree().Root.GetNode("AlmightyTimer");
            AlmightyTimer.TheLowlyMasses.Add(this);
        }
        alertIfNoTimerFinishedMethod = aINTFM;
    }
    public Timer()
    {

    }
    List<Timer> TheLowlyMasses = new(); //seeing as its only used for the Almighty Timer, well...
    public float threshHold;
    public float timerVal = 0;
    public bool active;
    public bool done = false; //true after timer stops, false when it is not going. Starts off false, too

    //public delegate void TimerFinishedEventHandler(); //why it so long tho
    public event Action TimerFinished;

    // Called every frame. 'delta' is the elapsed time since the previous frame.

    public override void _Process(double delta)
    {
        foreach (Timer t in TheLowlyMasses)
        {
            t.UpdateValues((float)delta);
        }
    }
    public void UpdateValues(float d)
    {
        if (active)
        {
            if (timerVal < threshHold)
            {
                timerVal += d;
            }
            else
            {
                if (timerVal > threshHold)
                {
                    timerVal = threshHold;
                }
                active = false;
                done = true;
                if (TimerFinished != null) {
                    TimerFinished?.Invoke(); //first time using events in c#, wish me lukc guys!!!1!!!1!
                                         //those stupid freaking nullable question marks. every time.
                } else if (alertIfNoTimerFinishedMethod) {
                    GD.Print("WARNING: Timer finished method not defined!");
                }
            }
        }
    }
    public void Start()
    {
        timerVal = 0;
        active = true;
        done = false;
    }
    public void Start(float newThreshHold)
    {
        threshHold = newThreshHold;
        Start();
    }

    public void Stop(bool makeDone = true)
    {
        active = false;
        done = makeDone;
    }
}
