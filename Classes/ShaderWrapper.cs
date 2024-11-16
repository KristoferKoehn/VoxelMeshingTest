using Godot;
using Godot.Collections;
using System;
using System.Threading;

namespace VoxelMeshingTest.Classes
{
    public class ShaderWrapper
    {

        RenderingDevice rd;
        RDShaderFile shaderFile;
        RDShaderSpirV shaderBytecode;
        Rid ShaderRID;
        Rid pipelineRID;

        Rid QuadBuffer;
        Rid QuadCountBuffer;
        Rid ChunkDataBuffer;
        Rid ChunkDimensionalBuffer;

        Array<RDUniform> Uniforms = new Array<RDUniform>();

        RDUniform QuadUniform;
        RDUniform QuadCountUniform;
        RDUniform ChunkDataUniform;
        RDUniform ChunkDimensionalUniform;
        Rid UniformSet;

        int ChunkSize = GameConstants.CHUNK_SIZE;
        int WorkGroupSide = 64;

        uint BufferSize = 402653184;

        bool BuffersBuilt = false;

        public ShaderWrapper()
        {
            rd = RenderingServer.CreateLocalRenderingDevice();
            shaderFile = GD.Load<RDShaderFile>("res://Compute/ChunkMesherFast2.glsl");
            shaderBytecode = shaderFile.GetSpirV();
            ShaderRID = rd.ShaderCreateFromSpirV(shaderBytecode);
            CreateBuffers();
            CreateComputeList();
        }

        private void CreateBuffers()
        {

            int WorkGroups = WorkGroupSide * WorkGroupSide * WorkGroupSide;

            byte[] DimensionBytes = new byte[sizeof(int) * 2];
            Buffer.BlockCopy(new int[] { ChunkSize, WorkGroupSide }, 0, DimensionBytes, 0, DimensionBytes.Length);

            QuadBuffer = rd.StorageBufferCreate(BufferSize);
            QuadCountBuffer = rd.StorageBufferCreate(sizeof(int) * 2);
            ChunkDataBuffer = rd.StorageBufferCreate((uint)(ChunkSize * ChunkSize * ChunkSize * sizeof(int)));
            ChunkDimensionalBuffer = rd.StorageBufferCreate(sizeof(int) * 2, DimensionBytes);

            QuadUniform = new RDUniform();
            Uniforms.Add(QuadUniform);
            QuadUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
            QuadUniform.Binding = 0;
            QuadUniform.AddId(QuadBuffer);

            QuadCountUniform = new RDUniform();
            Uniforms.Add(QuadCountUniform);
            QuadCountUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
            QuadCountUniform.Binding = 1;
            QuadCountUniform.AddId(QuadCountBuffer);

            ChunkDataUniform = new RDUniform();
            Uniforms.Add(ChunkDataUniform);
            ChunkDataUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
            ChunkDataUniform.Binding = 2;
            ChunkDataUniform.AddId(ChunkDataBuffer);

            ChunkDimensionalUniform = new RDUniform();
            Uniforms.Add(ChunkDimensionalUniform);
            ChunkDimensionalUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
            ChunkDimensionalUniform.Binding = 3;
            ChunkDimensionalUniform.AddId(ChunkDimensionalBuffer);

            pipelineRID = rd.ComputePipelineCreate(ShaderRID);
            UniformSet = rd.UniformSetCreate(Uniforms, ShaderRID, 0);
        }

        void CreateComputeList()
        {
            long ComputeList = rd.ComputeListBegin();

            rd.ComputeListBindComputePipeline(ComputeList, pipelineRID);
            rd.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
            rd.ComputeListDispatch(ComputeList, (uint)WorkGroupSide, (uint)WorkGroupSide, (uint)WorkGroupSide);
            rd.ComputeListEnd();
        }

        public void AssignMesh(int[] data, Chunk ch)
        {

            byte[] inputBytes = new byte[data.Length * sizeof(int)];
            Buffer.BlockCopy(data, 0, inputBytes, 0, inputBytes.Length);

            rd.BufferClear(QuadCountBuffer, 0, 8);
            rd.BufferClear(ChunkDataBuffer, 0, (uint)inputBytes.Length);


            Error f = rd.BufferUpdate(ChunkDataBuffer, 0, (uint)inputBytes.Length, inputBytes);

            rd.Submit();

            byte[] countBytes = rd.BufferGetData(QuadCountBuffer);
            int[] Count = new int[2];
            Buffer.BlockCopy(countBytes, 0, Count, 0, sizeof(uint) * 2);

            int loopcount = 0;
            while (Count[1] == 0 && loopcount < 4)
            {
                countBytes = rd.BufferGetData(QuadCountBuffer);
                Count = new int[2];
                Buffer.BlockCopy(countBytes, 0, Count, 0, sizeof(uint) * 2);
                loopcount++;
            }
            GD.Print($"face count: {Count[0]} loop {loopcount}");

            byte[] QBytes = rd.BufferGetData(QuadBuffer, 0, (uint)Count[0] * 64);

            ch.PChunkByteAssignment(QBytes);

            rd.BufferClear(QuadBuffer, 0, (uint)Count[0] * 64);
            rd.BufferClear(ChunkDataBuffer, 0, (uint)inputBytes.Length);

            //DisposeComputeList();
        }

        public void DisposeComputeList()
        {
            Uniforms.Clear();
            rd.FreeRid(pipelineRID);
            rd.FreeRid(QuadBuffer);
            rd.FreeRid(ChunkDataBuffer);
            rd.FreeRid(QuadCountBuffer);
            rd.FreeRid(ChunkDimensionalBuffer);
        }
    }
}
