using Godot;
using Godot.Collections;
using VoxelMeshingTest.Classes;

public partial class VoxelMaker : Node3D
{
	Chunk ch;
	[Export] ItemList ItemList;
	[Export] CheckButton MetalIndexCheckbox;
	[Export] ItemList Faces;
    [Export] HSlider MetalSlider;
    [Export] LineEdit MetalIndex;

    [Export] LineEdit AlbedoIndex;
    [Export] ColorPickerButton Corner1Color;
    [Export] ColorPickerButton Corner2Color;
    [Export] ColorPickerButton Corner3Color;
    [Export] ColorPickerButton Corner4Color;

    [Export] LineEdit NewVoxelName;

    int SelectedFaceID = -1;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		PlayerTrackingManager.Instance();
		ChunkMeshManager.Instance();
		ch = new Chunk();
		int[,,] data = new int[GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE];
		data[33, 33, 33] = 1;

        ch.ChunkData = data;
		AddChild(ch);
		ch.Generated = true;
		RefreshList();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

    public override void _ExitTree()
    {

    }

    public void AssignBlock(int id) {
        GD.Print($"Assigning block ID: {id}");
		ch.ChunkData[33,33,33] = id;
	}

	public void RefreshBlock()
	{
		ChunkMeshManager.Instance().InitializeVoxelData();
        ch.Stale = true;
	}

	public void RefreshList()
	{
        ItemList.Clear();
		Dictionary<FaceData, Array<QuadData>> n = ChunkMeshManager.Instance().FaceQuadResourceDictionary;

		foreach (FaceData face in n.Keys)
		{
			ItemList.AddItem(face.ResourcePath.Replace("res://VoxelData/FaceData/", ""));
		}
	}

	//make big function that scrapes all the data from panel2 and applies it to selected faces.
	//texture index OR albedo from colorpickers
	//metalicity   index OR value from slider
	//emissiveness index OR value from slider
	//transparency index OR value from slider?

	//what else?

	//INDEX the quads, such that a voxel can have a polygon that is transp.
	public void SetValues()
	{

        if (SelectedFaceID == -1)
        {
            return;
        }

        int[] selected = Faces.GetSelectedItems();

        Dictionary<FaceData, Array<QuadData>> n = ChunkMeshManager.Instance().FaceQuadResourceDictionary;
        FaceData currentFace = null;
        foreach (FaceData face in n.Keys)
        {
            if (face.ResourcePath.Contains(ItemList.GetItemText(SelectedFaceID)))
            {
                currentFace = face;
                break; //only get the first one. No duplicate names considered
            }
        }
        foreach (int idx in selected)
        {
            switch (idx)
            {
                case 0:
                    foreach (QuadData qd in currentFace.UpFace)
                    {
                        AssignQuadValues(qd);
                    }
                    break;
                case 1:
                    foreach (QuadData qd in currentFace.NorthFace)
                    {
                        AssignQuadValues(qd);
                    }
                    break;
                case 2:
                    foreach (QuadData qd in currentFace.EastFace)
                    {
                        AssignQuadValues(qd);
                    }
                    break;
                case 3:
                    foreach (QuadData qd in currentFace.SouthFace)
                    {
                        AssignQuadValues(qd);
                    }
                    break;
                case 4:
                    foreach (QuadData qd in currentFace.WestFace)
                    {
                        AssignQuadValues(qd);
                    }
                    break;
                case 5:
                    foreach (QuadData qd in currentFace.DownFace)
                    {
                        AssignQuadValues(qd);
                    }
                    break;
                default:
                    break;
            }
        }
        ResourceSaver.Save(currentFace);
        RefreshBlock();
    }

    public void AssignQuadValues(QuadData quadData)
    {
        if (MetalIndexCheckbox.ButtonPressed)
        {
            if (float.TryParse(MetalIndex.Text, out float test))
            {
                quadData.UVMetallicityIndex = test;
            }
            else
            {
                quadData.UVMetallicityIndex = 0;
            }

        }
        else
        {
            quadData.UVMetallicityIndex = 1023 + (float)MetalSlider.Value;
        }

        quadData.Color[0] = Corner1Color.Color;
        quadData.Color[1] = Corner2Color.Color;
        quadData.Color[2] = Corner3Color.Color;
        quadData.Color[3] = Corner4Color.Color;
        if (float.TryParse(AlbedoIndex.Text, out float albedoIndex))
        {
            quadData.UVTextureIndex = albedoIndex;
        }

        ResourceSaver.Save(quadData);
    }

