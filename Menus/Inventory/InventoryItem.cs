using Godot;

[GlobalClass]
public partial class InventoryItem : Resource
{
    [Export]
    public string Name = "";
    [Export]
    public PackedScene Item = null;
    [Export]
    public Texture2D Sprite = null;
    [Export]
    public bool Stackable = false;
    [Export]
    public int StackAmount = 1;
    [Export]
    public int StackMax = 99;
}
