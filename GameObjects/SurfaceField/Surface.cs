using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;

[Tool]
public partial class Surface : MeshInstance3D
{

    int SURFACE_LENGTH = 80;

    float NOISE_SCALE = 0.25f;

    bool flag = false;
    int frameCount = 0;

    [Export]
	Vector3 pos = Vector3.Zero;

    [Export]
    CollisionShape3D collisionShape;


    Vector3 prev_pos = Vector3.Zero;
    ArrayMesh ArrMesh = new ArrayMesh();

    Dictionary<Vector2, float> Heights = new Dictionary<Vector2, float>();
    Dictionary<Vector2, Array<int>> NormalIndices = new Dictionary<Vector2, Array<int>>();

    Vector2[] QuadPos = {
        new Vector2 (0, 0),
        new Vector2 (1, 0),
        new Vector2 (0, 1),
        new Vector2 (0, 1),
        new Vector2 (1, 0),
        new Vector2 (1, 1),
    };

    Vector2[] Cardinals =
    {
        new Vector2 (1, 0),
        new Vector2 (0, 1),
        new Vector2 (-1, 0),
        new Vector2 (0, -1),
    };

    private static Vector3 RandomGradient(float x, float y, float z)
    {
        // Generate a pseudo-random unit vector based on input coordinates
        float hash = MathF.Sin(new Vector3(x, y, z).Dot(new Vector3(127.1f, 311.7f, 74.7f))) * 43758.5453f;
        return (new Vector3(
            MathF.Sin(hash), MathF.Cos(hash * 1.1f), MathF.Sin(hash * 1.3f)
        )).Normalized();
    }

    private static float Smoothstep(float t) => t * t * (3 - 2 * t);

    public static float Noise(float x, float y, float z)
    {
        int X0 = (int)MathF.Floor(x), Y0 = (int)MathF.Floor(y), Z0 = (int)MathF.Floor(z);
        float fx = x - X0, fy = y - Y0, fz = z - Z0;

        // Get random gradient vectors for cube corners
        Vector3 g000 = RandomGradient(X0, Y0, Z0);
        Vector3 g100 = RandomGradient(X0 + 1, Y0, Z0);
        Vector3 g010 = RandomGradient(X0, Y0 + 1, Z0);
        Vector3 g110 = RandomGradient(X0 + 1, Y0 + 1, Z0);
        Vector3 g001 = RandomGradient(X0, Y0, Z0 + 1);
        Vector3 g101 = RandomGradient(X0 + 1, Y0, Z0 + 1);
        Vector3 g011 = RandomGradient(X0, Y0 + 1, Z0 + 1);
        Vector3 g111 = RandomGradient(X0 + 1, Y0 + 1, Z0 + 1);

        // Compute dot products between gradient vectors and displacement vectors
        float n000 = g000.Dot(new Vector3(fx, fy, fz));
        float n100 = g100.Dot(new Vector3(fx - 1, fy, fz));
        float n010 = g010.Dot(new Vector3(fx, fy - 1, fz));
        float n110 = g110.Dot(new Vector3(fx - 1, fy - 1, fz));
        float n001 = g001.Dot(new Vector3(fx, fy, fz - 1));
        float n101 = g101.Dot(new Vector3(fx - 1, fy, fz - 1));
        float n011 = g011.Dot(new Vector3(fx, fy - 1, fz - 1));
        float n111 = g111.Dot(new Vector3(fx - 1, fy - 1, fz - 1));

        // Smooth interpolation
        float u = Smoothstep(fx), v = Smoothstep(fy), w = Smoothstep(fz);

        // Trilinear interpolation
        return Mathf.Lerp(
            Mathf.Lerp(Mathf.Lerp(n000, n100, u), Mathf.Lerp(n010, n110, u), v),
            Mathf.Lerp(Mathf.Lerp(n001, n101, u), Mathf.Lerp(n011, n111, u), v),
            w
        );
    }

