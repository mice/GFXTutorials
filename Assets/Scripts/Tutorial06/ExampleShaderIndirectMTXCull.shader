Shader "Unlit/ExampleShaderIndirectMTXCull"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR0;
            };

            StructuredBuffer<float3> _Positions;
            StructuredBuffer<float4x4> _ObjectToWorlds;
            StructuredBuffer<int> _MatrixIndex;

            v2f vert(uint svVertexID: SV_VertexID, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                v2f o;
                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);
                
                float3 pos = _Positions[GetIndirectVertexID(svVertexID)];
                int matrixIndex = _MatrixIndex[globalIndirectDrawArgs.startInstance + instanceID];//∑¿÷π–ﬁ∏ƒMatrix,ÃÌº”“ª≤„Indirect.
                //int matrixIndex = globalIndirectDrawArgs.startInstance + instanceID;
                float4 wpos =  mul(_ObjectToWorlds[matrixIndex], float4(pos,1.0));
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                //o.color = float4(cmdID & 1 ? 0.0f : 1.0f, cmdID & 1 ? 1.0f : 0.0f, instanceID / float(GetIndirectInstanceCount()), 0.0f);
                //o.color = float4(cmdID & 1 ? 0.0f : 1.0f, cmdID & 1 ? 1.0f : 0.0f, 1 / float(globalIndirectDrawArgs.startInstance), 0.0f);
                o.color = float4(float(globalIndirectDrawArgs.startInstance),float(globalIndirectDrawArgs.startInstance), float(globalIndirectDrawArgs.startInstance), 0.0f);
               
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}