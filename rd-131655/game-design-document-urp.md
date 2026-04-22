# **ДИЗАЙН-ДОКУМЕНТ: MINECRAFT RD-131655 REMAKE (UNITY URP)**
## **Next-Gen Visual Reimagining с сохранением оригинальной механики**

*Архитектор: AVK Software*
*Дата: 2026-04-09*  
*Целевой движок: Unity 2022.3 LTS + Universal Render Pipeline (URP)*  
*Проект: Современный ремейк с PBR-графикой и сохранением геймплея*  
*Базовый код: thecodeofnotch/rd-131655 (May 10-13, 2009)*

---

## **1. DESIGN INTENT**

**Цель проекта:** Создание визуально современного ремейка классического воксельного движка с использованием полного потенциала Unity URP, сохраняя аутентичность оригинальной механики.

**Ключевые игровые ощущения (НЕИЗМЕННЫ):**
- Свобода первого лица в трёхмерном воксельном пространстве
- Чистое исследование без игровых целей (pure sandbox)
- Ощущение масштаба через генерацию сферических пещер

**Новые визуальные ощущения (РЕМЕЙК):**
- **Photorealistic materials** — PBR-текстуры с нормалями, roughness, AO
- **Dramatic lighting** — Real-time shadows, dynamic GI, volumetric fog
- **Atmospheric depth** — Particle effects (dust motes, cave mist)
- **Environmental storytelling** — Визуальное разнообразие через advanced texturing
- **Cinematic post-processing** — Bloom, color grading, DOF, motion blur

**Визуальная философия:**
```
Original (2009):        Remake (2026):
16×16 pixel texture  →  512×512 PBR texture set
Vertex colors        →  Real-time lighting + shadows
Flat shading         →  Normal mapping + parallax
Linear fog           →  Volumetric fog + god rays
No particles         →  Atmospheric particle systems
Sky gradient         →  Procedural skybox + clouds
```

---

## **2. TARGET PLAYER PSYCHOLOGY**

**Профиль игрока (РАСШИРЕН):**
- **Тип:** Graphics Enthusiasts + Nostalgia Seekers + Tech Showcasers
- **Мотивация:** 
  - **Original fans:** Nostalgia через современную призму
  - **New players:** Визуальное wow-effect при первом входе в пещеру
  - **Content creators:** Screenshot/video-friendly graphics
- **Skill Profile:** Ценят визуальное качество, готовы к high-end hardware

**Эмоциональная реакция (УСИЛЕНА ГРАФИКОЙ):**
- **Awe (благоговение):** Volumetric light shafts в пещерах
- **Wonder (изумление):** Photorealistic rock textures с деталями
- **Immersion (погружение):** Particle systems + ambient audio
- **Pride (гордость):** "Look at my screenshots!" фактор

**Когнитивная нагрузка:** Minimal (механика неизменна)  
**Визуальная нагрузка:** High (но не overwhelming — tasteful graphics)

---

## **3. CORE GAME EQUATION (НЕИЗМЕННА)**

```
GameState = (PlayerPos, PlayerRot, VoxelGrid, ChunkVisibility)

ΔPlayerPos = f(Input, Δt, Gravity, Collision(VoxelGrid))
ΔChunkVisibility = Frustum(Camera) ∩ ChunkBounds

RenderCost = Σ(VisibleChunks) × [
    VertexCount(chunk) 
    + LightingCost(PBR) 
    + ShadowCost(realtime)
    + PostProcessCost
]

CoreLoop: Input → Physics → Collision → MeshGen → Lighting → PostProcess → Display
```

**Математическое ядро (ИДЕНТИЧНО ОРИГИНАЛУ):**
- **State Space (S):** `(position, velocity, rotation, grounded)`
- **Action Space (A):** `{WASD, Jump, Reset, Look}`
- **Transition Function (T):** `S_{t+1} = CollisionResolve(S_t + Physics)`
- **Reward Function (R):** None (sandbox)

**Визуальное уравнение (НОВОЕ):**
```
FinalPixelColor = PostProcess(
    DirectLight(PBR_Material, Normal, Roughness, Metallic)
    + IndirectLight(GI, AO)
    + VolumetricFog
    + ParticleContribution
)
```

---

## **4. PRIMARY LOOP (МЕХАНИКА НЕИЗМЕННА)**

**Частота:** 50 Hz physics (FixedUpdate), variable rendering (Update)

```
EVERY FRAME (Update):
  1. Camera.Rotate(MouseInput) → unchanged
  2. UpdateDynamicLights() → NEW: particle lights, animated torches
  3. UpdateParticleSystems() → NEW: dust, fog particles
  4. RebuildDirtyChunks(limit: 2/frame) → unchanged logic, PBR meshes
  5. URP auto-renders: Shadows → Opaque → Sky → Transparent → Post

EVERY FIXED UPDATE (FixedUpdate, 50 Hz):
  [АБСОЛЮТНО ИДЕНТИЧНО ОРИГИНАЛУ]
  1. Read Input (WASD, Space, R)
  2. Calculate movement vector
  3. Apply gravity: velocity.y -= 0.01f
  4. Resolve collision (BoxCast)
  5. Apply friction
  6. transform.position += velocity * dt
```

**ВИЗУАЛЬНЫЕ ДОПОЛНЕНИЯ (не влияют на геймплей):**
```csharp
void Update() {
    // Оригинальный код
    HandleCameraRotation();
    RebuildDirtyChunks();
    
    // НОВОЕ: Visual enhancements only
    UpdateFootstepParticles();      // Dust when walking
    UpdateBreathingParticles();     // Cold breath effect
    UpdateDynamicLightIntensity();  // Flickering torches
    UpdateCameraEffects();          // Head bob, FOV kick
}
```

---

## **5. SECONDARY / META LOOP (НЕИЗМЕНЁН)**

**Отсутствует в базовой версии.**  
- Нет прогрессии  
- Сохранение через JSON  
- **Meta-loop:** Explore → Save → Restart  

**ВИЗУАЛЬНОЕ РАСШИРЕНИЕ (опционально):**
- **Photo Mode:** F12 → pause game, free camera, filter controls
- **Replay System:** Record 30-sec clips для sharing
- **Lighting presets:** Day/Night/Sunset modes (не влияет на геймплей)

---

## **6. SYSTEM VARIABLES (МЕХАНИКА НЕИЗМЕННА)**

### **World State (WorldData.cs) — ИДЕНТИЧНО**
```csharp
public const int WIDTH = 256;
public const int HEIGHT = 256;
public const int DEPTH = 64;

private byte[] blocks; // Unchanged voxel grid
private int[] lightDepths; // Unchanged shadow map
```

