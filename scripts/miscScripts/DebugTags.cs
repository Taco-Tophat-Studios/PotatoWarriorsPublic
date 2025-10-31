using Godot;
using System;
using System.Collections.Generic;

public partial class DebugTags
{
    public static Dictionary<string, bool> GLOBAL_DEBUG_TAGS = new Dictionary<string, bool>()
    {
        {"SHOW:tool_tile_intersects", false},
        {"PRINT:effvelX_bounce_reset", false},
        {"PRINT:effvelY_bounce_reset", false},
        {"PRINT:tool_tile_movement", false},
        {"PRINT:tiles_none", false},
        {"PRINT:tool_collider_polygon_points", false},
        {"PRINT:tool_player_type", false},
        {"PRINT:player_number_authority", false},
        {"PRINT:entity_damage_info", false},
        {"PRINT:bounce_damage_info", false},
        {"PRINT:broadcast_info", false}
    };
}
