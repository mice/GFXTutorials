Shader "Unlit/ExampleShaderIndirectMTXCullTexture"
{
    Properties {
        _MyArr ("Tex", 2DArray) = "" {}
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require 2darray

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"
            
            UNITY_DECLARE_TEX2DARRAY(_MyArr);
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float textureID : TEXCOORD1;
                float4 color : COLOR0;
            };

            StructuredBuffer<float3> _Positions;
            StructuredBuffer<float2> _UVS;
            StructuredBuffer<float4x4> _ObjectToWorlds;
            StructuredBuffer<int> _ObjectTextures;
            StructuredBuffer<int> _MatrixIndex;

            v2f vert(uint svVertexID: SV_VertexID, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                v2f o;
                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);
                
                int Index = GetIndirectVertexID(svVertexID);
                float3 pos = _Positions[Index];
                int matrixIndex = _MatrixIndex[globalIndirectDrawArgs.startInstance + instanceID];//∑¿÷π–ﬁ∏ƒMatrix,ÃÌº”“ª≤„Indirect.
                //int matrixIndex = globalIndirectDrawArgs.startInstance + instanceID;
                float4 wpos =  mul(_ObjectToWorlds[matrixIndex], float4(pos,1.0));
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                o.uv = _UVS[Index];//; float3(_UVS[Index],_ObjectTextures[matrixIndex]);
                o.textureID = _ObjectTextures[matrixIndex];
                //o.color = float4(cmdID & 1 ? 0.0f : 1.0f, cmdID & 1 ? 1.0f : 0.0f, instanceID / float(GetIndirectInstanceCount()), 0.0f);
                //o.color = float4(cmdID & 1 ? 0.0f : 1.0f, cmdID & 1 ? 1.0f : 0.0f, 1 / float(globalIndirectDrawArgs.startInstance), 0.0f);
                o.color = float4(float(globalIndirectDrawArgs.startInstance),float(globalIndirectDrawArgs.startInstance), float(globalIndirectDrawArgs.startInstance), 0.0f);
               
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 mainUV = float3(i.uv,i.textureID);
                fixed3 albedo = UNITY_SAMPLE_TEX2DARRAY(_MyArr, mainUV).rgb;
                return float4(albedo,1);
            }
            ENDCG
        }
    }
}