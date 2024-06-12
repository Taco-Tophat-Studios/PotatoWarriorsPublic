using Godot;
using System;

public partial class Response : Node
{
    //used as a type for the enemyBase behaviour dictionary
    public delegate void ResponseDel(params Variant[] args);
    ResponseDel responseMethods;

    public Response()
    {

    }
    public Response(ResponseDel[] rs)
    {
        responseMethods = rs[0];

        if (rs.Length > 1) {
            for (int i = 1; i < rs.Length; i++) {
                responseMethods += rs[i];
            }
        }
    }
    
}
