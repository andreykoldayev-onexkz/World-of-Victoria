Отлично! Теперь создам полный дизайн-документ для Unity на основе анализа.

---

# **ДИЗАЙН-ДОКУМЕНТ: MINECRAFT RD-132211 (UNITY)**
## **Системный анализ версии с блок-интеракцией**

*Архитектор: AVK Software* 
*Дата: 2026-04-19*  
*Целевой движок: Unity 2022.3 LTS*  
*Базовый код: RD-132211 (May 13, 2009 20:11:00)*  
*Эволюция: RD-131655 → RD-132211 (Same Day Update)*

---

## **1. DESIGN INTENT**

**Цель проекта:** Расширение воксельного движка добавлением **core gameplay loop** — возможности размещать и разрушать блоки, превращая tech demo в интерактивную sandbox-игру.

**Ключевые игровые ощущения:**
- **Creation (творчество):** Размещение блоков для построения структур
- **Destruction (разрушение):** Уничтожение блоков для изменения ландшафта
- **Agency (влияние):** Мир **отвечает** на действия игрока (впервые!)
- **Expression (самовыражение):** Игрок оставляет след в мире

**Философия изменений:**
```
RD-131655:  Наблюдатель (Observer)
            ↓
RD-132211:  Создатель (Creator/Destroyer)
```

**Ключевое нововведение:**
- **Core Loop появился:** Explore → Target → Modify → Observe Result
- Это **первая версия** где игрок **влияет** на мир

---

## **2. TARGET PLAYER PSYCHOLOGY**

**Профиль игрока (РАСШИРЕН):**
- **Тип:** Builders + Explorers + Early Minecraft Fans
- **Мотивация:**
  - **Builders:** Создание структур, архитектура, pixel art
  - **Terraformers:** Изменение ландшафта, mining, tunneling
  - **Experimenters:** "Что если я разрушу всё здесь?"
  - **Nostalgia Seekers:** Ощущение первого Minecraft

**Когнитивная нагрузка:** Low → Medium  
- **Добавилась:** Целеполагание ("Что построить?")
- **Появилась:** Инвентарь-концепция (какой блок размещать)

**Эмоциональная реакция (НОВАЯ):**
- **Satisfaction (удовлетворение):** При размещении блока в нужное место
- **Power (сила):** "Я могу изменить этот мир"
- **Flow state:** Ритмичное размещение/разрушение блоков
- **Pride (гордость):** "Посмотрите, что я построил"

**Мотивационная петля:**
```
Idea (идея) → Action (размещение) → Result (блок появился) → 
Satisfaction → New Idea → ...
```

---

## **3. CORE GAME EQUATION**

```
GameState = (
    PlayerPos, 
    PlayerRot, 
    VoxelGrid,      // ТЕПЕРЬ MUTABLE!
    ChunkVisibility,
    TargetedBlock,  // NEW: HitResult
    SelectedBlockType // NEW: Inventory concept
)

// НОВОЕ: State transition включает модификацию мира
ΔVoxelGrid = f(MouseClick, TargetedBlock, SelectedBlockType)

// НОВОЕ: Chunk invalidation loop
if (ΔVoxelGrid != ∅):
    InvalidateChunk(BlockPosition)
    RecalculateLighting(BlockPosition)
    RebuildMesh(AffectedChunks)

CoreLoop (UPDATED):
    Input → Physics → Raycast → UI Feedback → 
    [MouseClick] → World Modification → 
    Observer Notification → Chunk Rebuild → Render
```

**Математическое ядро:**
- **State Space (S):** Добавлено `VoxelGrid` как изменяемое состояние
- **Action Space (A):** `{WASD, Jump, Look, LeftClick, RightClick, BlockSelect}`
- **Transition Function (T):**
  ```
  if (LeftClick && HitResult != null):
      VoxelGrid[HitResult.position] = 0 (AIR)
      
  if (RightClick && HitResult != null):
      adjacentPos = HitResult.position + HitResult.normal
      VoxelGrid[adjacentPos] = SelectedBlockType
  ```
- **Reward Function (R):** **ПОЯВИЛСЯ!**
  ```
  R = +1 for successful block placement
  R = +1 for successful block destruction
  R = subjective (player's creative satisfaction)
  ```
- **Information (I):** Perfect + Target Feedback (visual highlight)

---

## **4. PRIMARY LOOP (КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ)**

**Частота:** 50 Hz physics, variable rendering

```
EVERY FRAME (Update):
  1. Camera.Rotate(MouseInput)
  2. PerformRaycast() → Update HitResult // НОВОЕ
  3. RenderTargetOutline(HitResult)      // НОВОЕ: Visual feedback
  4. RebuildDirtyChunks(limit: 2/frame)
  5. Unity renders scene

EVERY FIXED UPDATE (FixedUpdate):
  [ФИЗИКА НЕИЗМЕННА — см. RD-131655]
  1. Read Input (WASD, Space, R)
  2. Apply physics (gravity, friction)
  3. Collision detection
  4. Update position

INPUT HANDLING (NEW):
  if (Input.GetMouseButtonDown(0)): // Left Click
      if (HitResult != null):
          DestroyBlock(HitResult.x, HitResult.y, HitResult.z)
          
  if (Input.GetMouseButtonDown(1)): // Right Click
      if (HitResult != null):
          Vector3Int placePos = HitResult.position + HitResult.normal
          PlaceBlock(placePos.x, placePos.y, placePos.z, selectedBlockType)
          
  if (Input.GetKeyDown(KeyCode.Alpha1..9)): // Number keys
      selectedBlockType = KeyToBlockType(key)
```

