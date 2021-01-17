Shader "RTR shaders/Gooch shading"{
    Properties{
        _Surface_Color ("Surface Color",Color)=(1,1,1,1)
        _Warm_Color ("Warm Color",Color)=(0.3,0.3,0.0,1)
        _Cool_Color ("Cool Color",Color)=(0,0,0.55)
        _Specular_Color ("Specular Color",Color)=(1,1,1,1)
    }
    SubShader{
        //渲染类型为不透明，渲染队列为默认的渲染队列
        Tags{"RenderType"="Opaque" "Queue"="Geometry"}

        pass{
            //定义该Pass在Unity的流水线中的角色 (只有定义它才能获取到一些Unity内置的光照变量如_LightColor0。ForwardBase：用于正向渲染中，该Pass会计算环境光、最重要的平行光、逐顶点/SH光源和Lightmaps光照贴图
            Tags{"LightMode"="ForwardBase"}
            CGPROGRAM
            //只有引入它才能正确进行获取如光照衰减值等信息
            #pragma multi_compile_fwdbase
            #pragma vertex vert
            #pragma fragment frag
            //这个命名空间中包含_LightColor0(平行光源颜色(即场景自带创建的光源))
            #include "Lighting.cginc"

            fixed4 _Surface_Color;
            fixed4 _Warm_Color;
            fixed4 _Cool_Color;
            fixed4 _Specular_Color;

            struct a2v{
                float4 vertex:POSITION;
                float3 normal:NORMAL;
            };
            struct v2f{
                float4 pos:SV_POSITION;
                float3 worldPos:TEXCOORD1;
                float3 worldnormal:TEXCOORD2;
            };

            v2f vert(a2v v){
                v2f o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.worldnormal=UnityObjectToWorldNormal(v.normal);
                o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f o):SV_TARGET{
                //光源向量
                float3 LightDir=
                normalize(UnityWorldSpaceLightDir(o.worldPos));
                //视线
                float3 ViewDir=
		        normalize(UnityWorldSpaceViewDir(o.worldPos));
                float t=0.5*(dot(o.worldnormal,LightDir)+1);
                float3 r=
			    2*dot(o.worldnormal,LightDir)*o.worldnormal-LightDir;
                float s=saturate(100*dot(r,ViewDir)-97);

                //公式计算Gooch shading模型中的颜色
                fixed4 c=s*_Specular_Color+(1-s)*(t*(_Warm_Color+0.25*_Surface_Color)+(1-t)*(_Cool_Color+0.25*_Surface_Color));

                return c;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
