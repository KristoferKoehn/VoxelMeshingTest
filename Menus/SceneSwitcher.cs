using Godot;
using System.Collections.Generic;

public partial class SceneSwitcher : Node
{

    public Stack<Node> SceneStack = new Stack<Node>();
    public static Node root;
    private static SceneSwitcher instance = null;

    public override void _Ready()
    {
        root = GetTree().Root;
        instance = this;
        PushScene("res://Menus/MainMenu.tscn");
    }
    
    public static SceneSwitcher Instance()
    {
        return instance;
    }

    public void PushScene(string ScenePath) // used to move to another scene
    {
        Node previousScene = null;
        if (SceneStack.Count > 0)
        {
            previousScene = SceneStack.Peek();
            RemoveChild(previousScene);
        }
        Node scene = GD.Load<PackedScene>(ScenePath).Instantiate<Node>();
        SceneStack.Push(scene);
        AddChild(scene);

    }


    public void PopScene() // used to go back to the previous scene (gets rid of the current scene forever).
    {
        if (SceneStack.Count == 0)
        {
            return;
        }

        Node node = SceneStack.Pop();

        if (node.GetParent() == this)
        {
            this.RemoveChild(node);
            node.QueueFree();
        }

        if (SceneStack.Count > 0)
        {
            Node previousScene = SceneStack.Peek();
            if (previousScene.GetParent() != this)
            {
                this.AddChild(previousScene);
            }
        }
    }
}
