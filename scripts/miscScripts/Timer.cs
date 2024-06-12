using Godot;
using System;
using System.Collections.Generic;

public partial class Timer : Node
{
	Timer HeadTimer; 
	public Timer (float t, bool a, Node p) {
		threshHold = t;
		active = a;
		if (this.Name != "AlmightyTimer") {
			HeadTimer = (Timer)p.GetTree().Root.GetNode("AlmightyTimer");
			HeadTimer.SubTimers.Add(this);
		}
	}
	public Timer() {

	}
	List<Timer> SubTimers = new List<Timer>(); 
	public float threshHold;
	public float timerVal = 0;
	public bool active;
	public bool done = false;

	public delegate void TimerFinishedEventHandler(); 
	public event TimerFinishedEventHandler TimerFinished;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	
	public override void _Process(double delta)
	{
		foreach (Timer t in SubTimers) {
			t.UpdateValues((float)delta);
		}
	}
	public void UpdateValues(float d) {
		if (active) {
			if (timerVal < threshHold) {
				timerVal += d;
			} else {
				if (timerVal > threshHold) {
					timerVal = threshHold;
				}
				active = false;
				done = true;
				TimerFinished?.Invoke(); 
			}
			
		}
	}
	public void Start() {
		timerVal = 0;
		active = true;
		done = false;
	}
	public void Start(float newThreshHold) {
		threshHold = newThreshHold;
		Start();
	}

	public void Stop(bool makeDone = true) {
		active = false;
		done = makeDone;
	}
}
