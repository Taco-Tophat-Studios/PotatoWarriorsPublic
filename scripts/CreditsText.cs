using Godot;
using System;

public partial class CreditsText : Control
{
	public string[] cTitles = {"OVERVIEW", "TECHNICS", "ART", "SOUND", "TESTING", "SUPPORT", "SPECIAL" };
	//was gonna make it multi-dimensional but 
	public string[] cNames =	{	"A Game by Taco Tophat Studios\nAn Arcollegiate Media Production\n\nCreated by CMTacoTophat\nDesigned by CMTacoTophat\nThe Big Cheese: CMTacoTophat" ,
									"Programmed by CMTacoTophat\nServer Management by <server manager>\nServers by <server service>",
									"Art by CMTacoTophat\nAnimation by CMTacoTophat\nArt and Animation done in Aseprite",
									"Music by TheQuantumDJ\nSFX by TheQuantumDJ\nSound Design by CMTacoTophat", "" +
									"Tested by:\nLiam Murphy\nOffsidePilot\nSpez exe\nSvledac\n<others>\nTheQuantumDJ\nCMTacoTophat",
									"Supported by:\n<supporters>",
									"Special Thanks to <cool people>"
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
		} else if (cIndex >= cTitles.Length) {
			cIndex = 0;
		}
		cTitleText.Text = cTitles[cIndex];
		cNamesText.Text = cNames[cIndex];
    }
}
