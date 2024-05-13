#version 330

precision mediump int; 
precision mediump float; 

out vec4 outputColor;

in vec2 texCoord;

uniform sampler2D texture3;
uniform sampler2D texture2;
uniform sampler2D texture1;

uniform float uAlpha;
uniform float uCon; 
uniform vec2 uCenPos; 
uniform vec2 uGCenPos; 
uniform float uRadius; 
uniform float uStd; 
uniform vec2 uWH;

vec4 applyStage1(vec4 color)
{
    color.r = texture(texture2, vec2(max(0.003, min(0.997, color.r)), 0.0)).r;
    color.g = texture(texture2, vec2(max(0.003, min(0.997, color.g)), 0.0)).g; 
    color.b = texture(texture2, vec2(max(0.003, min(0.997, color.b)), 0.0)).b;
    return vec4(color.r, color.g, color.b, 1.0) * uAlpha;
}

vec3 CProcess(vec3 color)
{ 
    vec3 dstcolor; 
    float cValue = uCon/100.0 + 1.0;
    dstcolor = clamp((color - 0.5) *cValue + 0.5,0.0,1.0); 
    return dstcolor; 
}

float WJianbianProcess()
{ 
    float disx,disy,dis,f1,f2,f,pf = uWH.x / uWH.y,x,y; 
    f1 = max(uCenPos.x,1.0 - uCenPos.x);f2 = max(uCenPos.y,1.0 - uCenPos.y); 
    if (pf < 1.0) { 
        disx = (texCoord.x - uCenPos.x) * (texCoord.x - uCenPos.x); 
        if (texCoord.y/pf < uCenPos.y) { 
            y = texCoord.y; 
        } else if ((1.0 - texCoord.y)/pf < (1.0 - uCenPos.y)) { 
            y = pf - (1.0 - texCoord.y); 
        } else { 
            y = uCenPos.y * pf; 
        } 
        disy = (y/pf - uCenPos.y) * (y/pf - uCenPos.y); 
    } else { 
        disy = (texCoord.y - uCenPos.y) * (texCoord.y - uCenPos.y); 
        if (texCoord.x * pf < uCenPos.x) { 
            x = texCoord.x; 
        } else if ((1.0 - texCoord.x)*pf < (1.0 - uCenPos.x)) { 
            x = 1.0/pf - (1.0 - texCoord.x); 
        } else { 
            x = uCenPos.x / pf; 
        } 
        disx = (x * pf - uCenPos.x) * (x * pf - uCenPos.x); 
    } 
    dis = disx + disy; 
    f1 = sqrt(dis)/(sqrt(f1 * f1 + f2 * f2) * uRadius); 
    if (f1 > 1.0) { 
        f = 0.4; 
    } else { 
        f2 = 0.9908 * pow(f1,3.0) -1.4934 * pow(f1,2.0) -0.4974 * f1 + 1.0; 
        f = 0.6 * f2 + 0.4; 
    } 
    return f; 
}

float WEraserProcess()
{ 
    float disx,disy,dis,f1,f2,f,pf = uWH.x / uWH.y,x,y,std1; 
    f1 = max(uGCenPos.x,1.0 - uGCenPos.x);f2 = max(uGCenPos.y,1.0 - uGCenPos.y); 
    std1 = 2.0 * uStd * uStd * (f1 * f1 + f2 * f2); 
    if (pf < 1.0) { 
        disx = (texCoord.x - uGCenPos.x) * (texCoord.x - uGCenPos.x); 
        if (texCoord.y /pf < uCenPos.y) { 
            y = texCoord.y; 
        } else if ((1.0 - texCoord.y)/pf < (1.0 - uCenPos.y)) { 
            y = pf - (1.0 - texCoord.y); 
        } else { 
            y = uCenPos.y * pf; 
        } 
        disy = (y/pf - uGCenPos.y) * (y/pf - uGCenPos.y); 
    } else { 
        disy = (texCoord.y - uCenPos.y) * (texCoord.y - uCenPos.y); 
        if (texCoord.x * pf < uCenPos.x) {  
            x = texCoord.x; 
        } else if ((1.0 - texCoord.x)*pf < (1.0 - uCenPos.x)) { 
            x = 1.0/pf - (1.0 - texCoord.x); 
        } else { 
            x = uCenPos.x / pf; 
        } 
        disx =  (x * pf - uCenPos.x) * (x * pf - uCenPos.x); 
    } 
    dis = disx + disy; 
    f = exp(-1.0 * (disx + disy)/std1); 
    return f; 
}

float BlendOverlayF(float base, float blend)
{ 
    return (base < 0.5 ? (2.0 * base * blend) : (1.0 - 2.0 * (1.0 - base) * (1.0 - blend))); 
}

vec3 BlendOverlay(vec3 base, vec3 blend)
{ 
    vec3 destColor; 
    destColor.r = BlendOverlayF(base.r, blend.r); 
    destColor.g = BlendOverlayF(base.g, blend.g); 
    destColor.b = BlendOverlayF(base.b, blend.b); 
    return destColor; 
}

vec4 applyStage2(vec4 color)
{
    int index = int(color.r * 255.0); 
    float index1 = float(index) / 256.0; 
    color.r = texture(texture3,vec2(index1,0.0)).r; 
    index = int(color.g * 255.0); 
    index1 = float(index) / 256.0; 
    color.g = texture(texture3,vec2(index1,0.0)).r; 
    index = int(color.b * 255.0); 
    index1 = float(index) / 256.0; 
    color.b = texture(texture3,vec2(index1,0.0)).r; 
    vec3 oricolor = vec3(color.r,color.g,color.b); 
    oricolor = CProcess(oricolor); 
    float f1 = WJianbianProcess(); 
    float f2 = WEraserProcess(); 
    float f = (1.0 - f2) * f1 + f2; 
    f = (1.0 - f2) * f + f2; 
    f = 1.0 - f; 
    vec3 dstcolor = BlendOverlay(oricolor,vec3(0.0,0.0,0.0)); 
    dstcolor = dstcolor * f + oricolor * (1.0 - f); 
    return vec4(dstcolor.rgb,1.0) * uAlpha; 
}

void main()
{
    outputColor = texture(texture1, texCoord);
    outputColor = applyStage1(outputColor);
    outputColor = applyStage2(outputColor);
}