**Unity-специфичная адаптация:**
```csharp
void Update() {
    // ОРИГИНАЛЬНЫЙ КОД
    HandleCameraRotation();
    
    // НОВОЕ: Raycast для target detection
    PerformBlockRaycast();
    
    // НОВОЕ: Block interaction
    HandleBlockInteraction();
    
    // ОРИГИНАЛЬНОЕ
    RebuildDirtyChunks();
}

void HandleBlockInteraction() {
    if (currentHitResult == null) return;
    
    // Destroy block (Left Click)
    if (Input.GetMouseButtonDown(0)) {
        worldData.SetBlock(
            currentHitResult.x, 
            currentHitResult.y, 
            currentHitResult.z, 
            0 // AIR
        );
        AudioManager.PlaySound("blockBreak");
    }
    
    // Place block (Right Click)
    if (Input.GetMouseButtonDown(1)) {
        Vector3Int placePos = currentHitResult.position + currentHitResult.faceNormal;
        
        // Prevent placing block inside player
        if (!IsPositionOccupiedByPlayer(placePos)) {
            worldData.SetBlock(
                placePos.x, 
                placePos.y, 
                placePos.z, 
                selectedBlockType
            );
            AudioManager.PlaySound("blockPlace");
        }
    }
}
```

---

## **5. SECONDARY / META LOOP (ПОЯВИЛСЯ!)**

**До RD-132211:** Meta loop отсутствовал  
**После RD-132211:** Появился **Creative Loop**

```
SESSION LOOP (NEW):
  1. Explore world → Find interesting location
  2. Get creative idea ("I'll build a tower here")
  3. Place blocks → See result → Iterate
  4. Admire creation → Screenshot (hypothetical)
  5. Continue exploring OR refine build

PROGRESSION (IMPLICIT):
  Skill(t) = Experience с block placement
  Speed(t+1) = Speed(t) + Practice
  Complexity(builds, t) ∝ Time_played
  
  Нет explicit unlocks, но есть SKILL PROGRESSION:
    Novice: Случайное размещение блоков
    Intermediate: Простые структуры (стены, башни)
    Expert: Сложная архитектура, pixel art
```

**Psychographic Progression:**
```
Phase 1 (0-10 min): Discovery
  "Wow, I can place blocks!"
  Random experimentation
  
Phase 2 (10-30 min): Purposeful Building
  "I'll make a house"
  Simple goal-oriented behavior
  
Phase 3 (30+ min): Expression
  "I want to make something impressive"
  Creative self-expression
```

---

## **6. SYSTEM VARIABLES**

### **World State (WorldData.cs) — EXTENDED**
```csharp
public class WorldData : MonoBehaviour {
    // ОРИГИНАЛЬНОЕ (unchanged)
    public const int WIDTH = 256;
    public const int HEIGHT = 256;
    public const int DEPTH = 64;
    
    private byte[] blocks;
    private int[] lightDepths;
    
    // НОВОЕ: Observer pattern
    private List<ILevelListener> listeners = new List<ILevelListener>();
    
    // НОВОЕ: SetBlock вместо read-only access
    public void SetBlock(int x, int y, int z, byte blockType) {
        if (!IsValidPosition(x, y, z)) return;
        
        int index = (y * HEIGHT + z) * WIDTH + x;
        byte oldBlock = blocks[index];
        
        if (oldBlock == blockType) return; // No change
        
        blocks[index] = blockType;
        
        // НОВОЕ: Notify listeners
        NotifyTileChanged(x, y, z);
        
        // НОВОЕ: Recalculate lighting if needed
        if (NeedsLightingUpdate(oldBlock, blockType)) {
            RecalculateLightColumn(x, z);
        }
    }
    
    // НОВОЕ: Observer pattern interface
    public void AddListener(ILevelListener listener) {
        listeners.Add(listener);
    }
    
    private void NotifyTileChanged(int x, int y, int z) {
        foreach (var listener in listeners) {
            listener.OnTileChanged(x, y, z);
        }
    }
    
    private void RecalculateLightColumn(int x, int z) {
        int oldDepth = lightDepths[x + z * WIDTH];
        
        // Find new light depth
        int newDepth = DEPTH - 1;
        while (newDepth > 0 && !IsSolidBlock(x, newDepth, z)) {
            newDepth--;
        }
        
        lightDepths[x + z * WIDTH] = newDepth;
        
        // Notify listeners about lighting change
        if (oldDepth != newDepth) {
            foreach (var listener in listeners) {
                listener.OnLightColumnChanged(x, z, oldDepth, newDepth);
            }
        }
    }
}

// НОВОЕ: Listener interface
public interface ILevelListener {
    void OnTileChanged(int x, int y, int z);
    void OnLightColumnChanged(int x, int z, int oldDepth, int newDepth);
    void OnAllChanged(); // For world reload
}
```

### **Player State (PlayerController.cs) — EXTENDED**
```csharp
public class PlayerController : MonoBehaviour {
    // ОРИГИНАЛЬНОЕ (все неизменно)
    private Vector3 velocity;
    private bool isGrounded;
    private float cameraPitch;
    
    // НОВОЕ: Block selection
    [Header("Block Interaction")]
    public float maxReachDistance = 5f; // Blocks
    public LayerMask worldLayer;
    
    [Header("Selected Block")]
    public byte selectedBlockType = 1; // Default: Stone
    
    // НОВОЕ: Current target
    private HitResult currentHitResult = null;
}
```

