// Made with Amplify Shader Editor v1.9.0.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TW/Particle/Particle_customBlend_dissolve"
{
	Properties
	{
		_InvFade ("Soft Particles Factor", float) = 1.0
		[Enum(Off,0,On,1)]_ZWrite ("ZWrite", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _Culling ("Culling", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("Blend mode Source", Int) = 5
 		[Enum(UnityEngine.Rendering.BlendMode)] _BlendDst ("Blend mode Destination", Int) = 10

		_MainTex("Main Tex", 2D) = "white" {}
		[HDR]_ColorHDR("Color HDR", Color) = (2,2,2,0)
		_Emissive("Emissive", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

		//_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	}


	Category 
	{
		SubShader
		{
		LOD 0

			Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
			Blend [_BlendSrc] [_BlendDst]
			ColorMask RGB
			Cull [_Culling]
			Lighting Off 
			ZWrite [_ZWrite]
			ZTest LEqual
			
			Pass {
				
				CGPROGRAM
				
				#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
					#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
				#endif
				
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#pragma multi_compile_instancing
				#pragma multi_compile_particles
				#pragma multi_compile_fog
				#define ASE_NEEDS_FRAG_COLOR


				#include "UnityCG.cginc"

				struct appdata_t 
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					
				};

				struct v2f 
				{
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD2;
					#endif
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
					
				};
				
				
				#if UNITY_VERSION >= 560
					UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
				#else
					uniform sampler2D_float _CameraDepthTexture;
				#endif

				//Don't delete this comment
				// uniform sampler2D_float _CameraDepthTexture;

				uniform sampler2D _MainTex;
				uniform float4 _MainTex_ST;
				uniform float _InvFade;
				uniform float4 _ColorHDR;
				uniform float _Emissive;


				v2f vert ( appdata_t v  )
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					

					v.vertex.xyz +=  float3( 0, 0, 0 ) ;
					o.vertex = UnityObjectToClipPos(v.vertex);
					#ifdef SOFTPARTICLES_ON
						o.projPos = ComputeScreenPos (o.vertex);
						COMPUTE_EYEDEPTH(o.projPos.z);
					#endif
					o.color = v.color;
					o.texcoord = v.texcoord;
					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}

				fixed4 frag ( v2f i  ) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( i );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( i );

					#ifdef SOFTPARTICLES_ON
						float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
						float partZ = i.projPos.z;
						float fade = saturate (_InvFade * (sceneZ-partZ));
						i.color.a *= fade;
					#endif

					float2 uv_MainTex = i.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
					float4 tex2DNode159 = tex2D( _MainTex, uv_MainTex );
					
					float4 texCoord235 = i.texcoord;
					texCoord235.xy = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float smoothstepResult156 = smoothstep( saturate( ( texCoord235.z - texCoord235.w ) ) , ( texCoord235.z + texCoord235.w ) , tex2DNode159.a);
					

					

					fixed4 col = ( ( _ColorHDR * tex2DNode159 * i.color ) * _Emissive );
					col.a = ( i.color.a * ( saturate( texCoord235.z ) == 0.0 ? tex2DNode159.a : smoothstepResult156 ) );
				
					UNITY_APPLY_FOG(i.fogCoord, col);
					return col;
				}
				ENDCG 
			}
		}	
	}
	CustomEditor "ASEMaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19002
196;286;1181;618;2987.884;1302.264;4.899267;True;False
Node;AmplifyShaderEditor.TextureCoordinatesNode;235;981.0144,487.2327;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;223;1370.882,526.2238;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;159;949.773,238.6188;Inherit;True;Property;_MainTex;Main Tex;0;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;218;1510.218,420.7831;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;226;1516.02,526.9477;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;189;1524.679,643.9274;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;156;1679.361,502.8398;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;182;1372.453,430.45;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;199;1030.532,-81.54135;Inherit;False;Property;_ColorHDR;Color HDR;1;1;[HDR];Create;True;0;0;0;False;0;False;2,2,2,0;16.94838,16.94838,16.94838,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;168;1064.525,120.8495;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Compare;215;1870.742,285.2521;Inherit;False;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;227;1405.181,96.35779;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;236;1791.436,13.62152;Inherit;False;Property;_Emissive;Emissive;2;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;169;1989.027,102.1113;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;170;2101.518,223.4296;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;234;2298.027,104.0919;Float;False;True;-1;2;ASEMaterialInspector;0;13;TW/Particle/Particle_customBlend_dissolve;7540b549f6f2af143b5ea8bebff0279f;True;SubShader 0 Pass 0;0;0;SubShader 0 Pass 0;3;False;True;1;0;True;_BlendSrc;0;True;_BlendDst;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;0;True;_Culling;False;True;True;True;True;False;0;False;;False;False;False;False;False;False;False;False;True;True;0;True;_ZWrite;True;0;False;;False;True;4;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;223;0;235;3
WireConnection;223;1;235;4
WireConnection;218;0;159;4
WireConnection;226;0;223;0
WireConnection;189;0;235;3
WireConnection;189;1;235;4
WireConnection;156;0;218;0
WireConnection;156;1;226;0
WireConnection;156;2;189;0
WireConnection;182;0;235;3
WireConnection;215;0;182;0
WireConnection;215;2;159;4
WireConnection;215;3;156;0
WireConnection;227;0;199;0
WireConnection;227;1;159;0
WireConnection;227;2;168;0
WireConnection;169;0;227;0
WireConnection;169;1;236;0
WireConnection;170;0;168;4
WireConnection;170;1;215;0
WireConnection;234;0;169;0
WireConnection;234;1;170;0
ASEEND*/
//CHKSM=270564B9F908A819A0E6C02086B7A211A8722822