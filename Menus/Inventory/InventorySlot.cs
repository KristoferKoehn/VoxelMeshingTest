using Godot;

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

	bool MouseInside = false;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

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
        }
		else
		{
			amount.Visible = false;
            Sprite2D.Texture = null;
            Sprite2D.Visible = false;
		}
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