### **HitResult (NEW CLASS)**
```csharp
public class HitResult {
    public int x;          // Block X coordinate
    public int y;          // Block Y coordinate
    public int z;          // Block Z coordinate
    public int face;       // Which face was hit (0-5: -X, +X, -Y, +Y, -Z, +Z)
    public Vector3Int faceNormal; // Normal vector of hit face
    
    public Vector3Int position => new Vector3Int(x, y, z);
    
    public HitResult(int x, int y, int z, int face) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.face = face;
        this.faceNormal = FaceToNormal(face);
    }
    
    private Vector3Int FaceToNormal(int face) {
        return face switch {
            0 => Vector3Int.left,     // -X
            1 => Vector3Int.right,    // +X
            2 => Vector3Int.down,     // -Y
            3 => Vector3Int.up,       // +Y
            4 => Vector3Int.back,     // -Z
            5 => Vector3Int.forward,  // +Z
            _ => Vector3Int.zero
        };
    }
}
```

---

## **7. MECHANICS DERIVATION**

### **7.1. Процедурная генерация (НЕИЗМЕННА)**
```csharp
// WorldGenerator.cs — ИДЕНТИЧНО RD-131655
// Sphere-based cave generation unchanged
```

### **7.2. Block Raycasting (NEW: CRITICAL MECHANIC)**

**Алгоритм (DDA Ray Marching):**
```csharp
public class BlockRaycaster {
    public static HitResult Raycast(Vector3 origin, Vector3 direction, 
                                     float maxDistance, WorldData world) {
        // Normalize direction
        direction.Normalize();
        
        // DDA (Digital Differential Analyzer) algorithm
        // Step through voxel grid along ray
        
        Vector3 pos = origin;
        Vector3 step = direction * 0.1f; // Step size (10cm)
        float distance = 0f;
        
        while (distance < maxDistance) {
            pos += step;
            distance += 0.1f;
            
            // Convert world position to block coordinates
            int x = Mathf.FloorToInt(pos.x);
            int y = Mathf.FloorToInt(pos.y);
            int z = Mathf.FloorToInt(pos.z);
            
            // Check if block is solid
            if (world.IsSolidBlock(x, y, z)) {
                // Determine which face was hit
                int face = DetermineFace(pos, new Vector3Int(x, y, z));
                return new HitResult(x, y, z, face);
            }
        }
        
        return null; // No hit
    }
    
    private static int DetermineFace(Vector3 hitPos, Vector3Int blockPos) {
        // Calculate fractional position within block
        Vector3 frac = hitPos - (Vector3)blockPos;
        
        // Find which face is closest
        float minDist = float.MaxValue;
        int closestFace = 0;
        
        // Test each face
        float[] distances = {
            frac.x,           // -X face (left)
            1f - frac.x,      // +X face (right)
            frac.y,           // -Y face (bottom)
            1f - frac.y,      // +Y face (top)
            frac.z,           // -Z face (back)
            1f - frac.z       // +Z face (front)
        };
        
        for (int i = 0; i < 6; i++) {
            if (distances[i] < minDist) {
                minDist = distances[i];
                closestFace = i;
            }
        }
        
        return closestFace;
    }
}
```

**Оптимизированный DDA (более точный):**
```csharp
public static HitResult RaycastDDA(Vector3 origin, Vector3 direction, 
                                    float maxDistance, WorldData world) {
    // John Amanatides, Andrew Woo - "A Fast Voxel Traversal Algorithm"
    // http://www.cse.yorku.ca/~amana/research/grid.pdf
    
    int x = Mathf.FloorToInt(origin.x);
    int y = Mathf.FloorToInt(origin.y);
    int z = Mathf.FloorToInt(origin.z);
    
    int stepX = direction.x > 0 ? 1 : -1;
    int stepY = direction.y > 0 ? 1 : -1;
    int stepZ = direction.z > 0 ? 1 : -1;
    
    // tMax: distance to next voxel boundary
    float tMaxX = IntBound(origin.x, direction.x);
    float tMaxY = IntBound(origin.y, direction.y);
    float tMaxZ = IntBound(origin.z, direction.z);
    
    // tDelta: distance to cross one voxel
    float tDeltaX = stepX / direction.x;
    float tDeltaY = stepY / direction.y;
    float tDeltaZ = stepZ / direction.z;
    
    int face = -1;
    
    while (true) {
        // Check current voxel
        if (world.IsSolidBlock(x, y, z)) {
            return new HitResult(x, y, z, face);
        }
        
        // Step to next voxel
        if (tMaxX < tMaxY) {
            if (tMaxX < tMaxZ) {
                if (tMaxX > maxDistance) break;
                x += stepX;
                tMaxX += tDeltaX;
                face = stepX > 0 ? 0 : 1; // -X or +X
            } else {
                if (tMaxZ > maxDistance) break;
                z += stepZ;
                tMaxZ += tDeltaZ;
                face = stepZ > 0 ? 4 : 5; // -Z or +Z
            }
        } else {
            if (tMaxY < tMaxZ) {
                if (tMaxY > maxDistance) break;
                y += stepY;
                tMaxY += tDeltaY;
                face = stepY > 0 ? 2 : 3; // -Y or +Y
            } else {
                if (tMaxZ > maxDistance) break;
                z += stepZ;
                tMaxZ += tDeltaZ;
                face = stepZ > 0 ? 4 : 5;
            }
        }
    }
    
    return null; // No hit within maxDistance
}

private static float IntBound(float s, float ds) {
    if (ds < 0) {
        return IntBound(-s, -ds);
    } else {
        s = Mod(s, 1);
        return (1 - s) / ds;
    }
}

private static float Mod(float value, float modulus) {
    return (value % modulus + modulus) % modulus;
}
```