### **Visual State (NEW: VisualManager.cs)**
```csharp
public class VisualManager : MonoBehaviour {
    [Header("Lighting")]
    public Light mainDirectionalLight;      // Sun/ambient light
    public LightingSettings lightingPreset; // URP lighting config
    public float globalLightIntensity = 1.2f;
    
    [Header("Fog")]
    public bool volumetricFog = true;
    public Color fogColor = new Color(0.055f, 0.043f, 0.039f);
    public float fogDensity = 0.05f;
    public float fogHeightFalloff = 0.1f;
    
    [Header("Post-Processing")]
    public Volume globalVolume;            // URP Volume
    public Bloom bloomSettings;
    public ColorAdjustments colorGrading;
    public DepthOfField depthOfField;
    public MotionBlur motionBlur;
    
    [Header("Particles")]
    public ParticleSystem dustMotes;       // Cave atmosphere
    public ParticleSystem cavernFog;       // Ground fog
    public ParticleSystem footstepDust;    // Player movement
    
    [Header("Materials")]
    public Material stonePBR;              // Lit PBR shader
    public Material grassPBR;
    public Texture2D stoneAlbedo;          // 512×512
    public Texture2D stoneNormal;          // Normal map
    public Texture2D stoneHeight;          // Parallax map
    public Texture2D stoneAO;              // Ambient occlusion
    public Texture2D stoneRoughness;       // PBR roughness
}
```

---

## **7. VISUAL ENHANCEMENT DESIGN (НОВЫЙ РАЗДЕЛ)**

### **7.1. PBR Material Pipeline**

**Текстурная пирамида (для каждого блока):**
```
STONE BLOCK (example):
├── stone_albedo_512.png        // Base color (512×512)
├── stone_normal_512.png        // Normal map (RGB)
├── stone_height_512.png        // Height/parallax (Grayscale)
├── stone_ao_512.png            // Ambient occlusion (Grayscale)
├── stone_roughness_512.png     // PBR roughness (Grayscale)
└── stone_metallic_512.png      // PBR metallic (Grayscale, обычно 0 для камня)

GRASS BLOCK:
├── grass_top_albedo_512.png    // Отдельная текстура для верха
├── grass_side_albedo_512.png   // Боковая сторона (grass + dirt)
└── ... (полный набор для каждой стороны)
```

**Shader Graph Architecture:**
```
VoxelPBR.shadergraph:
  Inputs:
    - Position (vertex)
    - Normal (vertex)
    - UV (vertex)
    - BlockType (vertex color R channel)
    - FaceDirection (vertex color G channel, 0-5 для 6 граней)
    
  Nodes:
    1. TriplanarMapping → устраняет UV-стретчинг
    2. BlockTypeSwitch → выбор текстур по blockType
    3. FaceTextureSelect → разные текстуры для top/side/bottom
    4. ParallaxOcclusionMapping → псевдо-3D глубина
    5. PBRMaster → final output (Albedo, Normal, Metallic, Smoothness, AO)
    
  Outputs:
    - BaseColor (RGB)
    - Normal (RGB)
    - Metallic (R)
    - Smoothness (R) = 1 - Roughness
    - Occlusion (R)
    - Emission (RGB, для светящихся блоков)
```

**Пример Shader Graph узла (псевдокод):**
```
// TriplanarMapping.hlsl
float3 TriplanarSample(Texture2D tex, float3 worldPos, float3 worldNormal) {
    // Sample texture с трех осей
    float3 blendWeights = abs(worldNormal);
    blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);
    
    float3 xProjection = tex.Sample(sampler_tex, worldPos.zy);
    float3 yProjection = tex.Sample(sampler_tex, worldPos.xz);
    float3 zProjection = tex.Sample(sampler_tex, worldPos.xy);
    
    return xProjection * blendWeights.x +
           yProjection * blendWeights.y +
           zProjection * blendWeights.z;
}
```

### **7.2. Lighting System (URP)**

**Real-time Directional Light:**
```csharp
public class CaveLightingManager : MonoBehaviour {
    public Light directionalLight; // Main "sun" light
    
    void Start() {
        // Setup для пещерной атмосферы
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 0.8f; // Приглушённо для пещер
        directionalLight.color = new Color(0.9f, 0.85f, 0.75f); // Тёплый оттенок
        
        // Shadows
        directionalLight.shadows = LightShadows.Soft;
        directionalLight.shadowStrength = 0.6f;
        directionalLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
        
        // Rotation для интересного освещения
        directionalLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
    }
}
```

**Dynamic Point Lights (NEW):**
```csharp
// Для будущих факелов/лавы
public class DynamicBlockLight : MonoBehaviour {
    private Light pointLight;
    
    void Start() {
        pointLight = gameObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 8f;        // 8 блоков радиус
        pointLight.intensity = 2f;
        pointLight.color = new Color(1f, 0.6f, 0.2f); // Orange для факела
        pointLight.shadows = LightShadows.Soft;
        pointLight.renderMode = LightRenderMode.ForcePixel;
        
        // Flickering effect
        StartCoroutine(FlickerLight());
    }
    
    IEnumerator FlickerLight() {
        while (true) {
            float flicker = Random.Range(0.9f, 1.1f);
            pointLight.intensity = 2f * flicker;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }
    }
}
```

**Baked Global Illumination (опционально):**
```
Для статичных чанков можно использовать Lightmapping:
  1. Mark chunks as "Lightmap Static"
  2. Setup Light Probes в пещерах
  3. Bake через Progressive GPU Lightmapper
  4. Result: Indirect lighting без runtime cost
  
Performance trade-off:
  ✓ Beautiful ambient light bounces
  ✗ Увеличенный размер build (lightmap textures)
  ✗ Нельзя для динамически изменяемых блоков
  
Решение: Hybrid approach
  - Baked GI для больших статичных секций
  - Real-time для player-modified areas
```

### **7.3. Volumetric Fog (URP)**

**URP Fog Volume:**
```csharp
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumetricFogSetup : MonoBehaviour {
    private Volume fogVolume;
    
    void Start() {
        // Create Volume GameObject
        GameObject volObj = new GameObject("FogVolume");
        fogVolume = volObj.AddComponent<Volume>();
        fogVolume.isGlobal = true;
        fogVolume.priority = 1;
        
        // Setup profile
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        // Add volumetric fog
        var fog = profile.Add<Fog>();
        fog.enabled.Override(true);
        fog.mode.Override(FogMode.Exponential);
        fog.color.Override(new Color(0.055f, 0.043f, 0.039f)); // Brown cave fog
        fog.density.Override(0.05f);
        
        fogVolume.profile = profile;
    }
}
```

