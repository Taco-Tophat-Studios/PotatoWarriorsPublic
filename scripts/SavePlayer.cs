using Godot;
using System;
public partial class SavePlayer : Button
{
    /*
	 WARNING!!! Weird file stuff!

	 https://docs.godotengine.org/en/stable/tutorials/io/saving_games.html
	 */
	
	LineEdit nameEdit;
	TextEdit descEdit;
	ItemList faceEdit;
	ItemList swordEdit;
	ItemList shieldEdit;
	Label pointsLabel;
	Label winsLabel;
    // Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		nameEdit = (LineEdit)GetNode("../NameEdit");
        descEdit = (TextEdit)GetNode("../DescEdit");
		faceEdit = (ItemList)GetNode("../FaceSelector");
        swordEdit = (ItemList)GetNode("../SwordSelector");
		shieldEdit = (ItemList)GetNode("../ShieldSelector");
		pointsLabel = (Label)GetNode("../PointsLabel");
		winsLabel = (Label)GetNode("../WinsLabel");
        
		//Global.LoadCharData();

		/*nameEdit.Text = Global.name;
        descEdit.Text = Global.desc;
    	faceEdit.Select(Global.faceIndex);
        swordEdit.Select(Global.swordIndex);
		shieldEdit.Select(Global.shieldIndex);
		pointsLabel.Text = "You have " + Global.points + " points!";
		winsLabel.Text = "You have " + Global.wonGames + " wins!";*/
	}

	private void _on_button_down()
	{
		
		/*Global.name = nameEdit.Text;
		Global.desc = descEdit.Text;
		Global.swordIndex = swordEdit.GetSelectedItems()[0];
        Global.faceIndex = faceEdit.GetSelectedItems()[0];
		Global.shieldIndex = shieldEdit.GetSelectedItems()[0];
        
		Global.StoreData();*/
    }
    
}



