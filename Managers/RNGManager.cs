using Godot;
using System;

public partial class RNGManager : Node
{

    static RNGManager instance;

    public RandomNumberGenerator rng;
    ulong seed;
    int state;

    public static RNGManager Instance()
    {
        if (!IsInstanceValid(instance))
        {
            instance = new RNGManager();
            SceneSwitcher.Instance().AddChild(instance);
            instance.Name = "RNGManager";
            instance.rng = new RandomNumberGenerator();
            instance.seed = instance.rng.Seed;

            instance.SetSeed(10);
        }
        return instance;
    }

    public void SetSeed(ulong seed)
    {
        rng.Seed = seed;
        this.seed = seed;
    }

    public ulong GetSeed()
    {
        return seed;
    }
}