**Shader-based Volumetric Rays (advanced):**
```glsl
// VolumetricFog.shader (fragment shader)
float4 frag(v2f i) : SV_Target {
    // Sample depth
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
    float linearDepth = Linear01Depth(depth, _ZBufferParams);
    
    // Raymarch volumetric fog
    float3 rayDir = normalize(i.viewRay);
    float3 rayStart = _WorldSpaceCameraPos;
    
    float fogAmount = 0;
    int steps = 32;
    for (int i = 0; i < steps; i++) {
        float t = (i / (float)steps) * linearDepth;
        float3 samplePos = rayStart + rayDir * t;
        
        // 3D noise для variation
        float noise = SimplexNoise(samplePos * 0.1 + _Time.y * 0.05);
        fogAmount += _FogDensity * (1 + noise * 0.3) / steps;
    }
    
    float3 fogColor = _FogColor.rgb;
    return float4(fogColor, saturate(fogAmount));
}
```

### **7.4. Particle Systems**

**Dust Motes (атмосферные частицы):**
```csharp
public class CaveDustParticles : MonoBehaviour {
    void SetupDustMotes() {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        
        var main = ps.main;
        main.startLifetime = 15f;
        main.startSpeed = 0.1f;
        main.startSize = 0.02f;
        main.maxParticles = 2000;
        
        var emission = ps.emission;
        emission.rateOverTime = 50;
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(50, 20, 50); // Large volume
        
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.05f, 0.1f); // Slow upward drift
        
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = Resources.Load<Material>("Particles/DustMote");
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 0;
    }
}
```

**Footstep Dust (reactive particles):**
```csharp
public class FootstepDustEmitter : MonoBehaviour {
    public ParticleSystem dustPrefab;
    private ParticleSystem currentDust;
    private float stepTimer = 0f;
    private const float STEP_INTERVAL = 0.5f; // 2 steps/second
    
    void Update() {
        // Detect player movement
        Vector3 velocity = GetComponent<PlayerController>().velocity;
        float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        
        if (horizontalSpeed > 0.5f && IsGrounded()) {
            stepTimer += Time.deltaTime;
            
            if (stepTimer >= STEP_INTERVAL) {
                EmitDust();
                stepTimer = 0f;
            }
        }
    }
    
    void EmitDust() {
        Vector3 spawnPos = transform.position - Vector3.up * 0.9f; // At feet
        currentDust = Instantiate(dustPrefab, spawnPos, Quaternion.identity);
        currentDust.Play();
        Destroy(currentDust.gameObject, 2f);
    }
}
```

### **7.5. Post-Processing Stack**

**URP Post-Processing Volume:**
```csharp
public class PostProcessingSetup : MonoBehaviour {
    void SetupPostProcessing() {
        var volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;
        
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        // === BLOOM ===
        var bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.threshold.Override(0.8f);      // Bright areas only
        bloom.intensity.Override(0.4f);      // Subtle glow
        bloom.scatter.Override(0.7f);        // Wide spread
        bloom.tint.Override(new Color(1f, 0.95f, 0.85f)); // Warm tint
        
        // === COLOR ADJUSTMENTS ===
        var colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.postExposure.Override(0.2f);           // Slightly brighter
        colorAdj.contrast.Override(10f);                // More contrast
        colorAdj.saturation.Override(-5f);              // Slightly desaturated (реализм)
        colorAdj.colorFilter.Override(new Color(1f, 0.98f, 0.95f)); // Warm filter
        
        // === TONEMAPPING ===
        var tonemapping = profile.Add<Tonemapping>();
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES); // Filmic look
        
        // === VIGNETTE ===
        var vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.25f);  // Subtle darkening at edges
        vignette.smoothness.Override(0.4f);
        vignette.color.Override(Color.black);
        
        // === DEPTH OF FIELD (optional, для cinematic mode) ===
        var dof = profile.Add<DepthOfField>();
        dof.active = false; // Выключено по умолчанию (gameplay)
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focusDistance.Override(10f);
        dof.aperture.Override(5.6f);
        
        // === MOTION BLUR (optional) ===
        var motionBlur = profile.Add<MotionBlur>();
        motionBlur.active = false; // Опционально для performance
        motionBlur.intensity.Override(0.3f);
        
        // === AMBIENT OCCLUSION ===
        var ao = profile.Add<ScreenSpaceAmbientOcclusion>();
        ao.active = true;
        ao.intensity.Override(1.5f);        // Strong AO для глубины
        ao.radius.Override(0.5f);
        ao.fallOffDistance.Override(100f);
        
        volume.profile = profile;
    }
}
```

**Dynamic Post-Processing (gameplay-driven):**
```csharp
public class DynamicPostEffects : MonoBehaviour {
    private Volume volume;
    private ChromaticAberration chromaticAb;
    private LensDistortion lensDistortion;
    
    void Update() {
        // Chromatic aberration при быстром движении
        float speed = GetComponent<PlayerController>().velocity.magnitude;
        chromaticAb.intensity.Override(Mathf.Lerp(0, 0.3f, speed / 10f));
        
        // Lens distortion при падении
        float fallSpeed = -GetComponent<PlayerController>().velocity.y;
        if (fallSpeed > 5f) {
            lensDistortion.intensity.Override(Mathf.Lerp(0, -0.2f, (fallSpeed - 5f) / 10f));
        } else {
            lensDistortion.intensity.Override(0);
        }
    }
}
```

### **7.6. Advanced Texture Techniques**

**Texture Array для вариаций:**
```csharp
// Вместо одной текстуры камня — 4 вариации
public class TextureArraySetup : MonoBehaviour {
    public Texture2D[] stoneVariations; // 4 текстуры: stone_01, stone_02, etc.
    
    Texture2DArray CreateTextureArray() {
        int width = stoneVariations[0].width;
        int height = stoneVariations[0].height;
        
        Texture2DArray texArray = new Texture2DArray(
            width, height, stoneVariations.Length,
            TextureFormat.RGBA32, true
        );
        
        for (int i = 0; i < stoneVariations.Length; i++) {
            Graphics.CopyTexture(stoneVariations[i], 0, 0, texArray, i, 0);
        }
        
        texArray.Apply(updateMipmaps: true);
        return texArray;
    }
}

// В Shader Graph:
// Sample Texture2D Array node с random index per block
```

**Detail Maps (Micro-detail):**
```glsl
// В fragment shader добавляем high-frequency detail
float3 SampleDetailMap(float2 uv, float3 baseColor) {
    // Detail texture tiled 16x для микротекстуры
    float3 detail = tex2D(_DetailMap, uv * 16).rgb;
    
    // Blend с base color
    return baseColor * detail;
}
```

