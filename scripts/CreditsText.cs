using Godot;

public partial class CreditsText : Control
{
    public string[] cTitles = { "OVERVIEW", "TECHNICS", "ART", "SOUND", "TESTING", "SPECIAL" };
    //was gonna make it multi-dimensional but 
    public string[] cNames =  {   "A Game by Taco Tophat Studios\n\nCreated by CMTacoTophat\nDesigned by CMTacoTophat\nCredits Self-embellishment: CMTacoTophat" ,
                                    "Programming and Networking by CMTacoTophat",
                                    "Art and Animation by CMTacoTophat\nArt and Animation done in Aseprite",
                                    "Music by TheQuantumDJ\nSFX garnered by TheQuantumDJ\nSFX sourced from Pixabay and Freesound\nSound Design by CMTacoTophat", "" +
                                    "Tested by:\nSpez exe\nOffsidePilot\nCMTacoTophat",
                                    "Special Thanks to Jonas Tyroller for the inspiration and wisdom, and to my family & friends for the support"
                                };
    public int cIndex = 0;
    private Label cTitleText;
    private Label cNamesText;
    public override void _Ready()
    {
        cTitleText = (Label)GetNode("CreditsTitle");
        cNamesText = (Label)GetNode("CreditsNames");
    }

    public void SetText(int LR)
    {
        //set LR as either -1 if going backward or 1 if going forward
        cIndex += LR;
        //handle underflow
        if (cIndex < 0)
        {
            cIndex = cTitles.Length - 1;
            //dont add 1 because its >= and not >
        }
        else if (cIndex >= cTitles.Length)
        {
            cIndex = 0;
        }
        cTitleText.Text = cTitles[cIndex];
        cNamesText.Text = cNames[cIndex];
    }
}