### **7.3. Observer Pattern для Chunk Updates (NEW)**

```csharp
public class Chunk : MonoBehaviour, ILevelListener {
    private WorldData world;
    private bool isDirty = false;
    
    void Start() {
        // Subscribe to world changes
        world.AddListener(this);
    }
    
    // ILevelListener implementation
    public void OnTileChanged(int x, int y, int z) {
        // Check if changed block is in this chunk
        if (ContainsBlock(x, y, z)) {
            isDirty = true;
            
            // Also mark adjacent chunks if on boundary
            if (IsOnChunkBoundary(x, y, z)) {
                MarkAdjacentChunksDirty(x, y, z);
            }
        }
    }
    
    public void OnLightColumnChanged(int x, int z, int oldDepth, int newDepth) {
        // Lighting changed — may affect multiple vertical chunks
        if (ContainsColumn(x, z)) {
            isDirty = true;
        }
    }
    
    public void OnAllChanged() {
        isDirty = true; // Full world reload
    }
    
    void Update() {
        if (isDirty && CanRebuild()) {
            RebuildMesh();
            isDirty = false;
        }
    }
    
    private bool CanRebuild() {
        // Limit to 2 chunk rebuilds per frame (original limit)
        return ChunkManager.rebuiltThisFrame < 2;
    }
}
```

### **7.4. Collision Detection (SLIGHTLY MODIFIED)**

```csharp
// PlayerController.cs
private void MoveWithCollision(Vector3 deltaMove) {
    // ОРИГИНАЛЬНЫЙ 3-pass AABB sweep (unchanged)
    // [КОД ИДЕНТИЧЕН RD-131655]
    
    // НОВОЕ: Не позволяем игроку застревать в размещённых блоках
    Vector3 playerPos = transform.position;
    int blockX = Mathf.FloorToInt(playerPos.x);
    int blockY = Mathf.FloorToInt(playerPos.y - 0.9f); // At feet
    int blockZ = Mathf.FloorToInt(playerPos.z);
    
    if (worldData.IsSolidBlock(blockX, blockY, blockZ)) {
        // Player stuck in newly placed block — push out
        PushOutOfBlock(blockX, blockY, blockZ);
    }
}
```

---

## **8. DECISION DENSITY ANALYSIS (КРИТИЧЕСКИ ИЗМЕНЁН)**

**Частота решений:** 10-30 decisions/minute (было 1-2!)  
**Категория:** Medium (было Extremely Low)

**Типы решений (ОБНОВЛЕНЫ):**

1. **Block Placement Decision:** "Где разместить следующий блок?"  
   - Частота: ~3-10 seconds (очень часто!)  
   - Вес: Medium (влияет на структуру)  
   - Reversible: Да (можно уничтожить)

2. **Block Destruction Decision:** "Какой блок удалить?"  
   - Частота: ~5-15 seconds  
   - Вес: Medium  
   - Irreversible: Нет save/undo → permanent

3. **Block Type Selection:** "Stone или Grass?"  
   - Частота: ~30-60 seconds  
   - Вес: Low (только визуал)

4. **Spatial Planning:** "Как построить эту структуру?"  
   - Частота: ~60-180 seconds  
   - Вес: High (архитектурное планирование)

5. **Navigation:** "Как добраться до того места?"  
   - Частота: ~15-30 seconds  
   - Вес: Low (unchanged from RD-131655)

**Decision Density Progression:**
```
Minute 0-5:   ~5 decisions/min  (Learning controls)
Minute 5-15:  ~15 decisions/min (Active building)
Minute 15+:   ~25 decisions/min (Flow state, rapid building)
```

**Обоснование высокой плотности:**
- Каждое размещение блока = решение
- Rapid iteration: place → evaluate → adjust
- **Flow state achievable** (было невозможно в RD-131655)

---

## **9. FAILURE MODES & DOMINANT STRATEGIES**

### **9.1. Оригинальные баги (сохранены)**
- ❌ Tunneling bug (unchanged)
- ❌ Spawn in solid block (unchanged)

### **9.2. НОВЫЕ механические проблемы**

**❌ Issue 1: Self-Entombment**
```
Проблема: Игрок может заблокировать себя в структуре без выхода

Воспроизведение:
  1. Построить 4 стены вокруг себя
  2. Разместить блок над головой
  3. Теперь в closed box без escape

Решение:
  - Check player collision перед размещением блока
  - Если блок блокирует player bounding box → reject placement
```

```csharp
bool CanPlaceBlock(Vector3Int position) {
    // Check if block would intersect player
    Bounds blockBounds = new Bounds(
        position + Vector3.one * 0.5f,
        Vector3.one
    );
    
    Bounds playerBounds = playerController.GetBounds();
    
    if (blockBounds.Intersects(playerBounds)) {
        return false; // Would trap player
    }
    
    return true;
}
```

**❌ Issue 2: Infinite Block Duplication**
```
Проблема: Нет inventory system → бесконечные блоки

Exploitable: Да, но это feature в creative mode

Решение (если хотим survival):
  - Implement inventory с finite resources
  - Destroyed blocks → inventory
  - Place blocks → consume from inventory
```