---

## **8. MECHANICS DERIVATION (НЕИЗМЕННА)**

### **8.1. Процедурная генерация (ИДЕНТИЧНА)**
```csharp
// WorldGenerator.cs — ТОЧНО КАК В ОРИГИНАЛЕ
public static void GenerateCaves(WorldData world, int seed) {
    Random.InitState(seed);
    
    // Заполнить мир
    FillWithStone(world);
    
    // 10,000 сферических пещер
    for (int i = 0; i < 10000; i++) {
        GenerateCaveSphere(world); // Unchanged algorithm
    }
    
    // Calculate lighting
    world.CalculateLightDepths(); // Unchanged
}
```

**ВИЗУАЛЬНОЕ ДОПОЛНЕНИЕ (не влияет на генерацию):**
```csharp
void GenerateCaveSphere(WorldData world) {
    // ... оригинальный код ...
    
    // НОВОЕ: Добавить визуальные детали
    if (Random.value < 0.01f) { // 1% chance
        SpawnStalactite(caveCenter); // Чисто визуальный объект
    }
}

void SpawnStalactite(Vector3 position) {
    // Procedural mesh для stalactite/stalagmite
    // НЕ влияет на collision (только визуал)
}
```

### **8.2. Chunk Mesh Generation (ОБНОВЛЁН ПОД PBR)**

```csharp
public class ChunkMeshBuilderPBR {
    // Оригинальная логика сохранена
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    
    // НОВОЕ: Additional channels для PBR
    private List<Vector3> normals = new List<Vector3>();
    private List<Vector4> tangents = new List<Vector4>();
    private List<Color> vertexColors = new List<Color>(); // Block metadata
    
    public Mesh BuildChunkMesh(Chunk chunk, WorldData world) {
        ClearBuffers();
        
        // Оригинальная логика iteration
        for (int x = chunk.minBounds.x; x < chunk.maxBounds.x; x++) {
            for (int y = chunk.minBounds.y; y < chunk.maxBounds.y; y++) {
                for (int z = chunk.minBounds.z; z < chunk.maxBounds.z; z++) {
                    
                    byte blockType = world.GetBlock(x, y, z);
                    if (blockType == 0) continue; // Air
                    
                    // ОРИГИНАЛЬНАЯ логика face culling
                    AddBlockFacesPBR(x, y, z, blockType, world);
                }
            }
        }
        
        // Создать mesh с PBR данными
        return CreatePBRMesh();
    }
    
    void AddBlockFacesPBR(int x, int y, int z, byte blockType, WorldData world) {
        // Top face (+Y)
        if (!world.IsSolidBlock(x, y + 1, z)) {
            Vector3 v0 = new Vector3(x,   y+1, z);
            Vector3 v1 = new Vector3(x+1, y+1, z);
            Vector3 v2 = new Vector3(x+1, y+1, z+1);
            Vector3 v3 = new Vector3(x,   y+1, z+1);
            
            // ОРИГИНАЛ: Только vertices, triangles, UVs
            // НОВОЕ: + normals, tangents, vertex colors
            AddQuadPBR(v0, v1, v2, v3, 
                       Vector3.up,              // Normal
                       blockType,               // Block ID (для shader)
                       0,                       // Face ID (top = 0)
                       GetUVRectForBlock(blockType, "top"));
        }
        
        // Bottom, North, South, East, West — аналогично
    }
    
    void AddQuadPBR(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
                    Vector3 normal, byte blockType, int faceID, Rect uvRect) {
        int vertIndex = vertices.Count;
        
        // Vertices (unchanged)
        vertices.Add(v0);
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        
        // Triangles (unchanged)
        triangles.Add(vertIndex + 0);
        triangles.Add(vertIndex + 2);
        triangles.Add(vertIndex + 1);
        triangles.Add(vertIndex + 0);
        triangles.Add(vertIndex + 3);
        triangles.Add(vertIndex + 2);
        
        // UVs (unchanged)
        uvs.Add(new Vector2(uvRect.xMin, uvRect.yMin));
        uvs.Add(new Vector2(uvRect.xMax, uvRect.yMin));
        uvs.Add(new Vector2(uvRect.xMax, uvRect.yMax));
        uvs.Add(new Vector2(uvRect.xMin, uvRect.yMax));
        
        // НОВОЕ: Normals для lighting
        for (int i = 0; i < 4; i++) {
            normals.Add(normal);
        }
        
        // НОВОЕ: Tangents для normal mapping
        Vector3 tangent = CalculateTangent(normal);
        Vector4 tangent4 = new Vector4(tangent.x, tangent.y, tangent.z, 1);
        for (int i = 0; i < 4; i++) {
            tangents.Add(tangent4);
        }
        
        // НОВОЕ: Vertex colors для metadata
        // R channel = block type, G channel = face ID
        Color metadata = new Color(
            blockType / 255f,   // Block type
            faceID / 6f,        // Face ID (0-5)
            0,                  // Reserved
            1                   // Alpha
        );
        for (int i = 0; i < 4; i++) {
            vertexColors.Add(metadata);
        }
    }
    
    Mesh CreatePBRMesh() {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Support >65k verts
        
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);       // НОВОЕ
        mesh.SetTangents(tangents);     // НОВОЕ
        mesh.SetColors(vertexColors);   // НОВОЕ
        
        mesh.RecalculateBounds();
        mesh.Optimize();
        
        return mesh;
    }
}
```

### **8.3. Collision Detection (НЕИЗМЕННА)**

```csharp
// PlayerController.cs — ФИЗИКА ИДЕНТИЧНА
void MoveWithCollision(Vector3 deltaMove) {
    // Точно такой же 3-pass AABB sweep
    // Physics.BoxCast для Y → X → Z
    // [КОД ИДЕНТИЧЕН ПРЕДЫДУЩЕМУ ДОКУМЕНТУ]
}
```

---

## **9. DECISION DENSITY ANALYSIS (НЕИЗМЕНЁН)**

**Частота решений:** 1-2 decisions/minute (unchanged)

**ВИЗУАЛЬНОЕ ВЛИЯНИЕ:**
- **Screenshot decision:** "Остановиться для скриншота?" — NEW
- **Lighting exploration:** "Идти к источнику света?" — NEW (если добавлены факелы)

---

## **10. FAILURE MODES & DOMINANT STRATEGIES (МЕХАНИКА НЕИЗМЕННА)**

**Оригинальные баги сохранены:**
- ❌ Tunneling bug
- ❌ Spawn in solid block
- ❌ Chunk thrashing

**НОВЫЕ визуальные проблемы:**

