#version 330 core

in vec2 vUV;
in vec3 vLocalPos;

out vec4 FragColor;

uniform float uTime;
uniform bool  uShowUV;

uniform vec3  uSeed;
uniform float uSeaLevel;
uniform float uNoiseScale;
uniform float uIceLatitude;
uniform float uCloudCover;
uniform float uCloudSpeed;

// Zgomot 3D (8 colturi, interpolare x -> y -> z)
float hash31(vec3 p)
{
    p = fract(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return fract((p.x + p.y) * p.z);
}

float noise3D(vec3 p)
{
    vec3 i = floor(p);
    vec3 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);

    float n000 = hash31(i + vec3(0.0, 0.0, 0.0));
    float n100 = hash31(i + vec3(1.0, 0.0, 0.0));
    float n010 = hash31(i + vec3(0.0, 1.0, 0.0));
    float n110 = hash31(i + vec3(1.0, 1.0, 0.0));
    float n001 = hash31(i + vec3(0.0, 0.0, 1.0));
    float n101 = hash31(i + vec3(1.0, 0.0, 1.0));
    float n011 = hash31(i + vec3(0.0, 1.0, 1.0));
    float n111 = hash31(i + vec3(1.0, 1.0, 1.0));

    float nx00 = mix(n000, n100, f.x);
    float nx10 = mix(n010, n110, f.x);
    float nx01 = mix(n001, n101, f.x);
    float nx11 = mix(n011, n111, f.x);

    float nxy0 = mix(nx00, nx10, f.y);
    float nxy1 = mix(nx01, nx11, f.y);

    return mix(nxy0, nxy1, f.z);
}

float fbm(vec3 p)
{
    float value = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 5; i++)
    {
        value += amp * noise3D(p);
        p = p * 2.05 + vec3(17.1, 31.7, 11.3);
        amp *= 0.5;
    }
    return value;
}

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    vec3 pos = normalize(vLocalPos);

    // 1. Terenul din zgomot 3D
    float h = fbm(pos * uNoiseScale + uSeed);

    vec3 deepOcean  = vec3(0.03, 0.14, 0.38);
    vec3 shallowSea = vec3(0.10, 0.50, 0.70);
    vec3 beach      = vec3(0.86, 0.79, 0.55);
    vec3 plains     = vec3(0.28, 0.62, 0.24);
    vec3 forest     = vec3(0.11, 0.36, 0.14);
    vec3 mountain   = vec3(0.45, 0.42, 0.39);
    vec3 snow       = vec3(0.95, 0.97, 0.99);

    float seaT = smoothstep(uSeaLevel - 0.16, uSeaLevel, h);
    vec3 waterCol = mix(deepOcean, shallowSea, seaT);

    float landH = (h - uSeaLevel) / max(1.0 - uSeaLevel, 0.001);
    vec3 landCol = beach;
    landCol = mix(landCol, plains,   smoothstep(0.04, 0.12, landH));
    landCol = mix(landCol, forest,   smoothstep(0.22, 0.38, landH));
    landCol = mix(landCol, mountain, smoothstep(0.48, 0.64, landH));
    landCol = mix(landCol, snow,     smoothstep(0.72, 0.85, landH));

    float fw = max(fwidth(h), 0.004);
    vec3 color = mix(waterCol, landCol, smoothstep(uSeaLevel - fw, uSeaLevel + fw, h));

    // 2. Calote polare cu margine neregulata
    float poleNoise = (fbm(pos * 4.2 + uSeed * 0.5) - 0.5) * 0.14;
    float iceMask = smoothstep(uIceLatitude - 0.03, uIceLatitude + 0.03, abs(pos.y) + poleNoise);
    color = mix(color, snow, iceMask);

    // 3. Nori animati in timp
    vec3 cloudPos = pos * 3.2 + vec3(uTime * uCloudSpeed, 0.0, uTime * uCloudSpeed * 0.35) + uSeed * 0.3;
    float c = fbm(cloudPos);
    float cloudMask = smoothstep(0.48, 0.68, c) * clamp(uCloudCover, 0.0, 1.0);
    color = mix(color, vec3(1.0), cloudMask * 0.85);

    // 4. Iluminare simpla (pe sfera, pos este chiar normala)
    vec3 lightDir = normalize(vec3(0.6, 0.5, 0.8));
    float diff = max(dot(pos, lightDir), 0.0);
    float light = 0.25 + 0.75 * diff;

    FragColor = vec4(color * light, 1.0);
}