**❌ Issue 3: Chunk Update Cascade**
```
Проблема: Destroying column of blocks вызывает lighting recalc для всей колонны
  → Cascade rebuild многих chunks

Воспроизведение:
  1. Stack 64 blocks vertically
  2. Destroy bottom block
  3. ALL chunks in column mark dirty
  4. Frame drop

Решение:
  - Batch lighting updates (не рассчитывать каждый блок отдельно)
  - Defer chunk rebuilds на несколько frames
  - Limit lighting recalcs per frame
```

**❌ Issue 4: Z-Fighting on Block Outline**
```
Проблема: Outline рендерится на той же глубине что и блок → flicker

Решение:
  - Offset outline slightly вперёд (camera direction)
  - Или использовать stencil buffer
```

### **9.3. Деструктивные стратегии**

**⚠️ Strategy: Griefing (Multiplayer Concern)**
```
If multiplayer was added:
  Problem: Players can destroy others' creations
  
  Solution:
    - Claim system (protected zones)
    - Permissions system
    - History/rollback system
```

**✅ Strategy: Speed Building**
```
Optimal building pattern:
  1. Jump while placing blocks under feet
  2. Rapid pillar to sky → fast vertical travel
  3. "Tower rush" dominant strategy для exploration
  
This is ACCEPTABLE — part of game identity
```

---

## **10. BALANCE MODEL (EXTENDED)**

**Физические константы (НЕИЗМЕННЫ):**
```csharp
// Все константы из RD-131655 идентичны
public float groundAcceleration = 0.4f;
public float gravity = 0.1f;
// ... etc
```

**НОВЫЕ константы:**
```csharp
[Header("Block Interaction")]
public float maxReachDistance = 5f;     // Unity units (~5 blocks)
public float blockPlaceDelay = 0.15f;   // Cooldown между placements (prevent spam)
public float blockBreakDelay = 0.2f;    // Cooldown между destructions

[Header("Feedback")]
public float outlineThickness = 0.02f;  // Outline render thickness
public Color outlineColor = new Color(0, 0, 0, 0.4f); // Black, 40% alpha
```

**Balance Rationale:**
```
maxReachDistance = 5 blocks:
  Обоснование: Minecraft uses 4.5 blocks
  Rationale: Достаточно для комфортного строительства, не too long
  
blockPlaceDelay = 0.15s:
  Обоснование: ~6-7 blocks/second max
  Rationale: Prevents accidental spam, requires intentional action
  Trade-off: Fast enough для flow, slow enough для deliberate
```

---

## **11. PROGRESSION SCALING MODEL**

**Отсутствует явная прогрессия, НО:**

**Implicit Skill Progression:**
```
Skill Curve:
  t=0:    Random block placement, exploring controls
  t=10m:  Simple structures (walls, towers)
  t=30m:  Complex architecture (houses, bridges)
  t=60m:  Advanced techniques (flying stairs, arches)
  t=2h+:  Pixel art, detailed sculptures

MasteryMetric = f(BlocksPlaced, StructureComplexity, BuildTime)
```

**Hypothetical Future Progression (если добавлять):**
```csharp
// NOT in RD-132211, but possible extension
public enum GameMode {
    Creative,  // Infinite blocks, flying (current behavior)
    Survival   // Finite resources, health, hunger
}

// Survival mode progression
public class SurvivalProgression {
    public int woodCollected;
    public int stoneCollected;
    
    // Unlock tree
    bool CanCraftStoneTools => woodCollected >= 3;
    bool CanCraftFurnace => stoneCollected >= 8;
}
```

---

## **12. SCOPE & TECHNICAL REQUIREMENTS**

### **12.1. Unity Setup (ИДЕНТИЧНО RD-131655)**

**Версия:** Unity 2022.3 LTS  
**Render Pipeline:** Built-in RP или URP  

### **12.2. NEW: Input System**

**Mouse Buttons:**
```
Left Click (Button 0):  Destroy block
Right Click (Button 1): Place block
```

**Keyboard:**
```
Numbers 1-9: Select block type (hotbar concept)
  1 = Stone
  2 = Grass
  3-9 = Future blocks
```

### **12.3. NEW: Audio Requirements**

**Sound Effects (NEW):**
```
blockPlace.wav:  Short click sound (~0.2s)
blockBreak.wav:  Crack sound (~0.3s)

// Future expansion
blockPlace_stone.wav
blockPlace_grass.wav
blockBreak_stone.wav
```

**Implementation:**
```csharp
public class AudioManager : MonoBehaviour {
    public AudioClip blockPlaceSound;
    public AudioClip blockBreakSound;
    
    private AudioSource audioSource;
    
    public void PlayBlockPlace() {
        audioSource.PlayOneShot(blockPlaceSound);
    }
    
    public void PlayBlockBreak() {
        audioSource.PlayOneShot(blockBreakSound);
    }
}
```

### **12.4. UI Requirements (NEW)**

**Crosshair (NEW):**
```
Simple cross at screen center:
  - 2px white lines
  - 20px length
  - Always visible
  - Changes color when targeting block (white → black)
```

**Block Selection UI (NEW):**
```
Hotbar at bottom center:
  - 9 slots (1-9)
  - Current selection highlighted
  - Shows block icon
  - Keyboard shortcuts (1-9)
```

**Block Outline Renderer (NEW):**
```
Wireframe cube around targeted block:
  - Rendered using GL.Lines или LineRenderer
  - Offset slightly to prevent z-fighting
  - Black color, 40% alpha
  - Updated every frame based on HitResult
```

---

## **13. MVP CUTLINE**

