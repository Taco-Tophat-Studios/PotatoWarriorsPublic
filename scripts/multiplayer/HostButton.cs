using Godot;

public partial class HostButton : Button
{
    //NOTE: ALSO CLASS FOR JOIN BUTTON!
    host_join hj;
    [Export]
    LineEdit joinCodeEdit;
    Control ConfirmJoinMenu;
    Button ConfirmJoinButton;
    Button CancelJoinButton;
    Label LobbyTitleLabel;
    [Export]
    Label CodeStatusLabel;
    //REMINDER for future self: godot has accept.reject dialogue windows already, use THEM

    public override void _Ready()
    {
        hj = (host_join)GetNode("../../LobbyControl/HostJoinNode");
        ConfirmJoinMenu = (Control)GetNode("../../JoinDialogue");

        ConfirmJoinButton = (Button)ConfirmJoinMenu.GetNode("Panel/ConfirmJoinButton");
        ConfirmJoinButton.ButtonUp += _on_confirm_join_button_button_up;
        CancelJoinButton = (Button)ConfirmJoinMenu.GetNode("Panel/RejectJoinButton");
        CancelJoinButton.ButtonUp += _on_reject_join_button_button_up;

        LobbyTitleLabel = (Label)ConfirmJoinMenu.GetNode("Panel/LobbyNameLabel");
    }

    private void _on_button_down()
    {
        hj.OnHost();
    }
    private void _on_join_button_button_down()
    {
        string code = joinCodeEdit.Text.Trim();
        string err;
        hj.PreJoin(code, out err);
        if (err != "")
        {
            GD.Print("ERROR JOINING: " + err);
            CodeStatusLabel.Text = "Error Joining: " + err;
            return;
        }
        ConfirmJoinMenu.Visible = true;
        LobbyTitleLabel.Text = "Join Lobby: " + hj.LanInstance.GetSIFromCode(code).serverName + "?";
    }
    private void _on_confirm_join_button_button_up()
    {
        hj.OnJoin();
        ConfirmJoinMenu.Visible = false;
    }
    private void _on_reject_join_button_button_up()
    {
        ConfirmJoinMenu.Visible = false;
    }

    public void _on_check_button_toggled(bool toggle)
    {
        hj.LanInstance.SetCanConnectToSelf(toggle);
        GD.Print("Set canConnectToSelf to " + toggle);
    }
}
