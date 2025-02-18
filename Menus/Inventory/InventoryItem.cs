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
}
