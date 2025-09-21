﻿namespace RebuildSharedData.Enum;

public enum NpcDisplayType : byte
{
    Sprite,
    Effect,
    MaskedEffect,
    VendingProxy
}

public enum NpcEffectType : byte
{
    None,
    Firewall,
    Pneuma,
    SafetyWall,
    WarpPortalOpening,
    WarpPortal,
    MapWarp,
    Demonstration,
    WaterBall,
    Sanctuary,
    MagnusExorcismus,
    StatueOfGoddessWoodBlock,
    StatueOfGoddessVakarine,
    StatueOfGoddessCarveOwl,

    //traps
    AnkleSnare,
    BlastMine,
    ClaymoreTrap,
    LandMine,
    SandmanTrap,
    FlasherTrap,
    FreezingTrap,
    SkidTrap,
    ShockwaveTrap,
    TalkieBox,
    
    //custom
    LightOrb,
}