### **10.1. Performance Cliff (URP Overhead)**
```
Проблема: PBR shaders + real-time shadows = 10x rendering cost
  Original: 60 FPS на Intel HD Graphics
  Remake: 60 FPS требует GTX 1060 или лучше

Решение: Scalability settings
  - Low: Unlit shaders, no shadows, simple fog
  - Medium: PBR без shadows, vertex lighting
  - High: Full PBR + shadows + post-processing
  - Ultra: Volumetric fog + all effects
```

### **10.2. Texture Memory Explosion**
```
Оригинал: 256×256 atlas = 256 KB
Remake: 512×512 × 6 maps × 10 blocks = 15 MB

Решение: Texture streaming
  - Load только visible chunk textures
  - Mipmaps для distant chunks
  - Texture compression (DXT5/BC7)
```

### **10.3. Shadow Cascade Pop-In**
```
Проблема: Видимые переходы между shadow cascades

Решение:
  shadowCascadeBlendDistance = 0.3f; // Smooth transitions
  shadowDistance = 50f; // Не слишком далеко для пещер
```

---

## **11. BALANCE MODEL (НЕИЗМЕНЁН)**

**Все физические константы идентичны:**
```csharp
public float groundAcceleration = 0.4f;  // Unchanged
public float gravity = 0.1f;             // Unchanged
public float jumpVelocity = 2.4f;        // Unchanged
// ... все остальные константы неизменны
```

**ВИЗУАЛЬНЫЙ БАЛАНС (новый):**
```csharp
[Header("Visual Balance")]
public float bloomIntensity = 0.4f;      // Не слишком много
public float fogDensity = 0.05f;         // Читаемая видимость
public float particleDensity = 0.5f;     // Atmospheric но не overwhelming
public float shadowStrength = 0.6f;      // Visible но не чрезмерно dark
```

---

## **12. SCOPE & TECHNICAL REQUIREMENTS**

### **12.1. Unity URP Setup**

**Версия:** Unity 2022.3 LTS  
**Render Pipeline:** Universal Render Pipeline (URP) 14.x

**URP Asset Settings:**
```
Rendering:
  - Renderer: UniversalRenderer (Forward+)
  - Depth Texture: Enabled (для DOF, SSAO)
  - Opaque Texture: Disabled (не нужно для voxels)
  
Lighting:
  - Main Light: Enabled
  - Additional Lights: Per Pixel (для динамических факелов)
  - Max Additional Lights: 8
  - Main Light Shadows: Enabled
  - Shadow Resolution: 2048
  - Shadow Cascades: 2 (оптимально для пещер)
  - Shadow Distance: 50m
  
Post-Processing:
  - Enabled in URP Asset
  - HDR: Enabled
  - MSAA: 4x (если performance позволяет)
  - Render Scale: 1.0 (native resolution)
```

**Forward+ Renderer Features:**
```
Add Renderer Features:
  ✓ Screen Space Ambient Occlusion (SSAO)
  ✓ Decal (для будущих деталей)
  ✓ Screen Space Shadows (soft shadows)
```

### **12.2. Required Packages**

```json
{
  "com.unity.render-pipelines.universal": "14.0.8",
  "com.unity.shadergraph": "14.0.8",
  "com.unity.postprocessing": "3.2.2",
  "com.unity.visualeffectgraph": "14.0.8",  // Для advanced particles
  "com.unity.textmeshpro": "3.0.6",
  "com.unity.cinemachine": "2.9.7"          // Для camera effects
}
```

### **12.3. Material Authoring Workflow**

**Texture Creation Pipeline:**
```
Tools:
  - Substance Designer → PBR texture generation
  - Photoshop/GIMP → Manual authoring
  - Materialize → Auto-generate normals/AO from albedo
  - Crazy Bump → Height/normal map creation

Per-Block Requirements:
  1. Albedo Map (512×512, PNG, sRGB)
     → Color information, no lighting
     
  2. Normal Map (512×512, PNG, Linear)
     → Tangent-space normals, RGB channels
     → Generated from height map или hand-painted
     
  3. Height Map (512×512, PNG, Linear)
     → Grayscale, для parallax occlusion
     → 0 (low) to 1 (high)
     
  4. AO Map (512×512, PNG, Linear)
     → Ambient occlusion baked
     → 0 (occluded) to 1 (exposed)
     
  5. Roughness Map (512×512, PNG, Linear)
     → Surface roughness
     → 0 (smooth/glossy) to 1 (rough/matte)
     
  6. Metallic Map (512×512, PNG, Linear)
     → Usually 0 для камня, 1 для металлов
```

**Example Substance Designer Graph:**
```
StoneGenerator.sbsar:
  Inputs:
    - Scale: 0.5-2.0 (texture tile size)
    - RoughnessAmount: 0.6-0.9
    - CrackDensity: 0.0-1.0
    
  Outputs:
    - BaseColor (sRGB)
    - Normal (Linear, OpenGL format)
    - Height (Linear)
    - AO (Linear)
    - Roughness (Linear)
```

### **12.4. Performance Targets (UPDATED FOR URP)**

**Target Hardware:** RTX 3060 / RX 6600 XT (mid-range 2024)

```
Quality Presets:

=== ULTRA (RTX 3070+) ===
FPS: 90+ at 1080p, 60+ at 1440p
  - Full PBR materials (all 5 maps)
  - Real-time shadows (4096 resolution)
  - Volumetric fog (full quality)
  - SSAO (high quality)
  - All post-processing enabled
  - Particle density: 100%
  - Texture resolution: 512×512

=== HIGH (RTX 3060 / RX 6600 XT) ===
FPS: 60+ at 1080p
  - Full PBR materials
  - Real-time shadows (2048 resolution)
  - Standard fog
  - SSAO (medium quality)
  - Post-processing enabled
  - Particle density: 70%
  - Texture resolution: 512×512

=== MEDIUM (GTX 1660 / RX 580) ===
FPS: 60+ at 1080p
  - PBR без parallax
  - Simplified shadows (1024 resolution)
  - Standard fog
  - No SSAO
  - Limited post-processing (bloom + color grading only)
  - Particle density: 40%
  - Texture resolution: 256×256

=== LOW (GTX 1050 / Intel Xe) ===
FPS: 60+ at 720p
  - Unlit shaders (vertex lighting)
  - No shadows
  - Linear fog
  - No post-processing
  - Particle density: 10%
  - Texture resolution: 128×128
```

