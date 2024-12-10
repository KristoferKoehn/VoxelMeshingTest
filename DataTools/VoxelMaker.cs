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
		ch.Remesh();
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
		ch.ChunkData[33,33,33] = id;
	}

	public void RefreshBlock()
	{
		ChunkMeshManager.Instance().InitializeVoxelData();

        ch.Remesh();
	}

	public void RefreshList()
	{
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
	}

    public void Exit()
    {
        SceneSwitcher.Instance().PopScene();
    }
}
