using Godot;

public partial class VolSlider : HSlider
{
    string index;
    int ind;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Global.LoadCharData();
        if (Global.volSliderValues.IsEmpty())
        {
            Global.volSliderValues = new float[] { 0, 0, 0 };
            Global.StoreData();
        }

        if (this.Name == "MasterVolSlider")
        {
            SetUp("Master", 0);
        }
        else if (this.Name == "MusicVolSlider")
        {
            SetUp("Music", 1);
        }
        else
        {
            SetUp("SFX", 2);
        }
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(index), Global.volSliderValues[ind]);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    private void _on_value_changed(double value)
    {
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(index), (float)value);
    }
    private void _on_drag_ended(bool value_changed)
    {
        Global.volSliderValues[ind] = AudioServer.GetBusVolumeDb(ind);
        Global.StoreData();
    }
    private void SetUp(string ind1, int ind2)
    {
        index = ind1;
        ind = ind2;
        this.Value = Global.volSliderValues[ind];
    }
}