**Unity Profiler Targets (HIGH preset):**
```
CPU:
  - Main Thread: ≤ 12ms (60 FPS)
    - Scripts: ≤ 3ms
    - Rendering: ≤ 5ms
    - Physics: ≤ 2ms
    - Post-Processing: ≤ 2ms
    
GPU:
  - Frame Time: ≤ 14ms (60 FPS)
    - Shadow Rendering: ≤ 3ms
    - Opaque Geometry: ≤ 7ms
    - Post-Processing: ≤ 3ms
    - Particles: ≤ 1ms
    
Memory:
  - Mesh Memory: ~100 MB (PBR meshes larger)
  - Texture Memory: ~500 MB (high-res textures)
  - Shader Memory: ~50 MB
  - Total: ~800 MB (vs 200 MB в базовой версии)
  
Draw Calls:
  - Opaque: ≤ 150 (все чанки batched)
  - Transparent: ≤ 50 (particles)
  - Shadow Casters: ≤ 150
  - Total: ≤ 350 draw calls
```

---

## **13. MVP CUTLINE (URP REMAKE)**

### **✅ MUST HAVE (Core + Visual)**

**Механика (unchanged):**
1. ✅ WorldGenerator.cs — Procedural generation
2. ✅ PlayerController.cs — Movement physics
3. ✅ ChunkManager.cs — Chunk system
4. ✅ Collision system

**Визуал (URP essentials):**
5. ✅ **PBR Material System** — Albedo + Normal + Roughness для stone/grass
6. ✅ **URP Lit Shader** — Basic PBR lighting
7. ✅ **Real-time Directional Light** — Мягкое освещение пещер
8. ✅ **Soft Shadows** — 2048 resolution, 2 cascades
9. ✅ **Fog System** — URP fog (exponential)
10. ✅ **Basic Post-Processing** — Bloom + Color Grading + Tonemapping
11. ✅ **Particle System** — Dust motes (атмосферные частицы)

### **🔶 NICE TO HAVE (Enhanced Visual)**

12. 🔶 **Parallax Occlusion Mapping** — Псевдо-3D глубина на текстурах
13. 🔶 **SSAO** — Screen-space ambient occlusion
14. 🔶 **Dynamic Point Lights** — Для будущих факелов
15. 🔶 **Advanced Particles** — Footstep dust, cave fog, breath
16. 🔶 **Volumetric Fog** — Shader-based light shafts
17. 🔶 **DOF + Motion Blur** — Cinematic effects (toggle)
18. 🔶 **Texture Variations** — 4 варианта камня через Texture2DArray
19. 🔶 **Baked GI** — Lightmaps для статичных областей
20. 🔶 **Photo Mode** — Free camera + filter controls

### **❌ OUT OF SCOPE (Future DLC)**

21. ❌ Water shaders (reflections, refractions)
22. ❌ Weather system (rain, snow particles)
23. ❌ Advanced vegetation (grass, flowers)
24. ❌ Animated creatures/mobs
25. ❌ Ray-traced lighting (DXR)
26. ❌ Multiplayer networking

---

## **14. IMPLEMENTATION ROADMAP (URP)**

### **Phase 1: URP Migration (Week 1)**
```
✓ Create new URP project (2022.3 LTS)
✓ Setup URP Asset (Forward+ renderer)
✓ Configure quality settings (Ultra/High/Medium/Low)
✓ Import post-processing packages
✓ Setup scene lighting (directional light + ambient)
```

### **Phase 2: Core Mechanics (Week 2)**
```
✓ Port WorldData.cs (unchanged)
✓ Port WorldGenerator.cs (unchanged)
✓ Port PlayerController.cs (unchanged)
✓ Port ChunkManager.cs (unchanged)
✓ Test: Генерация работает, физика работает
```

### **Phase 3: PBR Texture Pipeline (Week 3)**
```
✓ Create Substance Designer graphs:
  - Stone PBR set (albedo, normal, height, AO, roughness)
  - Grass PBR set (top/side variations)
  
✓ Export at 512×512 resolution
✓ Import в Unity (correct import settings)
✓ Setup texture atlases (if needed)
```

### **Phase 4: PBR Mesh Generation (Week 4)**
```
✓ Extend ChunkMeshBuilder → ChunkMeshBuilderPBR
✓ Add normal/tangent generation
✓ Add vertex color metadata (block type, face ID)
✓ Test single chunk rendering с PBR materials
```

### **Phase 5: Shader Graph Development (Week 5)**
```
✓ Create VoxelPBR.shadergraph:
  - Triplanar mapping node
  - Block type switching
  - Face-specific texture selection
  - Parallax occlusion mapping
  - PBR Master output
  
✓ Test на различных блоках
✓ Optimize shader (shader variants, keyword stripping)
```

### **Phase 6: Lighting & Shadows (Week 6)**
```
✓ Setup URP main light (directional)
✓ Configure shadow cascades
✓ Add dynamic point lights (torch prefab)
✓ Setup Light Probes (для indirect lighting)
✓ Test shadow performance (2048 vs 4096 resolution)
```

### **Phase 7: Fog & Atmosphere (Week 7)**
```
✓ Setup URP fog (exponential)
✓ Implement shader-based volumetric fog (optional)
✓ Create dust mote particle system
✓ Create cave fog particle system
✓ Test fog density на различных дистанциях
```

### **Phase 8: Post-Processing (Week 8)**
```
✓ Setup global Volume
✓ Configure Bloom (threshold, intensity)
✓ Configure Color Adjustments (exposure, contrast, saturation)
✓ Configure Tonemapping (ACES)
✓ Add Vignette (subtle)
✓ Add SSAO (URP renderer feature)
✓ Test performance impact
```

### **Phase 9: Particle Systems (Week 9)**
```
✓ Atmospheric dust motes (always active)
✓ Footstep dust (triggered by player movement)
✓ Breath particles (cold cave effect, optional)
✓ Optimize particle counts (LOD system)
```

### **Phase 10: Optimization & Scalability (Week 10)**
```
✓ Implement quality presets (Ultra/High/Medium/Low)
✓ Dynamic texture resolution switching
✓ Shader LOD system (simple shaders для distant chunks)
✓ Particle density scaling
✓ Shadow distance/resolution scaling
✓ Profile на target hardware (RTX 3060, GTX 1660, GTX 1050)
```

### **Phase 11: Polish & Details (Week 11)**
```
✓ Texture variations (Texture2DArray)
✓ Detail maps (micro-surface detail)
✓ Light flickering (dynamic lights)
✓ Camera effects (head bob, FOV kick, chromatic aberration)
✓ Audio integration (footsteps match particles)
```

### **Phase 12: Photo Mode & Extras (Week 12+)**
```
○ Photo mode implementation (Cinemachine free-look)
○ Lighting presets (day/night/sunset modes)
○ Filter controls (Instagram-like effects)
○ Screenshot manager (auto-save to folder)
○ Replay recording (30-second clips)
```

---

## **15. URP-SPECIFIC BEST PRACTICES**