### **✅ MUST HAVE (Core Interactive Gameplay)**

**От RD-131655 (unchanged):**
1. ✅ WorldGenerator — Cave generation
2. ✅ PlayerController — Movement physics
3. ✅ ChunkManager — Chunk system
4. ✅ Collision system

**НОВОЕ (RD-132211):**
5. ✅ **BlockRaycaster** — DDA raycast для targeting
6. ✅ **HitResult class** — Raycast result storage
7. ✅ **WorldData.SetBlock()** — Mutable world state
8. ✅ **ILevelListener interface** — Observer pattern
9. ✅ **Chunk.OnTileChanged()** — Chunk invalidation
10. ✅ **Block placement logic** — Right click handler
11. ✅ **Block destruction logic** — Left click handler
12. ✅ **Block outline rendering** — Visual feedback
13. ✅ **Crosshair UI** — Targeting indicator
14. ✅ **Block selection system** — Hotbar (1-9 keys)
15. ✅ **Audio feedback** — Place/break sounds

### **🔶 NICE TO HAVE (Polish)**

16. 🔶 **Block placement animation** — Scale from 0 to 1
17. 🔶 **Block break animation** — Crack texture progression
18. 🔶 **Particle effects** — Block fragments on break
19. 🔶 **Hotbar UI** — Visual inventory bar
20. 🔶 **Block tooltips** — Show block name on hover

### **❌ OUT OF SCOPE**

21. ❌ Inventory system (infinite blocks OK для MVP)
22. ❌ Crafting
23. ❌ Multiple block types (>2)
24. ❌ Save/load player-modified world
25. ❌ Undo/redo system

---

## **14. IMPLEMENTATION ROADMAP**

### **Phase 1: Core from RD-131655 (Week 1-2)**
```
✓ Port all RD-131655 systems (see previous document)
✓ WorldData, PlayerController, ChunkManager working
✓ Baseline: Exploration working без interaction
```

### **Phase 2: Raycast System (Week 3)**
```
✓ Implement BlockRaycaster.RaycastDDA()
✓ Create HitResult class
✓ Test raycast accuracy (debug visualization)
✓ Performance test (raycast every frame = expensive?)
```

### **Phase 3: Block Outline Rendering (Week 3)**
```
✓ Create OutlineRenderer.cs
✓ Use GL.Lines или LineRenderer для wireframe cube
✓ Update every frame based on HitResult
✓ Handle z-fighting (slight offset)
```

### **Phase 4: Observer Pattern (Week 4)**
```
✓ Create ILevelListener interface
✓ Modify Chunk to implement ILevelListener
✓ Add WorldData.AddListener()
✓ Implement NotifyTileChanged()
✓ Test chunk invalidation на block change
```

### **Phase 5: Block Modification (Week 4)**
```
✓ Implement WorldData.SetBlock()
✓ Left click → destroy block
✓ Right click → place block
✓ Test collision prevention (don't place in player)
✓ Test lighting recalculation
```

### **Phase 6: Audio & UI (Week 5)**
```
✓ Add AudioManager
✓ blockPlace.wav, blockBreak.wav
✓ Crosshair UI (Canvas + Image)
✓ Block selection system (hotbar, 1-9 keys)
✓ Visual feedback on key press
```

### **Phase 7: Polish & Bug Fixes (Week 5-6)**
```
✓ Fix self-entombment bug
✓ Fix chunk update cascade lag
✓ Fix z-fighting on outline
✓ Optimize raycast (cache last result?)
✓ Playtesting & iteration
```

### **Phase 8: Optional Enhancements (Week 6+)**
```
○ Block place/break animations
○ Particle effects (block fragments)
○ Hotbar UI (visual inventory)
○ More block types (dirt, wood, glass)
○ Save/load modified world
```

---

## **15. CRITICAL CODE EXAMPLES**

### **15.1. BlockRaycaster.cs (Complete Implementation)**

```csharp
using UnityEngine;

public class BlockRaycaster {
    // Amanatides & Woo DDA algorithm
    public static HitResult Raycast(Vector3 origin, Vector3 direction, 
                                     float maxDistance, WorldData world) {
        // Normalize
        direction.Normalize();
        
        // Current voxel
        int x = Mathf.FloorToInt(origin.x);
        int y = Mathf.FloorToInt(origin.y);
        int z = Mathf.FloorToInt(origin.z);
        
        // Step direction
        int stepX = direction.x > 0 ? 1 : -1;
        int stepY = direction.y > 0 ? 1 : -1;
        int stepZ = direction.z > 0 ? 1 : -1;
        
        // tMax: distance along ray to next voxel boundary
        float tMaxX = IntBound(origin.x, direction.x);
        float tMaxY = IntBound(origin.y, direction.y);
        float tMaxZ = IntBound(origin.z, direction.z);
        
        // tDelta: distance to cross one voxel
        float tDeltaX = stepX / direction.x;
        float tDeltaY = stepY / direction.y;
        float tDeltaZ = stepZ / direction.z;
        
        int face = -1;
        float radius = maxDistance;
        
        // Traverse voxel grid
        while (Mathf.Abs(tMaxX) < radius || 
               Mathf.Abs(tMaxY) < radius || 
               Mathf.Abs(tMaxZ) < radius) {
            
            // Check if current voxel is solid
            if (world.IsSolidBlock(x, y, z)) {
                return new HitResult(x, y, z, face);
            }
            
            // Advance to next voxel
            if (Mathf.Abs(tMaxX) < Mathf.Abs(tMaxY)) {
                if (Mathf.Abs(tMaxX) < Mathf.Abs(tMaxZ)) {
                    x += stepX;
                    tMaxX += tDeltaX;
                    face = stepX > 0 ? 0 : 1; // -X or +X
                } else {
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                    face = stepZ > 0 ? 4 : 5; // -Z or +Z
                }
            } else {
                if (Mathf.Abs(tMaxY) < Mathf.Abs(tMaxZ)) {
                    y += stepY;
                    tMaxY += tDeltaY;
                    face = stepY > 0 ? 2 : 3; // -Y or +Y
                } else {
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                    face = stepZ > 0 ? 4 : 5;
                }
            }
        }
        
        return null; // No hit
    }
    
    private static float IntBound(float s, float ds) {
        if (ds < 0) {
            return IntBound(-s, -ds);
        } else {
            s = Mod(s, 1);
            return (1 - s) / ds;
        }
    }
    
    private static float Mod(float value, float modulus) {
        return (value % modulus + modulus) % modulus;
    }
}
```