    public static Vector3 GetPlaneNormal(Vector3 A, Vector3 B, Vector3 C)
    {
        Vector3 v1 = B - A;
        Vector3 v2 = C - A;
        return (v1.Cross(v2)).Normalized(); // Returns a unit normal vector
    }

    public static Vector3 GetNormalFromSurrounding(Vector3[] points)
    {

        Vector3 center = points[0];
        Vector3 north = points[1];
        Vector3 south = points[2];
        Vector3 east = points[3];
        Vector3 west = points[4];

        // Compute normals for four triangles around the center
        Vector3 normal1 = (north - center).Cross( east - center).Normalized();
        Vector3 normal2 = (east - center).Cross(south - center).Normalized();
        Vector3 normal3 = (south - center).Cross( west - center).Normalized();
        Vector3 normal4 = (west - center).Cross(north - center).Normalized();

        // Average the normals to smooth out variations
        Vector3 averagedNormal = (normal1 + normal2 + normal3 + normal4).Normalized();

        return averagedNormal;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        Mesh = ArrMesh;

	}
	
	public override void _Process(double delta)
	{
        if (pos != prev_pos) {

            ArrMesh.ClearSurfaces();
            Heights.Clear();

            Vector3[] vertices = new Vector3[SURFACE_LENGTH * SURFACE_LENGTH * 6];
            Vector3[] normals = new Vector3[SURFACE_LENGTH * SURFACE_LENGTH * 6];
            Godot.Collections.Array mesh = new();
            mesh.Resize((int)Mesh.ArrayType.Max);
            int count = 0;

            for (int i = SURFACE_LENGTH / 2 - SURFACE_LENGTH; i < SURFACE_LENGTH - SURFACE_LENGTH / 2; i++)
            {
                for (int j = SURFACE_LENGTH / 2 - SURFACE_LENGTH; j < SURFACE_LENGTH - SURFACE_LENGTH / 2; j++)
                {

                    for (int k = 0; k < 6; k++)
                    {
                        Vector2 vec2 = new Vector2(i, j) + QuadPos[k];
                        vec2 *= 0.2f;

                        if (Heights.ContainsKey(vec2))
                        {
                            Vector3 v = new Vector3(vec2.X, Heights[vec2], vec2.Y);
                            vertices[count] = v;
                        }
                        else
                        {
                            Vector3 v = new Vector3(vec2.X, 0, vec2.Y);
                            float f = Noise((vec2.X + pos.X) * NOISE_SCALE, pos.Y * NOISE_SCALE, (vec2.Y + pos.Z) * NOISE_SCALE) * 5;
                            Heights[vec2] = f;
                            vertices[count] = v + new Vector3(0, f, 0);
                        }
                        count++;
                    }

                    normals[count - 6] = -GetPlaneNormal(vertices[count - 6], vertices[count - 5], vertices[count - 4]);
                    normals[count - 5] = -GetPlaneNormal(vertices[count - 6], vertices[count - 5], vertices[count - 4]);
                    normals[count - 4] = -GetPlaneNormal(vertices[count - 6], vertices[count - 5], vertices[count - 4]);

                    normals[count - 3] = -GetPlaneNormal(vertices[count - 3], vertices[count - 2],  vertices[count - 1]);
                    normals[count - 2] = -GetPlaneNormal(vertices[count - 3], vertices[count - 2],  vertices[count - 1]);
                    normals[count - 1] = -GetPlaneNormal(vertices[count - 3], vertices[count - 2],  vertices[count - 1]);
                    
                }
            }

            mesh[(int)Mesh.ArrayType.Vertex] = vertices;
            ((ConcavePolygonShape3D)collisionShape.Shape).SetFaces(vertices);
            mesh[(int)Mesh.ArrayType.Normal] = normals;
            ArrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, mesh);
        }

        prev_pos = pos;
        pos += new Vector3(0.05f, 0.0f, 0);
    }
}
