using Godot;
using System;

public partial class MatchSettings : Button
{
	// Called when the node enters the scene tree for the first time.
	private Control ms;
	private host_join hj;
	private PlayButton pb;
	public bool vis = false;
	public override void _Ready()
	{
		ms = (Control)GetNode("../MatchSettings");
		hj = (host_join)GetNode("../HostJoinNode");
		pb = (PlayButton)GetNode("../PlayButton");

		pb.arenaPicker.Select(5);
    }

	private void _on_button_down() {
		if (Multiplayer.GetUniqueId() == 1)
		{
			if (!ms.Visible) {
				ms.Visible = true;
				vis = true;
			} else {
				ms.Visible = false;
				vis = false;
				hj.Sync("SetMatchSettings", (float)pb.time.Value, pb.coin.ButtonPressed, (float)pb.health.Value, (float)pb.health2.Value, pb.arenaPicker.GetSelectedItems()[0]);
			}
		} else
		{
			this.Text = "Not Host!";
		}
		
	}
}
