using Godot;
using System;
using System.Collections.Generic;

public partial class musicPlayer : AudioStreamPlayer
{
    private AudioStream[] songs;
    private int randIndex;
    private AudioStream lastSong;
    [Export]
    public bool tutorial = false;
    // Called when the node enters the scene tree for the first time.
    //TODO: make a dictionary connecting each song name to an AudioStream object


    public override void _Ready()
    {
        //Stream = (AudioStream)GD.Load("res://music/PiandomoniumFinal.mp3");
        songs = new AudioStream[] { (AudioStream)GD.Load("res://music/PW1.mp3"), (AudioStream)GD.Load("res://music/PW2.mp3"), (AudioStream)GD.Load("res://music/PW3.mp3") };
        Select();
        this.VolumeDb = AudioServer.GetBusVolumeDb(1);
        //Play();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    private void _on_finished()
    {
        Select();
    }
    private void Select()
    {
        if (tutorial)
        {
            Stream = (AudioStream)GD.Load("res://music/PWTutorial.mp3");
            Play();
            return;
        }
        //select random song from list that is not the last one
        Random r = new();
        //one less than songs
        List<AudioStream> tempSongArray = new();
        for (int i = 0; i < songs.Length; i++)
        {
            if (songs[i] != lastSong)
            {
                tempSongArray.Add(songs[i]);
            }
        }

        randIndex = r.Next(0, songs.Length - 1);
        lastSong = tempSongArray[randIndex];
        Stream = lastSong;
        return;
    }
}



