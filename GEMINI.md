# NTGH (Người Trong Giang Hồ) - Project Rules & Guidelines

> Project rules and architecture guide for Antigravity AI agents working in this repository.

---

## 1. Project Overview & Tech Stack

- **Game Concept**: NTGH ("Người Trong Giang Hồ") — A gangster/triad-themed RPG Match-3 game with story progression, weapon inventory, and tactical combat.
- **Engine**: Unity **6000.6.1f1** (Unity 6 series).
- **Core Language**: C# (targeting .NET Standard / Unity runtime).
- **UI System**:
  - Unity uGUI with **TextMeshPro** (`TextMeshProUGUI`).
  - Screen / Pop-up management: `SS.UI` framework (`ScreenController`, `ScreenManager`) and `PopupBase`.
- **Tweening & VFX**:
  - Primary: **DOTween** (`DG.Tweening` / DOTweenPro).
  - Legacy: `GoKit` (found in `Assets/MathThreeGame/Plugins/GoKit/`).
  - VFX: Shuriken Particle Systems (`m_effAttack`, `m_effHealth`, `m_effHit`, etc.), Epic Toon FX, Cartoon FX.
- **Audio**: Custom `SoundManager` singleton managing BGM and SFX channels with DOTween fade support.
- **Monetization & Plugins**:
  - `GoogleMobileAds` (AdMob integration).
  - `com.coplaydev.unity-mcp` (Unity MCP integration for editor AI interaction).

---

## 2. Architecture & Design Patterns

### 2.1 Singletons & Lifecycle
- **Persistent Singletons**:
  - `GameManager.instance`: Persistent (`DontDestroyOnLoad`), coordinates scene transitions, global story index (`story_num`), level string, weapon retrieval, and screen fading.
  - `SoundManager.instance`: Persistent (`DontDestroyOnLoad`), manages audio clips, BGM, and SFX channels.
- **Scene-Scoped Singletons**:
  - `GameController.instance`: Controls the match-3 combat grid and turn mechanics in `Game.unity`.
  - `HomeController.instance`: Manages the main menu screen, currency display, and navigation.
  - `StoryController.instance`: Drives visual novel dialogue sequences.
  - `ShopWeapon.instance`, `Popup.instance`: Manage popups.
- **Rule**: When creating new singletons, always follow the duplicate cleanup pattern in `Awake()`:
  ```csharp
  if (instance == null) {
      instance = this;
      // DontDestroyOnLoad(gameObject); // Only if intended to persist across scenes
  } else {
      Destroy(gameObject);
  }
  ```

### 2.2 Scene Navigation & UI Stack
- Scenes are loaded using `GameManager`:
  - `GameManager.instance.LoadScene(string sceneName, object data = null)`: Single scene load with DOTween black-fade transition.
  - `GameManager.instance.AddScene(string sceneName, object data = null)`: Additive scene load for popups (`Popup`, `ShopWeapon`).
  - `GameManager.CloseScene(string sceneName)`: Unloads additive scenes via `SceneManager.UnloadSceneAsync`.
- Key Scenes (`Assets/Project/Scenes/`):
  - `Intro.unity`: Splash / intro cinematic transition.
  - `Main.unity`: Title screen / tap-to-start.
  - `Home.unity`: Lobby, weapon shop access, settings, story launcher.
  - `Story.unity`: Visual novel dialogue before battles.
  - `Game.unity`: Match-3 battle arena.
  - `ShopWeapon.unity`: Weapon purchasing and selection.
  - `Popup.unity`: Victory/defeat results and rewards.

### 2.3 Data Architecture & Persistence
1. **ScriptableObjects** for static configurations (`Assets/Project/Scenes/Scripts/`):
   - `CreateAllDataStory`: Master list containing all story chapters/stages.
   - `CreateData`: Single stage data containing Player characters, Enemy characters, `ContentStory` dialogue, and stage `itemReward`.
   - `CreateDataCharacter`: Character stats (`Name`, `avatar`, `attack`, `health`, `defence`, `isPlayer`).
   - `CreateItemReward`: Weapon/Reward items (`ID`, `Price`, `Sprite`, etc.).
2. **PlayerPrefs** for runtime player state and currencies (`Const.cs` keys):
   - `Const.EXP` ("Exp"): Experience points.
   - `Const.GOLD` ("Gold"): In-game currency.
   - `Const.Stage` ("Stage"): Current progression stage index.
   - `Const.LEVEL` ("Level"): Triad rank title based on EXP.
   - `Const.CURRENTWEAPON` ("CurrentWeapon"): ID of equipped weapon.
   - `Const.NUM_STORY` ("Num_Story"): Story index.
3. **Rank Progression** (`Const.GetLevel`):
   - Tứ Cửu (Rank 49 / Starter) -> Hồng Côn (Red Pole) -> Song Hoa Hồng Côn (Double Flower Red Pole) -> Nhị Lộ Nguyên Soái (Deputy Leader) -> Long Đầu (Dragon Head).

