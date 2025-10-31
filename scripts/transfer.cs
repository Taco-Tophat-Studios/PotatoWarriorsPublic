public partial class transfer : buttonBaseClass
{
    private bool quitting = false;

    private void _on_button_down()
    {
        switch (this.Name)
        {
            case "PlayButton":
                scene = "res://Screens/Menus/ModeSelect.tscn";
                break;
            case "SettingsButton":
                scene = "res://Screens/Menus/settings_menu.tscn";
                break;
            case "CustomizeButton":
                scene = "res://Screens/Menus/PlayerMenu.tscn";
                break;
            case "CreditsButton":
                scene = "res://Screens/Menus/credits.tscn";
                break;
            case "QuitButton":
                quitting = true; //just in case quitting doesn't pause execution
                GetTree().Quit();
                break;
            case "HostButton":
                scene = "res://Screens/Menus/Lobby.tscn";
                break;
            default:
                scene = "res://walschaerts_gear_challenge.tscn";
                break;
        }
        if (!quitting)
        {
            Transfer();
        }

    }
}