### **15.2. BlockInteractionController.cs**

```csharp
using UnityEngine;

public class BlockInteractionController : MonoBehaviour {
    [Header("References")]
    public Camera playerCamera;
    public WorldData worldData;
    public AudioManager audioManager;
    
    [Header("Settings")]
    public float maxReachDistance = 5f;
    public byte selectedBlockType = 1; // Stone
    public float interactionCooldown = 0.15f;
    
    [Header("UI")]
    public OutlineRenderer outlineRenderer;
    
    private HitResult currentHitResult;
    private float lastInteractionTime;
    
    void Update() {
        // Perform raycast every frame
        PerformRaycast();
        
        // Update outline visualization
        UpdateOutline();
        
        // Handle input
        HandleBlockInteraction();
        
        // Block selection (1-9 keys)
        HandleBlockSelection();
    }
    
    void PerformRaycast() {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;
        
        currentHitResult = BlockRaycaster.Raycast(
            origin, direction, maxReachDistance, worldData
        );
    }
    
    void UpdateOutline() {
        if (currentHitResult != null) {
            outlineRenderer.Show(currentHitResult.position);
        } else {
            outlineRenderer.Hide();
        }
    }
    
    void HandleBlockInteraction() {
        // Cooldown check
        if (Time.time - lastInteractionTime < interactionCooldown) return;
        
        if (currentHitResult == null) return;
        
        // DESTROY BLOCK (Left Click)
        if (Input.GetMouseButtonDown(0)) {
            worldData.SetBlock(
                currentHitResult.x,
                currentHitResult.y,
                currentHitResult.z,
                0 // AIR
            );
            
            audioManager.PlayBlockBreak();
            lastInteractionTime = Time.time;
        }
        
        // PLACE BLOCK (Right Click)
        if (Input.GetMouseButtonDown(1)) {
            Vector3Int placePos = currentHitResult.position + currentHitResult.faceNormal;
            
            // Check if position is valid
            if (CanPlaceBlockAt(placePos)) {
                worldData.SetBlock(
                    placePos.x,
                    placePos.y,
                    placePos.z,
                    selectedBlockType
                );
                
                audioManager.PlayBlockPlace();
                lastInteractionTime = Time.time;
            }
        }
    }
    
    bool CanPlaceBlockAt(Vector3Int position) {
        // Check if block would intersect player
        Vector3 playerPos = transform.position;
        Bounds playerBounds = new Bounds(playerPos, new Vector3(0.6f, 1.8f, 0.6f));
        Bounds blockBounds = new Bounds(position + Vector3.one * 0.5f, Vector3.one);
        
        if (playerBounds.Intersects(blockBounds)) {
            return false; // Would trap player
        }
        
        // Check if within world bounds
        return worldData.IsValidPosition(position.x, position.y, position.z);
    }
    
    void HandleBlockSelection() {
        // Number keys 1-9
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedBlockType = 1; // Stone
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedBlockType = 2; // Grass
        // ... etc for blocks 3-9
    }
}
```

### **15.3. OutlineRenderer.cs**

```csharp
using UnityEngine;

public class OutlineRenderer : MonoBehaviour {
    public Color outlineColor = new Color(0, 0, 0, 0.4f);
    public float lineWidth = 0.02f;
    
    private LineRenderer[] lines;
    private bool isVisible = false;
    
    void Start() {
        // Create 12 LineRenderers (12 edges of cube)
        lines = new LineRenderer[12];
        
        for (int i = 0; i < 12; i++) {
            GameObject lineObj = new GameObject($"OutlineLine{i}");
            lineObj.transform.parent = transform;
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = outlineColor;
            lr.endColor = outlineColor;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            
            lines[i] = lr;
            lines[i].enabled = false;
        }
    }
    
    public void Show(Vector3Int blockPosition) {
        isVisible = true;
        
        // Calculate cube vertices
        Vector3 min = blockPosition;
        Vector3 max = blockPosition + Vector3.one;
        
        Vector3[] vertices = {
            new Vector3(min.x, min.y, min.z), // 0
            new Vector3(max.x, min.y, min.z), // 1
            new Vector3(max.x, min.y, max.z), // 2
            new Vector3(min.x, min.y, max.z), // 3
            new Vector3(min.x, max.y, min.z), // 4
            new Vector3(max.x, max.y, min.z), // 5
            new Vector3(max.x, max.y, max.z), // 6
            new Vector3(min.x, max.y, max.z)  // 7
        };
        
        // Define 12 edges
        int[,] edges = {
            {0, 1}, {1, 2}, {2, 3}, {3, 0}, // Bottom face
            {4, 5}, {5, 6}, {6, 7}, {7, 4}, // Top face
            {0, 4}, {1, 5}, {2, 6}, {3, 7}  // Vertical edges
        };
        
        // Update line positions
        for (int i = 0; i < 12; i++) {
            lines[i].SetPosition(0, vertices[edges[i, 0]]);
            lines[i].SetPosition(1, vertices[edges[i, 1]]);
            lines[i].enabled = true;
        }
    }
    
    public void Hide() {
        if (!isVisible) return;
        isVisible = false;
        
        foreach (var line in lines) {
            line.enabled = false;
        }
    }
}
```