### 2.4 Match-3 Combat Mechanics (`GameController` & `GameItem`)
- **Grid Structure**: 8x8 grid dynamically populated with `GameItem` prefabs.
- **Item Modes (`itemMode`)**:
  - `Attack`: Deals weapon-scaled damage to enemy.
  - `Health`: Heals player character.
  - `Skill`: Multiplies player attack power (x2, x3).
  - `Food / Star / Level`: Utility / EXP / Mana recovery.
- **Combat Loop**:
  - Enemy operates on an automatic attack timer bar (`Enemy.m_timeToAttack`). When it hits 0, the enemy attacks the player.
  - Player matches items to attack, defend, heal, or delay enemy attacks.
  - Weapon animations are handled by `WeaponScripts` using DOTween paths and rotation.

---

## 3. Coding Conventions & Standards

### 3.1 C# Code Style
- **Naming Conventions**:
  - Classes, Structs, Enums, Methods, Properties: `PascalCase` (e.g. `GameController`, `PlayAudio`).
  - Serialized private fields: prefixed with `m_` (e.g. `[SerializeField] Image m_imgAvatar;`, `m_textGold`) or camelCase with clear intent.
  - Constants: `SCREAMING_SNAKE_CASE` or `PascalCase` grouped inside `Const.cs`.
- **Unity 6 Best Practices**:
  - Replace deprecated `FindObjectOfType<T>()` with `FindFirstObjectByType<T>()` or `FindAnyObjectByType<T>()`.
  - Prefer explicit null checks (`obj != null`) before accessing Unity Object components.
  - Cache `GetComponent<T>()` results in `Awake()` or `Start()` instead of repeating in `Update()`.

### 3.2 DOTween Lifecycle Management
- Always check if the target `GameObject` or `Transform` is active and not destroyed before running tweens.
- In `OnDestroy()` or when interrupting animations, kill active tweens using `.Kill()` to prevent `MissingReferenceException` or memory leaks when changing scenes:
  ```csharp
  private Tween moveTween;
  private void OnDestroy() {
      moveTween?.Kill();
  }
  ```

### 3.3 Text & UI
- Always use `TextMeshProUGUI` instead of legacy `UnityEngine.UI.Text` for all new UI text elements.
- Manage UI screens via `PopupBase` or `SS.UI.ScreenController` for smooth scale/fade animations.

### 3.4 Unity Asset & Meta Integrity
- **CRITICAL**: Never create, rename, or delete assets without keeping their corresponding `.meta` files in sync.
- Do not modify or commit files inside `Library/`, `Temp/`, `obj/`, or `Logs/`.
- Ensure new scripts have unique names to avoid conflicts with imported asset packs (`FPS Pack`, `Hit & Slashes`, `Thirdparties`).

---

## 4. File Structure Reference

```
Assets/
├── Project/                   # CORE GAME PROJECT (Work here)
│   ├── Editor/                # Custom editor scripts and inspectors
│   ├── Font/                  # Fonts used by TextMeshPro
│   ├── Prefabs/               # Game prefabs (Items, Characters, UI)
│   ├── Scenes/                # All active game scenes (.unity)
│   │   └── Scripts/           # Core gameplay, controllers, managers
│   │       ├── Home/          # Main menu logic
│   │       └── Story/         # Story dialogue & visual novel logic
│   └── Sprites/               # Project-specific 2D sprites & artwork
├── MathThreeGame/             # Match-3 mechanics base & legacy GoKit assets
├── Thirdparties/              # Third-party utilities (SS UI, ScreenUtils, Epic Toon FX)
├── Demigiant/                 # DOTween & DOTweenPro
└── TextMesh Pro/              # TextMeshPro package assets
```

---

## 5. Codebase Discovery & System Navigation (Codebase Memory MCP)

> **CRITICAL RULE**: ALWAYS prefer `codebase-memory-mcp` tools over grep/glob/manual file search when exploring files, inspecting system architecture, discovering symbols, or analyzing call chains.

### 5.1 MCP Tool Priority Order
1. **`get_architecture`**: Use first when analyzing high-level project structure, module breakdowns, or system relationships.
2. **`search_graph`**: Find functions, classes, routes, components, and variables by regex or pattern (e.g. `search_graph(name_pattern=".*GameController.*")`).
3. **`trace_path`**: Trace call hierarchies and dependencies — discover callers (`inbound`) or callees (`outbound`).
4. **`get_code_snippet`**: Retrieve the exact snippet and definition of a qualified class or method.
5. **`check_index_coverage`**: Validate candidate paths and index coverage before making negative or exhaustive claims.
6. **`query_graph`**: Execute Cypher queries for complex relationship queries across code nodes.

### 5.2 When to Fall Back to Grep / Glob
Only fall back to terminal search tools (`grep`, `Get-ChildItem`, `Select-String`) when:
- Searching for raw string literals, localization keys, or UI text content.
- Inspecting non-code or engine assets (`.unity`, `.prefab`, `.mat`, `.json`, `.txt`, `.meta`).
- MCP tools return insufficient results or index coverage reports unindexed ranges.

