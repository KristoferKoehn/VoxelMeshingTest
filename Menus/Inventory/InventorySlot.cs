using Godot;
using Godot.Collections;

public partial class InventorySlot : Panel
{

    [Signal]
    public delegate void DragItemStartEventHandler(InventorySlot inventorySlot);

    [Signal]
    public delegate void DragItemEndEventHandler(InventorySlot inventorySlot);

    [Export]
    public Sprite2D Sprite2D { get; set; }
    [Export]
    public InventoryItem item { get; set; }
    [Export]
    public Label amount { get; set; }
    [Export]
    public Sprite2D SlotUnderlay { get; set; }

    public Array<Inventory.SlotType> SlotType { get; set; } = new Array<Inventory.SlotType>();

    bool MouseInside = false;

    Dictionary<Inventory.SlotType, Rect2> SpriteOffsets = new()
    {
        { Inventory.SlotType.Weapon,  new Rect2(new Vector2(544, 136), 16, 16)},
        { Inventory.SlotType.Armor,  new Rect2(new Vector2(561, 17), 16, 16)},
        { Inventory.SlotType.Legs,  new Rect2(new Vector2(680, 17), 16, 16)},
        { Inventory.SlotType.Head,  new Rect2(new Vector2(578, 0), 16, 16)},
        { Inventory.SlotType.Ring,  new Rect2(new Vector2(765, 102), 16, 16)},
        { Inventory.SlotType.Shield,  new Rect2(new Vector2(629, 68), 16, 16)},
        { Inventory.SlotType.Hands,  new Rect2(new Vector2(697, 0), 16, 16)},
        { Inventory.SlotType.Axe,  new Rect2(new Vector2(714, 136), 16, 16)},
        { Inventory.SlotType.Shoulders,  new Rect2(new Vector2(595, 17), 16, 16)},
        { Inventory.SlotType.Staff,  new Rect2(new Vector2(544, 68), 16, 16)},
        { Inventory.SlotType.Hammer,  new Rect2(new Vector2(629, 119), 16, 16)},
    };

    Dictionary<Inventory.SlotType, string> SlotString = new Dictionary<Inventory.SlotType, string>() {
        { Inventory.SlotType.Weapon,  "right_hand"},
    };


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        AtlasTexture at = (AtlasTexture)SlotUnderlay.Texture;

        if (SlotType.Count != 0)
        {
            at.Region = SpriteOffsets[SlotType[0]];
        }
        
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public override void _Input(InputEvent @event)
	{
		if (MouseInside)
		{
            if (@event.IsActionPressed("click"))
            {
                EmitSignal("DragItemStart", this);
            }

            if (@event.IsActionReleased("click"))
            {
                EmitSignal("DragItemEnd", this);
            }
        }
    }

    public void Update(InventoryItem inventoryItem)
	{
		if (inventoryItem != null)
		{
            amount.Visible = true;
            Sprite2D.Visible = true;
			Sprite2D.Texture = inventoryItem.Sprite;
            if (item.StackAmount > 1)
			{
				amount.Text = item.StackAmount.ToString();
			}
			else
			{
				amount.Text = "";
            }
			SlotUnderlay.Visible = false;
        }
		else
		{
			amount.Visible = false;
            Sprite2D.Texture = null;
            if (SlotType.Count > 0)
            {
                SlotUnderlay.Visible = true;
            }
        }
    }

    public void Equip()
    {
        Node3D equip = item.Item.Instantiate() as Node3D;

    }

	public void MouseEnteredFunction()
	{
        MouseInside = true;
		SelfModulate = SelfModulate - new Color(0.5f, 0.5f, 0.5f, 0f);
    }

    public void MouseExitedFunction()
    {
        MouseInside = false;
		
        SelfModulate = SelfModulate + new Color(0.5f, 0.5f, 0.5f, 0f);
    }
}
