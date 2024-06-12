using Godot;
using System;

public partial class HostButton : Button
{
	//NOTE: ALSO CLASS FOR JOIN BUTTON!
	host_join hj;
	LineEdit codeEdit;
    public override void _Ready()
	{
		hj = (host_join)GetNode("../../LobbyControl/HostJoinNode");
		codeEdit = (LineEdit)GetNode("../JoinEdit");
	}

    private void _on_button_down()
	{
		hj.OnHost();
	}
	private void _on_join_button_button_down()
	{
		hj.OnJoin(codeEdit.Text);
	}
}
