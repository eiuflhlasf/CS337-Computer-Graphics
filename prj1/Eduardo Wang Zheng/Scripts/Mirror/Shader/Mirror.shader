// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FX/MirrorReflection"
{
	Properties
	{
        //主纹理
		_MainTex ("Base (RGB)", 2D) = "white" {}
        //反射纹理
		[HideInInspector] _ReflectionTex ("", 2D) = "white" {}
	}
	SubShader
	{
        //渲染类型设置：不透明
		Tags { "RenderType"="Opaque" }
        //细节层次设为：100
		LOD 100
        //--------------------------------唯一的通道-------------------------------
		Pass {
            //===========开启CG着色器语言编写模块===========
			CGPROGRAM
            //编译指令:告知编译器顶点和片段着色函数的名称
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

            //顶点着色器输入与输出结构
			struct v2f
			{
				float2 uv : TEXCOORD0;     //主纹理坐标
				float4 refl : TEXCOORD1;   //反射纹理坐标
				float4 pos : SV_POSITION;  //像素位置
			};
			float4 _MainTex_ST;

//--------------------------------【顶点着色函数】-----------------------------
//输入：顶点输入结构体
//输出：顶点输出结构体
//---------------------------------------------------------------------------------
			v2f vert(float4 pos : POSITION, float2 uv : TEXCOORD0)
			{
                //实例化一个输入结构体
				v2f o;
                //将三维空间中的坐标投影到了二维窗口
				o.pos = UnityObjectToClipPos (pos);
                //用UnityCG.cginc头文件中内置定义的宏,根据uv坐标来计算真正的纹理上对应的位置（按比例进行二维变换）
				o.uv = TRANSFORM_TEX(uv, _MainTex);
				o.refl = ComputeScreenPos (o.pos);
				return o;
			}
			sampler2D _MainTex;
			sampler2D _ReflectionTex;
//--------------------------------【片段着色函数】-----------------------------
//输入：顶点输出结构体
//输出：float4型的像素颜色值
//---------------------------------------------------------------------------------
			fixed4 frag(v2f i) : SV_Target
			{
                //采样主纹理在对应坐标下的颜色值
				fixed4 tex = tex2D(_MainTex, i.uv);
                //采样反射纹理在对应坐标下的颜色值
				fixed4 refl = tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(i.refl));
                //返回最终的颜色值
				return tex * refl;
			}
			ENDCG
	    }
	}
}