	public void ValueChange()
	{
		SetValues();
		RefreshBlock();
    }

    public void ValueChange(string str)
    {
        ValueChange();
    }

    public void SliderValueChange(float val)
    {
        ValueChange();
    }

    public void ColorValueChange(Godot.Color clr)
    {
        ValueChange();
    }

    public void FaceSelected(int idx)
	{
		SelectedFaceID = idx;
		GD.Print($"Face selected: {idx}, {ItemList.GetItemText(idx)}");

        int[] selected = Faces.GetSelectedItems();

        Dictionary<FaceData, Array<QuadData>> n = ChunkMeshManager.Instance().FaceQuadResourceDictionary;
        FaceData currentFace = null;
        foreach (FaceData face in n.Keys)
        {
            if (face.ResourcePath.Contains(ItemList.GetItemText(SelectedFaceID)))
            {
                currentFace = face;
                break; //only get the first one. No duplicate names considered
            }
        }


        AssignBlock(currentFace.ID);
        RefreshBlock();

    }

    public void Exit()
    {
        SceneSwitcher.Instance().PopScene();
    }

    public void _on_new_voxel_button_pressed()
    {
        if (NewVoxelName.Text.Length > 0)
        {
            FaceData template = (FaceData)ResourceLoader.Load<FaceData>("res://VoxelData/FaceData/TemplateVoxel.tres").Duplicate();
            template.ResourceName = NewVoxelName.Text;

            QuadData quadUp = new QuadData();
            QuadData quadNorth = new QuadData();
            QuadData quadEast = new QuadData();
            QuadData quadSouth = new QuadData();
            QuadData quadWest = new QuadData();
            QuadData quadDown = new QuadData();

            template.UpFace.Clear();
            template.NorthFace.Clear();
            template.EastFace.Clear();
            template.SouthFace.Clear();
            template.WestFace.Clear();
            template.DownFace.Clear();

            quadUp.vertices = ResourceLoader.Load<QuadData>("res://VoxelData/QuadData/TemplateUp.tres").vertices;
            quadNorth.vertices = ResourceLoader.Load<QuadData>("res://VoxelData/QuadData/TemplateNorth.tres").vertices;
            quadEast.vertices = ResourceLoader.Load<QuadData>("res://VoxelData/QuadData/TemplateEast.tres").vertices;
            quadSouth.vertices = ResourceLoader.Load<QuadData>("res://VoxelData/QuadData/TemplateSouth.tres").vertices;
            quadWest.vertices = ResourceLoader.Load<QuadData>("res://VoxelData/QuadData/TemplateWest.tres").vertices;
            quadDown.vertices = ResourceLoader.Load<QuadData>("res://VoxelData/QuadData/TemplateDown.tres").vertices;

            quadUp.Normals = ResourceLoader.Load<QuadData>("res://VoxelData/QuadData/TemplateUp.tres").Normals;
            quadNorth.Normals = ResourceLoader.Load<QuadData>("res://VoxelData/QuadData/TemplateNorth.tres").Normals;
            quadEast.Normals = ResourceLoader.Load<QuadData>("res://VoxelData/QuadData/TemplateEast.tres").Normals;
            quadSouth.Normals = ResourceLoader.Load<QuadData>("res://VoxelData/QuadData/TemplateSouth.tres").Normals;
            quadWest.Normals = ResourceLoader.Load<QuadData>("res://VoxelData/QuadData/TemplateWest.tres").Normals;
            quadDown.Normals = ResourceLoader.Load<QuadData>("res://VoxelData/QuadData/TemplateDown.tres").Normals;

            template.UpFace.Add(quadUp);
            template.NorthFace.Add(quadNorth);
            template.EastFace.Add(quadEast);
            template.SouthFace.Add(quadSouth);
            template.WestFace.Add(quadWest);
            template.DownFace.Add(quadDown);

            template.ID = ItemList.ItemCount + 1;
            
            ResourceSaver.Save(template, $"res://VoxelData/FaceData/{NewVoxelName.Text}.tres", ResourceSaver.SaverFlags.ChangePath);
            //load in voxel template
            //load in quads
            //copy voxel
            //copy quads, hook up quads to voxel
            ValueChange();
            RefreshList();
            ChunkMeshManager.Instance().InitializeVoxelData();
        }
    }
}