### **15.1. Shader Graph Optimization**

**❌ ПЛОХО:**
```
Shader Graph с 50+ nodes:
  - Сложные math operations
  - Nested branches (if statements)
  - Dynamic branching по vertex color
  
Result: 500+ shader variants, 10ms GPU time
```

**✅ ХОРОШО:**
```
Optimized Shader Graph:
  1. Minimize node count (combine operations)
  2. Use Static Branches (для quality settings)
  3. Avoid dynamic branching (use lerp вместо if)
  4. Bake complex calculations в textures
  5. Use Shader Graph Blackboard для shared properties
  
Result: <100 variants, 3ms GPU time
```

**Shader Variant Stripping:**
```csharp
// ShaderVariantStripping.cs (Editor script)
using UnityEditor.Rendering;

public class StripUnusedVariants : IPreprocessShaders {
    public void OnProcessShader(Shader shader, ...) {
        // Strip variants мы не используем
        if (shader.name.Contains("VoxelPBR")) {
            // Keep только нужные keywords
            if (!passHasKeyword("_NORMALMAP")) {
                // Strip pass без normal mapping
            }
        }
    }
}
```

### **15.2. Texture Compression**

```csharp
// TextureImportSettings.cs
void SetupTextureImport(string path) {
    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
    
    if (path.Contains("_albedo")) {
        importer.sRGBTexture = true;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.compressionQuality = 100;
        
        // Platform-specific
        var platformSettings = importer.GetPlatformTextureSettings("Standalone");
        platformSettings.format = TextureImporterFormat.DXT5; // BC7 для лучшего качества
        platformSettings.maxTextureSize = 512;
        importer.SetPlatformTextureSettings(platformSettings);
        
    } else if (path.Contains("_normal")) {
        importer.sRGBTexture = false; // LINEAR!
        importer.textureType = TextureImporterType.NormalMap;
        
        var platformSettings = importer.GetPlatformTextureSettings("Standalone");
        platformSettings.format = TextureImporterFormat.DXT5; // BC5 для нормалей
        importer.SetPlatformTextureSettings(platformSettings);
    }
    
    importer.SaveAndReimport();
}
```

### **15.3. Dynamic Batching & SRP Batcher**

```csharp
// URP поддерживает SRP Batcher для reducing draw calls
// Requirement: Все materials должны использовать одинаковый shader

public class ChunkMaterialManager : MonoBehaviour {
    public Material sharedVoxelMaterial; // Один материал для ВСЕХ чанков
    
    void SetupChunk(GameObject chunkGO) {
        MeshRenderer renderer = chunkGO.GetComponent<MeshRenderer>();
        
        // SHARED material (не instance!)
        renderer.sharedMaterial = sharedVoxelMaterial;
        
        // Enable GPU Instancing (если shader поддерживает)
        renderer.sharedMaterial.enableInstancing = true;
    }
}
```

**Shader Graph: Enable GPU Instancing**
```
В Shader Graph Master Node:
  → Graph Settings
  → Enable GPU Instancing: TRUE
  → Support VFX Graph: FALSE (не нужно)
```

### **15.4. Shadow Optimization**

```csharp
public class ShadowQualityManager : MonoBehaviour {
    public UniversalRenderPipelineAsset urpAsset;
    
    public void SetShadowQuality(int qualityLevel) {
        switch (qualityLevel) {
            case 0: // Low
                urpAsset.shadowDistance = 20f;
                urpAsset.mainLightShadowmapResolution = 1024;
                urpAsset.shadowCascadeCount = 1;
                break;
                
            case 1: // Medium
                urpAsset.shadowDistance = 40f;
                urpAsset.mainLightShadowmapResolution = 2048;
                urpAsset.shadowCascadeCount = 2;
                break;
                
            case 2: // High
                urpAsset.shadowDistance = 50f;
                urpAsset.mainLightShadowmapResolution = 2048;
                urpAsset.shadowCascadeCount = 2;
                break;
                
            case 3: // Ultra
                urpAsset.shadowDistance = 75f;
                urpAsset.mainLightShadowmapResolution = 4096;
                urpAsset.shadowCascadeCount = 4;
                break;
        }
    }
}
```

---

## **16. VISUAL COMPARISON**

### **Original (2009) vs Remake (2026)**

| Aspect | Original LWJGL | Remake URP |
|--------|---------------|------------|
| **Textures** | 16×16 px, flat | 512×512 px, PBR (5 maps) |
| **Lighting** | Vertex colors | Real-time PBR lighting |
| **Shadows** | None | Soft cascaded shadows (2048-4096) |
| **Normals** | Flat shading | Normal mapping + tangents |
| **Materials** | Unlit | PBR (Albedo, Normal, Roughness, AO, Metallic) |
| **Fog** | Linear (hardcoded) | Volumetric (shader-based) |
| **Particles** | None | Atmospheric dust, fog, footsteps |
| **Post-FX** | None | Bloom, Color Grading, SSAO, DOF, Vignette |
| **Sky** | Clear color | Procedural skybox / gradient |
| **Performance** | 60 FPS (Intel HD) | 60 FPS (RTX 3060) |
| **Memory** | 100 MB | 800 MB |
| **Visual Fidelity** | 2009 tech demo | 2026 indie game quality |

### **Screenshot Comparison (hypothetical)**

```
ORIGINAL:
  ██████████████  ← Blocky textures
  █ 16px █ flat █  ← No depth
  ██████████████  ← Vertex colors only
  
REMAKE:
  ▓▓▒▒░░▓▓▒▒░░  ← High-res textures
  ▓ PBR ▓ deep ▓  ← Normal mapping
  ▓▓▒▒░░▓▓▒▒░░  ← Real shadows + AO
  ╚═══════════╝
    Light rays ☀️  ← Volumetric fog
```

---

## **17. VALIDATION CLASSIFICATION**

| System | Classification | Notes |
|--------|---------------|-------|
| Voxel Grid | **(P)** Proven | Unchanged from original |
| Cave Generation | **(P)** Proven | Unchanged algorithm |
| Player Physics | **(P)** Proven | Unchanged mechanics |
| Chunk System | **(P)** Proven | Core logic unchanged |
| **PBR Materials** | **(E)** Empirical | URP standard practice, well-tested |
| **Normal Mapping** | **(P)** Proven | Industry standard since 2005 |
| **Real-time Shadows** | **(P)** Proven | URP built-in feature |
| **Volumetric Fog** | **(E)** Empirical | Custom shader, tested in other games |
| **SSAO** | **(P)** Proven | URP renderer feature |
| **Parallax Mapping** | **(E)** Empirical | Performance-dependent |
| **Particle Systems** | **(P)** Proven | Unity standard |
| **Post-Processing** | **(P)** Proven | URP Post-Processing v2 |

