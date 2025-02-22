using Godot;
using Godot.Collections;

public partial class InventoryPanel : Panel2
{
    [Export]
    Inventory Inventory { get; set; }
    [Export]
    GridContainer Container { get; set; }
    [Export]
    PackedScene SlotScene { get; set; }
    [Export]
    Sprite2D DraggerSprite { get; set; }

    Array<InventorySlot> Slots { get; set; } = new Array<InventorySlot>();

    InventorySlot DraggingSubject { get; set; } = null;

    bool MouseInside = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
        Inventory = ResourceLoader.Load<Inventory>("res://GameObjects/Inventories/PlayerInventory.tres");
        foreach (var item in Inventory.Slots)
        {
            InventorySlot IS = SlotScene.Instantiate() as InventorySlot;
            IS.DragItemStart += DraggingItemStartedFunction;
            IS.DragItemEnd += DraggingItemEndedFunction;
            if (item != null)
            {
                IS.item = item;
            }
            
            Slots.Add(IS);
            Container.AddChild(IS);
        }
        UpdateSlots();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);

        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            Visible = false;
        }
        else
        {
            Visible = true;
        }


        if (DraggingSubject != null && DraggingSubject.item != null && Input.IsActionPressed("click"))
        {
            DraggerSprite.Visible = true;
            DraggerSprite.Texture = DraggingSubject.item.Sprite;
            Vector2 mpos = GetViewport().GetMousePosition();
            DraggerSprite.Position = mpos + new Vector2(20,15) - Position;
        }
        else
        {

            DraggerSprite.Visible = false;
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (Input.IsActionJustReleased("click"))
        {
            if (MouseInside && DraggingSubject != null)
            {
                DraggingSubject.Modulate = DraggingSubject.Modulate + new Color(0.5f, 0.5f, 0.5f, 0f);
                DraggingSubject = null;
            }
        }
    }

    public void UpdateSlots()
    {
        Inventory.Slots.Clear();
        foreach (InventorySlot slot in Slots)
        {
            Inventory.Slots.Add(slot.item);
        }


        for (int i = 0; i < Mathf.Min(Slots.Count, Inventory.Slots.Count); i++) {
            Slots[i].Update(Inventory.Slots[i]);
        }
    }

    public void DraggingItemStartedFunction(InventorySlot inventorySlot)
    {
        DraggingSubject = inventorySlot;
        if (DraggingSubject != null)
        {
            DraggingSubject.Modulate = DraggingSubject.Modulate - new Color(0.5f, 0.5f, 0.5f, 0f);
        }
    }

    public void DraggingItemEndedFunction(InventorySlot inventorySlot)
    {
        

        if (DraggingSubject != null)
        {

            DraggingSubject.Modulate = DraggingSubject.Modulate + new Color(0.5f, 0.5f, 0.5f, 0f);


            if (inventorySlot.item != null && 
                inventorySlot.item.StackMax > inventorySlot.item.StackAmount + DraggingSubject.item.StackAmount && 
                DraggingSubject.item.Name.Equals(inventorySlot.item.Name) && 
                inventorySlot.item.Stackable)
            {
                GD.Print("Stackably");
                inventorySlot.item.StackAmount += DraggingSubject.item.StackAmount;
                DraggingSubject.item = null;
                DraggingSubject = null;

            }
            else
            {
                InventoryItem temp = inventorySlot.item;
                inventorySlot.item = DraggingSubject.item;
                DraggingSubject.item = temp;
                DraggingSubject = null;
            }
        }


        UpdateSlots();
    }

    public void MouseExitedFunction()
    {
        MouseInside = false;

        Vector2 mpos = GetViewport().GetMousePosition();

        Vector2 pos = Position + new Vector2(2,2);
        Vector2 size = Position + new Vector2(-2,-2) + Size;

        if (mpos.X < pos.X || mpos.X > size.X || mpos.Y < pos.Y || mpos.Y > size.Y)
        {
            if (DraggingSubject != null && DraggingSubject.item != null)
            {
                DraggingSubject.Modulate = DraggingSubject.Modulate + new Color(0.5f, 0.5f, 0.5f, 0f);
            }
            DraggingSubject = null;
        }
    }

    public void MouseEnteredFunction() {
        MouseInside = true;
    }
}