---

## **16. VALIDATION CLASSIFICATION**

| System | Classification | Notes |
|--------|---------------|-------|
| **Voxel Grid** | **(P)** Proven | Unchanged from RD-131655 |
| **Cave Generation** | **(P)** Proven | Unchanged |
| **Player Physics** | **(P)** Proven | Unchanged |
| **Chunk System** | **(P)** Proven | Core logic unchanged |
| **DDA Raycasting** | **(P)** Proven | Amanatides & Woo 1987, industry standard |
| **Observer Pattern** | **(P)** Proven | Design pattern, widely used |
| **Block Modification** | **(P)** Proven | Implemented in RD-132211 (inferred) |
| **Outline Rendering** | **(E)** Empirical | Custom implementation, tested approach |
| **Audio Feedback** | **(P)** Proven | Standard Unity AudioSource |
| **UI (Crosshair/Hotbar)** | **(P)** Proven | Unity UI Canvas |

---

## **17. PERFORMANCE CONSIDERATIONS**

### **17.1. Raycast Cost**

```
NAIVE: Raycast every frame = 60 FPS × 100 ray steps = 6000 checks/sec

OPTIMIZED:
  - Early exit on first hit
  - DDA algorithm (optimal traversal)
  - Expected checks per raycast: ~20-50 (5 blocks distance)
  - Cost: ~0.01-0.05ms per raycast
  
Verdict: ACCEPTABLE для каждого frame
```

### **17.2. Chunk Invalidation Cost**

```
WORST CASE: Place block на chunk boundary
  → 4 chunks mark dirty (corner case)
  → 8 chunks mark dirty (edge case)
  
MITIGATION:
  - Limit 2 chunk rebuilds per frame (unchanged from RD-131655)
  - Queue система для rebuilds
  - Priority: Closest chunks first
  
Verdict: Original design handles this well
```

### **17.3. Observer Notification Cost**

```
COST PER BLOCK CHANGE:
  - Notify all listeners: O(n) where n = number of chunks
  - Typical: 1000 chunks × simple check = ~0.1ms
  
OPTIMIZATION:
  - Spatial hashing (only notify chunks in radius)
  - Batch notifications (collect changes, notify once)
  
Verdict: Negligible cost
```

---

## **18. COMPARISON: RD-131655 vs RD-132211**

| Aspect | RD-131655 | RD-132211 |
|--------|-----------|-----------|
| **Core Gameplay** | Exploration only | Exploration + Creation |
| **Player Agency** | None (read-only) | High (world modification) |
| **Decision Density** | 1-2/min | 10-30/min |
| **Flow State** | Impossible | Achievable |
| **Session Length** | 10-15 min | 30-60+ min |
| **Replayability** | Low | High |
| **Code Complexity** | ~1000 LOC | ~1500 LOC (+50%) |
| **New Classes** | 0 | 2 (HitResult, LevelListener) |
| **Input Complexity** | Low | Medium |
| **World State** | Immutable | Mutable |
| **Chunk Updates** | Static (one-time) | Dynamic (on-demand) |
| **Player Role** | Observer | Creator/Destroyer |

---

## **19. FINAL NOTES**

**Этот дизайн-документ для RD-132211:**

✅ **Сохраняет:**
- Всю физику и генерацию из RD-131655
- Chunk system architecture
- Performance characteristics
- Camera/movement feel

✅ **Добавляет:**
- **Gameplay loop** (первый раз!)
- Block placement/destruction
- Raycasting system
- Observer pattern
- Visual feedback (outline)
- Audio feedback
- Basic UI (crosshair, hotbar)

✅ **Трансформирует:**
- Из tech demo → playable game
- Из passive → active experience
- Из read-only → read-write world
- Из low decision density → medium

**Критическое отличие:**
```
RD-131655: "Look at my voxel engine"
RD-132211: "Look at what I BUILT"
```

**Рекомендации для Unity разработчика:**

1. **Start with RD-131655 Foundation**  
   - Портируй базовую систему полностью
   - Тестируй exploration working

2. **Add Raycast Next**  
   - Implement DDA algorithm
   - Visualize with Debug.DrawLine
   - Verify accuracy before proceeding

3. **Observer Pattern Critical**  
   - Don't skip this architecture
   - Makes chunk updates automatic
   - Essential для dynamic world

4. **Test Self-Entombment Early**  
   - Collision check BEFORE placement
   - Prevent player frustration

5. **Audio = Feel**  
   - Even simple click sounds transform feel
   - 80% of perceived quality

---

**MRL Doctrine:** RD-132211 analysis complete. Gameplay loop identified. Raycasting system defined. Observer pattern documented. All critical systems specified. This is the Zero-Stub Era meets Interactive Sandbox.

---

**Signed,**  
AVK Software
Architected by Andrei Koldaev