---

## **18. CRITICAL CODE EXAMPLES**

### **18.1. VoxelPBR Shader (Shader Graph → HLSL)**

```hlsl
// VoxelPBR.shader (simplified, реальный будет в Shader Graph)
Shader "Custom/VoxelPBR" {
    Properties {
        _AlbedoArray ("Albedo Array", 2DArray) = "" {}
        _NormalArray ("Normal Array", 2DArray) = "" {}
        _RoughnessArray ("Roughness Array", 2DArray) = "" {}
        _AOArray ("AO Array", 2DArray) = "" {}
        _Parallax ("Parallax Scale", Range(0, 0.1)) = 0.02
    }
    
    SubShader {
        Tags { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
        }
        
        Pass {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            TEXTURE2D_ARRAY(_AlbedoArray);
            SAMPLER(sampler_AlbedoArray);
            TEXTURE2D_ARRAY(_NormalArray);
            SAMPLER(sampler_NormalArray);
            
            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // Block metadata
            };
            
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float4 metadata : COLOR;
            };
            
            Varyings vert(Attributes input) {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                output.uv = input.uv;
                output.metadata = input.color; // Pass block type
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target {
                // Decode metadata
                int blockType = (int)(input.metadata.r * 255);
                int faceID = (int)(input.metadata.g * 6);
                
                // Sample textures from array
                float3 albedo = SAMPLE_TEXTURE2D_ARRAY(_AlbedoArray, sampler_AlbedoArray, 
                                                       input.uv, blockType).rgb;
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D_ARRAY(_NormalArray, 
                                                                       sampler_NormalArray, 
                                                                       input.uv, blockType));
                
                // Transform normal to world space
                float3x3 TBN = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));
                
                // Setup PBR input data
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalWS;
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0.0; // Stone не металл
                surfaceData.smoothness = 0.3; // Roughness = 0.7
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = 1.0; // TODO: Sample AO map
                surfaceData.alpha = 1.0;
                
                // Calculate lighting (PBR)
                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);
                
                return color;
            }
            ENDHLSL
        }
        
        // Shadow pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}
```

### **18.2. Dynamic Quality Scaling**

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class QualityScaler : MonoBehaviour {
    public UniversalRenderPipelineAsset[] qualityAssets; // 4 assets: Low, Medium, High, Ultra
    public Volume postProcessVolume;
    
    private int currentQuality = 2; // Default: High
    
    void Start() {
        ApplyQualitySettings(currentQuality);
    }
    
    public void SetQuality(int level) {
        currentQuality = Mathf.Clamp(level, 0, 3);
        ApplyQualitySettings(currentQuality);
    }
    
    void ApplyQualitySettings(int level) {
        // Switch URP asset
        QualitySettings.renderPipeline = qualityAssets[level];
        
        // Post-processing adjustments
        if (postProcessVolume.profile.TryGet<Bloom>(out var bloom)) {
            bloom.active = level >= 1; // Medium+
        }
        
        if (postProcessVolume.profile.TryGet<DepthOfField>(out var dof)) {
            dof.active = level >= 3; // Ultra only
        }
        
        if (postProcessVolume.profile.TryGet<MotionBlur>(out var motionBlur)) {
            motionBlur.active = level >= 2; // High+
        }
        
        // Particle density
        ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>();
        float densityMultiplier = level switch {
            0 => 0.1f,  // Low
            1 => 0.4f,  // Medium
            2 => 0.7f,  // High
            3 => 1.0f,  // Ultra
            _ => 1.0f
        };
        
        foreach (var ps in particles) {
            var emission = ps.emission;
            emission.rateOverTimeMultiplier = densityMultiplier;
        }
        
        // Texture resolution (requires material switching)
        int texResolution = level switch {
            0 => 128,
            1 => 256,
            2 => 512,
            3 => 512,
            _ => 512
        };
        
        // Shadow distance
        float shadowDistance = level switch {
            0 => 0f,    // No shadows
            1 => 30f,
            2 => 50f,
            3 => 75f,
            _ => 50f
        };
        QualitySettings.shadowDistance = shadowDistance;
        
        Debug.Log($"Quality set to: {GetQualityName(level)}");
    }
    
    string GetQualityName(int level) => level switch {
        0 => "Low",
        1 => "Medium",
        2 => "High",
        3 => "Ultra",
        _ => "Unknown"
    };
}
```

---

## **19. FINAL NOTES**

**Этот дизайн-документ для URP Remake:**

✅ **Сохраняет:**
- Всю игровую механику без изменений
- Все физические константы
- Процедурную генерацию
- Collision system
- Player controller logic
- Chunk architecture

✅ **Обновляет:**
- Rendering pipeline (Built-in → URP)
- Material system (Unlit → PBR)
- Textures (16px → 512px, 5 maps)
- Lighting (vertex colors → real-time)
- Shaders (CG → Shader Graph + HLSL)
- Post-processing (none → full stack)
- Particles (none → atmospheric systems)

✅ **Добавляет:**
- Визуальная современность
- Screenshot-worthy graphics
- Scalability (Low → Ultra)
- Photorealistic materials
- Cinematic atmosphere

**Performance Trade-off:**
```
Original: 60 FPS на Intel HD Graphics (2009)
Remake:   60 FPS на RTX 3060 (2024)

→ 10x hardware requirement
→ 100x visual fidelity
→ Fair trade для modern remake
```

**Рекомендации для разработчика:**

1. **Start with Mechanics First**  
   - Портируй весь gameplay код
   - Тестируй без графики (simple materials)
   - Только потом добавляй PBR

2. **Incremental Visual Upgrades**  
   - Week 1: Basic PBR (albedo + normal)
   - Week 2: Add shadows
   - Week 3: Add fog
   - Week 4: Add post-processing
   - Week 5: Add particles

3. **Profile Constantly**  
   - After каждого визуального апгрейда
   - Используй Unity Profiler + Frame Debugger
   - Target: 60 FPS на RTX 3060

4. **Create Scalability Early**  
   - 4 quality presets с самого начала
   - Test на Low preset regularly
   - Ensure gameplay identical на всех уровнях

5. **Texture Pipeline Automation**  
   - Script для auto-import настроек
   - Batch processing для PBR sets
   - Version control для .sbsar files

---

**MRL Doctrine:** URP Remake complete. Gameplay preserved. Visuals transformed. PBR pipeline defined. Scalability guaranteed. Performance profiled. This is the Zero-Stub Era meets Next-Gen Graphics.

---

**Signed,**  
AVK Software
Architected by Andrei Koldaev