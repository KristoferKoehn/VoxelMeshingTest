using Godot;
using System;

public partial class Panel2 : Panel
{
	
    bool MouseInside = false;
    bool dragging = false;
    Vector2 DragOffset = Vector2.Zero;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        MouseEntered += _on_panel_mouse_entered;
        MouseExited += _on_panel_mouse_exited;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (dragging)
        {
            GlobalPosition = GetGlobalMousePosition() + DragOffset;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionReleased("click"))
        {
            dragging = false;
        }


        if (@event.IsActionPressed("click") && MouseInside)
        {
            dragging = true;
            DragOffset = GlobalPosition - GetGlobalMousePosition();
        }
    }

    public void Close()
    {
        Visible = false;
    }

    public void _on_panel_mouse_entered()
    {
        MouseInside = true;
    }

    public void _on_panel_mouse_exited()
    {
        MouseInside = false;
    }

}
