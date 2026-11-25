using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NTGH.Boosters;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class ShapesManager : MonoBehaviour
{
    [SerializeField] GameObject _content;
    public Text DebugText, ScoreText;
    public bool ShowDebugInfo = false;
    //candy graphics taken from http://opengameart.org/content/candy-pack-1

    public ShapesArray shapes;

    private int score;

    public Vector2 BottomRight {get; private set;}    

    private GameState state = GameState.None;
    private GameObject hitGo = null;
    private Vector2[] SpawnPositions;
    public GameObject[] CandyPrefabs;
    public GameObject[] ExplosionPrefabs;
    public GameObject[] BonusPrefabs;

    private IEnumerator CheckPotentialMatchesCoroutine;
    private IEnumerator AnimatePotentialMatchesCoroutine;

    IEnumerable<GameObject> potentialMatches;

    public SoundManager soundManager;
    [SerializeField] private float m_boardCenterY = -1.6f;

    // Booster runtime state & concurrency guards
    private bool m_isBoosterExecuting;
    private BoosterType? m_activeBooster;
    private Func<BoosterType, bool> m_boosterConsumeCallback;
    private GameObject m_freeSwapFirstGo;
    private Vector3 m_freeSwapFirstBaseScale = Vector3.one;
    private Color m_freeSwapFirstBaseColor = Color.white;
    private bool m_freeSwapDragging;

    public bool IsBoardBusy => state == GameState.Animating || m_isBoosterExecuting;
    public BoosterType? ActiveBoosterMode => m_activeBooster;

    public event Action<BoosterType?> OnBoosterModeChanged;
    public event Action<BoosterType> OnBoosterExecuted;

    private bool IsBoardBlocked => GameController.instance != null && GameController.instance.BlocksBoard;

    private Vector2 GetCellWorldPosition(int row, int column)
    {
        return BottomRight + new Vector2(column * Constants.ItemSize.x, row * Constants.ItemSize.y);
    }

    private static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            return true;
        return false;
    }

    /// <summary>
    /// Attempts to activate or toggle a booster. Returns false if the board is busy (falling/animating) or blocked.
    /// </summary>
    public bool TryActivateBooster(BoosterType type, Func<BoosterType, bool> consumeCallback)
    {
        if (IsBoardBlocked || IsBoardBusy || shapes == null)
        {
            return false;
        }

        // Toggle off if player taps the same active targeting booster again.
        if (m_activeBooster.HasValue && m_activeBooster.Value == type)
        {
            CancelBoosterMode();
            return true;
        }

        // Reset any unfinished normal drag selection cleanly.
        if (state == GameState.SelectionStarted)
        {
            hitGo = null;
            state = GameState.None;
        }

        ClearFreeSwapSelection();

        if (type == BoosterType.Shuffle)
        {
            CancelBoosterMode();

            if (consumeCallback != null && !consumeCallback(BoosterType.Shuffle))
            {
                return false;
            }

            // Lock board synchronously before starting coroutine to prevent same-frame double-triggers.
            m_isBoosterExecuting = true;
            state = GameState.Animating;
            StopCheckForPotentialMatches();
            StartCoroutine(ExecuteShuffleCoroutine());
            return true;
        }

        m_activeBooster = type;
        m_boosterConsumeCallback = consumeCallback;
        StopCheckForPotentialMatches();
        OnBoosterModeChanged?.Invoke(m_activeBooster);
        return true;
    }

    public void CancelBoosterMode()
    {
        ClearFreeSwapSelection();
        bool hadActive = m_activeBooster.HasValue;
        m_activeBooster = null;
        m_boosterConsumeCallback = null;

        if (hadActive)
        {
            OnBoosterModeChanged?.Invoke(null);
            if (!IsBoardBusy && !IsBoardBlocked)
            {
                StartCheckForPotentialMatches();
            }
        }
    }

    private void ClearFreeSwapSelection()
    {
        m_freeSwapDragging = false;
        if (m_freeSwapFirstGo != null)
        {
            m_freeSwapFirstGo.transform.localScale = m_freeSwapFirstBaseScale;
            SpriteRenderer sr = m_freeSwapFirstGo.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = m_freeSwapFirstBaseColor;
            }
            m_freeSwapFirstGo = null;
        }
    }

    private void SelectFreeSwapFirst(GameObject targetGo)
    {
        ClearFreeSwapSelection();
        if (targetGo == null) return;

        m_freeSwapFirstGo = targetGo;
        m_freeSwapFirstBaseScale = targetGo.transform.localScale;
        SpriteRenderer sr = targetGo.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            m_freeSwapFirstBaseColor = sr.color;
            sr.color = new Color(0.55f, 1f, 1f, 1f);
        }
        targetGo.transform.localScale = m_freeSwapFirstBaseScale * 1.12f;
    }
    void Awake()
    {
        DebugText.enabled = ShowDebugInfo;

        BottomRight = new Vector2(Constants.ItemSize.x * -1 * ((Constants.Columns - 1) / 2 ),Constants.ItemSize.y * -1 * ((float)(Constants.Rows - 1) / 2 ) + m_boardCenterY);        
    }    
    void resetMapFollowOffset()
    {
        
    }     
    // Use this for initialization
    void Start()
    {
        InitializeTypesOnPrefabShapesAndBonuses();

        InitializeCandyAndSpawnPositions();

        StartCheckForPotentialMatches();
                      
    }

    /// <summary>
    /// Initialize shapes
    /// </summary>
    private void InitializeTypesOnPrefabShapesAndBonuses()
    {
        //just assign the name of the prefab
        foreach (var item in CandyPrefabs)
        {
            item.GetComponent<Shape>().Type = item.name;

        }

        //assign the name of the respective "normal" candy as the type of the Bonus
        foreach (var item in BonusPrefabs)
        {
            item.GetComponent<Shape>().Type = CandyPrefabs.
                Where(x => x.GetComponent<Shape>().Type.Contains(item.name.Split('_')[1].Trim())).Single().name;
        }
    }

    public void InitializeCandyAndSpawnPositionsFromPremadeLevel()
    {
        InitializeVariables();

        var premadeLevel = DebugUtilities.FillShapesArrayFromResourcesData();

        if (shapes != null)
            DestroyAllCandy();

        shapes = new ShapesArray();
        SpawnPositions = new Vector2[Constants.Columns];

        for (int row = 0; row < Constants.Rows; row++)
        {
            for (int column = 0; column < Constants.Columns; column++)
            {

                GameObject newCandy = null;

                newCandy = GetSpecificCandyOrBonusForPremadeLevel(premadeLevel[row, column]);

                InstantiateAndPlaceNewCandy(row, column, newCandy);

            }
        }

        SetupSpawnPositions();
    }


    public void InitializeCandyAndSpawnPositions()
    {
        InitializeVariables();

        if (shapes != null)
            DestroyAllCandy();

        shapes = new ShapesArray();
        SpawnPositions = new Vector2[Constants.Columns];

        for (int row = 0; row < Constants.Rows; row++)
        {
            for (int column = 0; column < Constants.Columns; column++)
            {

                GameObject newCandy = GetRandomCandy();

                //check if two previous horizontal are of the same type
                while (column >= 2 && shapes[row, column - 1].GetComponent<Shape>()
                    .IsSameType(newCandy.GetComponent<Shape>())
                    && shapes[row, column - 2].GetComponent<Shape>().IsSameType(newCandy.GetComponent<Shape>()))
                {
                    newCandy = GetRandomCandy();
                }

                //check if two previous vertical are of the same type
                while (row >= 2 && shapes[row - 1, column].GetComponent<Shape>()
                    .IsSameType(newCandy.GetComponent<Shape>())
                    && shapes[row - 2, column].GetComponent<Shape>().IsSameType(newCandy.GetComponent<Shape>()))
                {
                    newCandy = GetRandomCandy();
                }

                InstantiateAndPlaceNewCandy(row, column, newCandy);

            }
        }

        SetupSpawnPositions();
    }



    private void InstantiateAndPlaceNewCandy(int row, int column, GameObject newCandy)
    {
        Vector2 pos = BottomRight + new Vector2(column * Constants.ItemSize.x, row * Constants.ItemSize.y);
        GameObject go = Instantiate(newCandy,
            pos , Quaternion.identity)
            as GameObject;
            go.transform.SetParent(_content.transform);            
        Debug.Log("InstantiateAndPlaceNewCandy: " + go.name + " row: " + row + " column: " + column + " pos: " + pos);        
        //assign the specific properties
        go.GetComponent<Shape>().Assign(newCandy.GetComponent<Shape>().Type, row, column);
        shapes[row, column] = go;
    }

    private void SetupSpawnPositions()
    {
        //create the spawn positions for the new shapes (will pop from the 'ceiling')
        for (int column = 0; column < Constants.Columns; column++)
        {
            SpawnPositions[column] = BottomRight
                + new Vector2(column * Constants.ItemSize.x, Constants.Rows * Constants.ItemSize.y);
        }
    }




    /// <summary>
    /// Destroy all candy gameobjects
    /// </summary>
    private void DestroyAllCandy()
    {
        for (int row = 0; row < Constants.Rows; row++)
        {
            for (int column = 0; column < Constants.Columns; column++)
            {
                Destroy(shapes[row, column]);
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (IsBoardBlocked)
        {
            if (m_activeBooster.HasValue)
            {
                CancelBoosterMode();
            }
            return;
        }

        if (ShowDebugInfo)
            DebugText.text = DebugUtilities.GetArrayContents(shapes);

        // Hard concurrency guard: never process board clicks or boosters while items are falling/animating.
        if (IsBoardBusy)
        {
            return;
        }

        if (m_activeBooster.HasValue)
        {
            HandleBoosterTargetingInput();
            return;
        }

        if (state == GameState.None)
        {
            //user has clicked or touched
            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI()) return;

                //get the hit position
                var hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (hit.collider != null && hit.collider.GetComponent<Shape>() != null) //we have a hit!!!
                {
                    hitGo = hit.collider.gameObject;
                    state = GameState.SelectionStarted;
                }
            }
        }
        else if (state == GameState.SelectionStarted)
        {
            if (Input.GetMouseButtonUp(0))
            {
                hitGo = null;
                state = GameState.None;
                return;
            }

            //user dragged
            if (Input.GetMouseButton(0))
            {
                if (hitGo == null)
                {
                    state = GameState.None;
                    return;
                }

                var hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                //we have a hit
                if (hit.collider != null && hitGo != hit.collider.gameObject && hit.collider.GetComponent<Shape>() != null)
                {
                    //user did a hit, no need to show him hints 
                    StopCheckForPotentialMatches();

                    //if the two shapes are diagonally aligned (different row and column), just return
                    if (!Utilities.AreVerticalOrHorizontalNeighbors(hitGo.GetComponent<Shape>(),
                        hit.collider.gameObject.GetComponent<Shape>()))
                    {
                        hitGo = null;
                        state = GameState.None;
                    }
                    else
                    {
                        state = GameState.Animating;
                        FixSortingLayer(hitGo, hit.collider.gameObject);
                        StartCoroutine(FindMatchesAndCollapse(hit));
                    }
                }
            }
        }
    }

    private void HandleBoosterTargetingInput()
    {
        if (!m_activeBooster.HasValue || IsBoardBusy || IsBoardBlocked)
        {
            return;
        }

        BoosterType active = m_activeBooster.Value;

        if (active == BoosterType.FreeSwap)
        {
            HandleFreeSwapInput();
            return;
        }

        if (!Input.GetMouseButtonDown(0) || IsPointerOverUI())
        {
            return;
        }

        var hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (hit.collider == null)
        {
            return;
        }

        GameObject targetGo = hit.collider.gameObject;
        Shape targetShape = targetGo.GetComponent<Shape>();
        if (targetShape == null || shapes == null)
        {
            return;
        }

        // Verify the clicked object is actually still registered at its grid cell.
        if (targetShape.Row < 0 || targetShape.Row >= Constants.Rows ||
            targetShape.Column < 0 || targetShape.Column >= Constants.Columns ||
            shapes[targetShape.Row, targetShape.Column] != targetGo)
        {
            return;
        }

        var consume = m_boosterConsumeCallback;
        if (consume != null && !consume(active))
        {
            CancelBoosterMode();
            return;
        }

        m_activeBooster = null;
        m_boosterConsumeCallback = null;
        OnBoosterModeChanged?.Invoke(null);

        // Lock board synchronously before starting coroutine.
        m_isBoosterExecuting = true;
        state = GameState.Animating;
        StopCheckForPotentialMatches();

        if (active == BoosterType.Hammer)
        {
            StartCoroutine(ExecuteClearTargetsCoroutine(new List<GameObject> { targetGo }, BoosterType.Hammer));
        }
        else if (active == BoosterType.LineBlast)
        {
            var targets = new List<GameObject>();
            targets.AddRange(shapes.GetRow(targetShape.Row));
            targets.AddRange(shapes.GetColumn(targetShape.Column));
            StartCoroutine(ExecuteClearTargetsCoroutine(targets, BoosterType.LineBlast));
        }
    }

    private void HandleFreeSwapInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI()) return;

            var hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider == null) return;

            GameObject clickedGo = hit.collider.gameObject;
            Shape clickedShape = clickedGo.GetComponent<Shape>();
            if (clickedShape == null) return;

            if (m_freeSwapFirstGo == null)
            {
                SelectFreeSwapFirst(clickedGo);
                m_freeSwapDragging = true;
                return;
            }

            if (m_freeSwapFirstGo == clickedGo)
            {
                ClearFreeSwapSelection();
                return;
            }

            Shape firstShape = m_freeSwapFirstGo.GetComponent<Shape>();
            if (firstShape != null && Utilities.AreVerticalOrHorizontalNeighbors(firstShape, clickedShape))
            {
                CommitFreeSwap(m_freeSwapFirstGo, clickedGo);
            }
            else
            {
                SelectFreeSwapFirst(clickedGo);
                m_freeSwapDragging = true;
            }
        }
        else if (m_freeSwapDragging && Input.GetMouseButton(0) && m_freeSwapFirstGo != null)
        {
            var hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider == null || hit.collider.gameObject == m_freeSwapFirstGo) return;

            GameObject secondGo = hit.collider.gameObject;
            Shape firstShape = m_freeSwapFirstGo.GetComponent<Shape>();
            Shape secondShape = secondGo.GetComponent<Shape>();
            if (firstShape != null && secondShape != null &&
                Utilities.AreVerticalOrHorizontalNeighbors(firstShape, secondShape))
            {
                CommitFreeSwap(m_freeSwapFirstGo, secondGo);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            m_freeSwapDragging = false;
        }
    }

    private void CommitFreeSwap(GameObject firstGo, GameObject secondGo)
    {
        if (firstGo == null || secondGo == null || IsBoardBusy || IsBoardBlocked)
        {
            ClearFreeSwapSelection();
            return;
        }

        ClearFreeSwapSelection();

        var consume = m_boosterConsumeCallback;
        if (consume != null && !consume(BoosterType.FreeSwap))
        {
            CancelBoosterMode();
            return;
        }

        m_activeBooster = null;
        m_boosterConsumeCallback = null;
        OnBoosterModeChanged?.Invoke(null);

        m_isBoosterExecuting = true;
        state = GameState.Animating;
        StopCheckForPotentialMatches();
        FixSortingLayer(firstGo, secondGo);
        StartCoroutine(ExecuteFreeSwapCoroutine(firstGo, secondGo));
    }

    /// <summary>
    /// Modifies sorting layers for better appearance when dragging/animating
    /// </summary>
    /// <param name="hitGo"></param>
    /// <param name="hitGo2"></param>
    private void FixSortingLayer(GameObject hitGo, GameObject hitGo2)
    {
        SpriteRenderer sp1 = hitGo.GetComponent<SpriteRenderer>();
        SpriteRenderer sp2 = hitGo2.GetComponent<SpriteRenderer>();
        if (sp1.sortingOrder <= sp2.sortingOrder)
        {
            sp1.sortingOrder = 1;
            sp2.sortingOrder = 0;
        }
    }

    private IEnumerator FindMatchesAndCollapse(RaycastHit2D hit2)
    {
        try
        {
            //get the second item that was part of the swipe
            var hitGo2 = hit2.collider != null ? hit2.collider.gameObject : null;
            if (hitGo == null || hitGo2 == null)
            {
                yield break;
            }

            shapes.Swap(hitGo, hitGo2);

            //move the swapped ones
            Vector3 pos1 = hitGo.transform.position;
            Vector3 pos2 = hitGo2.transform.position;
            Go.killAllTweensWithTarget(hitGo.transform);
            Go.killAllTweensWithTarget(hitGo2.transform);
            hitGo.transform.positionTo(Constants.AnimationDuration, pos2);
            hitGo2.transform.positionTo(Constants.AnimationDuration, pos1);
            yield return new WaitForSeconds(Constants.AnimationDuration);

            //get the matches via the helper methods
            var hitGomatchesInfo = shapes.GetMatches(hitGo);
            var hitGo2matchesInfo = shapes.GetMatches(hitGo2);

            var groups = new List<HashSet<GameObject>>();
            AddMatchGroup(groups, hitGomatchesInfo);
            AddMatchGroup(groups, hitGo2matchesInfo);
            var totalMatches = groups.SelectMany(group => group).ToList();

            //if user's swap didn't create at least a 3-match, undo their swap
            if (totalMatches.Count < Constants.MinimumMatches)
            {
                Go.killAllTweensWithTarget(hitGo.transform);
                Go.killAllTweensWithTarget(hitGo2.transform);
                hitGo.transform.positionTo(Constants.AnimationDuration, pos1);
                hitGo2.transform.positionTo(Constants.AnimationDuration, pos2);
                yield return new WaitForSeconds(Constants.AnimationDuration);

                shapes.UndoSwap();
                SnapBoardPositions();
                yield break;
            }

            //if more than 3 matches and no Bonus is contained in the line, we will award a new Bonus
            bool addBonus = totalMatches.Count >= Constants.MinimumMatchesForBonus &&
                !BonusTypeUtilities.ContainsDestroyWholeRowColumn(hitGomatchesInfo.BonusesContained) &&
                !BonusTypeUtilities.ContainsDestroyWholeRowColumn(hitGo2matchesInfo.BonusesContained);

            Shape hitGoCache = null;
            if (addBonus)
            {
                //get the game object that was of the same type
                var sameTypeGo = hitGomatchesInfo.MatchedCandy.Count() > 0 ? hitGo : hitGo2;
                hitGoCache = sameTypeGo.GetComponent<Shape>();
            }

            yield return StartCoroutine(ResolveMatchesAndCollapseLoop(groups, addBonus, hitGoCache));
        }
        finally
        {
            hitGo = null;
            state = GameState.None;
            StartCheckForPotentialMatches();
        }
    }

    private IEnumerator ExecuteFreeSwapCoroutine(GameObject firstGo, GameObject secondGo)
    {
        try
        {
            if (firstGo == null || secondGo == null)
            {
                yield break;
            }

            shapes.Swap(firstGo, secondGo);

            Shape s1 = firstGo.GetComponent<Shape>();
            Shape s2 = secondGo.GetComponent<Shape>();
            Vector2 target1 = GetCellWorldPosition(s1.Row, s1.Column);
            Vector2 target2 = GetCellWorldPosition(s2.Row, s2.Column);

            Go.killAllTweensWithTarget(firstGo.transform);
            Go.killAllTweensWithTarget(secondGo.transform);
            firstGo.transform.positionTo(Constants.AnimationDuration, target1);
            secondGo.transform.positionTo(Constants.AnimationDuration, target2);
            yield return new WaitForSeconds(Constants.AnimationDuration);

            SnapBoardPositions();
            OnBoosterExecuted?.Invoke(BoosterType.FreeSwap);

            // FreeSwap never undoes! If a match was formed, resolve & cascade normally.
            var firstMatches = shapes.GetMatches(firstGo);
            var secondMatches = shapes.GetMatches(secondGo);

            var groups = new List<HashSet<GameObject>>();
            AddMatchGroup(groups, firstMatches);
            AddMatchGroup(groups, secondMatches);
            var totalMatches = groups.SelectMany(group => group).ToList();

            if (totalMatches.Count >= Constants.MinimumMatches)
            {
                bool addBonus = totalMatches.Count >= Constants.MinimumMatchesForBonus &&
                    !BonusTypeUtilities.ContainsDestroyWholeRowColumn(firstMatches.BonusesContained) &&
                    !BonusTypeUtilities.ContainsDestroyWholeRowColumn(secondMatches.BonusesContained);

                Shape bonusAnchor = null;
                if (addBonus)
                {
                    var sameTypeGo = firstMatches.MatchedCandy.Count() > 0 ? firstGo : secondGo;
                    bonusAnchor = sameTypeGo.GetComponent<Shape>();
                }

                yield return StartCoroutine(ResolveMatchesAndCollapseLoop(groups, addBonus, bonusAnchor));
            }
        }
        finally
        {
            m_isBoosterExecuting = false;
            hitGo = null;
            state = GameState.None;
            StartCheckForPotentialMatches();
        }
    }

    private IEnumerator ExecuteClearTargetsCoroutine(List<GameObject> initialTargets, BoosterType boosterType)
    {
        try
        {
            // Expand chain reactions if any targeted tile carries a DestroyWholeRowColumn bonus.
            var expanded = new HashSet<GameObject>();
            var queue = new Queue<GameObject>();

            for (int i = 0; i < initialTargets.Count; i++)
            {
                GameObject go = initialTargets[i];
                if (go != null && expanded.Add(go))
                {
                    queue.Enqueue(go);
                }
            }

            while (queue.Count > 0)
            {
                GameObject current = queue.Dequeue();
                if (current == null) continue;
                Shape s = current.GetComponent<Shape>();
                if (s == null) continue;

                if (BonusTypeUtilities.ContainsDestroyWholeRowColumn(s.Bonus))
                {
                    foreach (var extra in shapes.GetRow(s.Row))
                    {
                        if (extra != null && expanded.Add(extra)) queue.Enqueue(extra);
                    }
                    foreach (var extra in shapes.GetColumn(s.Column))
                    {
                        if (extra != null && expanded.Add(extra)) queue.Enqueue(extra);
                    }
                }
            }

            if (expanded.Count == 0)
            {
                yield break;
            }

            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlaySfx("ClearSFX");
            }

            Vector3 center = Vector3.zero;
            var affectedColumns = new HashSet<int>();
            foreach (var item in expanded)
            {
                if (item == null) continue;
                center += item.transform.position;
                Shape shape = item.GetComponent<Shape>();
                if (shape != null)
                {
                    affectedColumns.Add(shape.Column);
                }
                SpawnTileExplosion(item.transform.position);
                Go.killAllTweensWithTarget(item.transform);
                shapes.Remove(item);
                Destroy(item);
            }

            IncreaseScore(expanded.Count * Constants.Match3Score);
            GameController.instance?.AttackFromBooster(expanded.Count, center / expanded.Count);
            OnBoosterExecuted?.Invoke(boosterType);

            yield return StartCoroutine(CollapseSpawnAndCascadeCoroutine(affectedColumns));
        }
        finally
        {
            m_isBoosterExecuting = false;
            hitGo = null;
            state = GameState.None;
            StartCheckForPotentialMatches();
        }
    }

    private IEnumerator ExecuteShuffleCoroutine()
    {
        try
        {
            var allTiles = new List<GameObject>(Constants.Rows * Constants.Columns);
            for (int row = 0; row < Constants.Rows; row++)
            {
                for (int col = 0; col < Constants.Columns; col++)
                {
                    if (shapes[row, col] != null)
                    {
                        allTiles.Add(shapes[row, col]);
                    }
                }
            }

            if (allTiles.Count < 2)
            {
                yield break;
            }

            List<GameObject> bestOrder = null;
            List<GameObject> fallbackNoMatchOrder = null;
            const int maxAttempts = 40;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var candidate = new List<GameObject>(allTiles);
                for (int i = candidate.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (candidate[i], candidate[j]) = (candidate[j], candidate[i]);
                }

                ApplyPermutationToArray(candidate);
                bool hasImmediate = shapes.HasImmediateMatch();
                bool hasPotential = Utilities.GetPotentialMatches(shapes) != null;

                if (!hasImmediate && hasPotential)
                {
                    bestOrder = candidate;
                    break;
                }

                if (!hasImmediate && fallbackNoMatchOrder == null)
                {
                    fallbackNoMatchOrder = candidate;
                }
            }

            List<GameObject> chosen = bestOrder ?? fallbackNoMatchOrder ?? allTiles;
            ApplyPermutationToArray(chosen);

            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlaySfx("ClearSFX");
            }

            const float shuffleDuration = 0.32f;
            for (int row = 0; row < Constants.Rows; row++)
            {
                for (int col = 0; col < Constants.Columns; col++)
                {
                    GameObject tile = shapes[row, col];
                    if (tile == null) continue;
                    Go.killAllTweensWithTarget(tile.transform);
                    tile.transform.positionTo(shuffleDuration, GetCellWorldPosition(row, col));
                }
            }

            yield return new WaitForSeconds(shuffleDuration);
            SnapBoardPositions();
            OnBoosterExecuted?.Invoke(BoosterType.Shuffle);

            // Safety: if board had too few distinct colors and an immediate match still exists, resolve it cleanly.
            var cascadeGroups = new List<HashSet<GameObject>>();
            for (int row = 0; row < Constants.Rows; row++)
            {
                for (int col = 0; col < Constants.Columns; col++)
                {
                    GameObject tile = shapes[row, col];
                    if (tile != null)
                    {
                        AddMatchGroup(cascadeGroups, shapes.GetMatches(tile));
                    }
                }
            }

            if (cascadeGroups.Count > 0)
            {
                yield return StartCoroutine(ResolveMatchesAndCollapseLoop(cascadeGroups, false, null));
            }
        }
        finally
        {
            m_isBoosterExecuting = false;
            hitGo = null;
            state = GameState.None;
            StartCheckForPotentialMatches();
        }
    }

    private void ApplyPermutationToArray(List<GameObject> orderedTiles)
    {
        int idx = 0;
        for (int row = 0; row < Constants.Rows; row++)
        {
            for (int col = 0; col < Constants.Columns; col++)
            {
                if (idx < orderedTiles.Count)
                {
                    GameObject tile = orderedTiles[idx++];
                    shapes[row, col] = tile;
                    Shape shape = tile.GetComponent<Shape>();
                    if (shape != null)
                    {
                        shape.Row = row;
                        shape.Column = col;
                    }
                }
                else
                {
                    shapes[row, col] = null;
                }
            }
        }
    }

    private IEnumerator CollapseSpawnAndCascadeCoroutine(IEnumerable<int> affectedColumns)
    {
        var collapsedCandyInfo = shapes.Collapse(affectedColumns);
        var newCandyInfo = CreateNewCandyInSpecificColumns(affectedColumns);

        int maxDistance = Mathf.Max(1, Mathf.Max(collapsedCandyInfo.MaxDistance, newCandyInfo.MaxDistance));
        MoveAndAnimate(newCandyInfo.AlteredCandy, maxDistance);
        MoveAndAnimate(collapsedCandyInfo.AlteredCandy, maxDistance);

        yield return new WaitForSeconds(Constants.MoveAnimationMinDuration * maxDistance);
        SnapBoardPositions();

        var groups = new List<HashSet<GameObject>>();
        foreach (var tile in collapsedCandyInfo.AlteredCandy.Union(newCandyInfo.AlteredCandy))
        {
            if (tile != null)
            {
                AddMatchGroup(groups, shapes.GetMatches(tile));
            }
        }

        if (groups.Count > 0)
        {
            yield return StartCoroutine(ResolveMatchesAndCollapseLoop(groups, false, null));
        }
    }

    private IEnumerator ResolveMatchesAndCollapseLoop(List<HashSet<GameObject>> groups, bool addBonus, Shape hitGoCache)
    {
        var totalMatches = groups.SelectMany(group => group).Where(go => go != null).Distinct().ToList();
        int timesRun = 1;

        while (totalMatches.Count >= Constants.MinimumMatches)
        {
            IncreaseScore((totalMatches.Count - 2) * Constants.Match3Score);

            if (timesRun >= 2)
                IncreaseScore(Constants.SubsequentMatchScore);

            if (SoundManager.instance != null)
                SoundManager.instance.PlaySfx("ClearSFX");

            foreach (var group in groups)
            {
                Vector3 center = Vector3.zero;
                int validCount = 0;
                foreach (var tile in group)
                {
                    if (tile == null) continue;
                    center += tile.transform.position;
                    validCount++;
                }
                if (validCount > 0)
                {
                    GameController.instance?.AttackWithMatch(validCount, center / validCount);
                }
            }

            var columns = new HashSet<int>();
            foreach (var item in totalMatches)
            {
                if (item == null) continue;
                Shape s = item.GetComponent<Shape>();
                if (s != null) columns.Add(s.Column);
                Go.killAllTweensWithTarget(item.transform);
                shapes.Remove(item);
                Destroy(item);
            }

            //check and instantiate Bonus if needed
            if (addBonus && hitGoCache != null)
                CreateBonus(hitGoCache);

            addBonus = false;

            //the order the 2 methods below get called is important!!!
            var collapsedCandyInfo = shapes.Collapse(columns);
            var newCandyInfo = CreateNewCandyInSpecificColumns(columns);

            int maxDistance = Mathf.Max(1, Mathf.Max(collapsedCandyInfo.MaxDistance, newCandyInfo.MaxDistance));

            MoveAndAnimate(newCandyInfo.AlteredCandy, maxDistance);
            MoveAndAnimate(collapsedCandyInfo.AlteredCandy, maxDistance);

            //will wait for both of the above animations
            yield return new WaitForSeconds(Constants.MoveAnimationMinDuration * maxDistance);
            SnapBoardPositions();

            //search if there are matches with the new/collapsed items
            groups.Clear();
            foreach (var tile in collapsedCandyInfo.AlteredCandy.Union(newCandyInfo.AlteredCandy))
            {
                if (tile != null)
                    AddMatchGroup(groups, shapes.GetMatches(tile));
            }
            totalMatches = groups.SelectMany(group => group).Where(go => go != null).Distinct().ToList();

            timesRun++;
        }
    }

    private void SnapBoardPositions()
    {
        if (shapes == null) return;
        for (int row = 0; row < Constants.Rows; row++)
        {
            for (int col = 0; col < Constants.Columns; col++)
            {
                GameObject tile = shapes[row, col];
                if (tile == null) continue;
                Go.killAllTweensWithTarget(tile.transform);
                tile.transform.position = GetCellWorldPosition(row, col);
            }
        }
    }

    private void SpawnTileExplosion(Vector3 worldPos)
    {
        if (ExplosionPrefabs == null || ExplosionPrefabs.Length == 0) return;
        GameObject prefab = ExplosionPrefabs[UnityEngine.Random.Range(0, ExplosionPrefabs.Length)];
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, worldPos, Quaternion.identity);
        Destroy(fx, Constants.ExplosionDuration + 0.25f);
    }

    /// <summary>
    /// Creates a new Bonus based on the shape parameter
    /// </summary>
    /// <param name="hitGoCache"></param>
    private void CreateBonus(Shape hitGoCache)
    {
        GameObject Bonus = Instantiate(GetBonusFromType(hitGoCache.typeShape), BottomRight
            + new Vector2(hitGoCache.Column * Constants.ItemSize.x,
                hitGoCache.Row * Constants.ItemSize.y), Quaternion.identity)
            as GameObject;
            Bonus.transform.SetParent(_content.transform);
        shapes[hitGoCache.Row, hitGoCache.Column] = Bonus;
        var BonusShape = Bonus.GetComponent<Shape>();
        //will have the same type as the "normal" candy
        BonusShape.Assign(hitGoCache.Type, hitGoCache.Row, hitGoCache.Column);
        //add the proper Bonus type
        BonusShape.Bonus |= BonusType.DestroyWholeRowColumn;
    }




    /// <summary>
    /// Spawns new candy in columns that have missing ones
    /// </summary>
    /// <param name="columnsWithMissingCandy"></param>
    /// <returns>Info about new candies created</returns>
    private AlteredCandyInfo CreateNewCandyInSpecificColumns(IEnumerable<int> columnsWithMissingCandy)
    {
        AlteredCandyInfo newCandyInfo = new AlteredCandyInfo();

        //find how many null values the column has
        foreach (int column in columnsWithMissingCandy)
        {
            var emptyItems = shapes.GetEmptyItemsOnColumn(column);
            foreach (var item in emptyItems)
            {
                var go = GetRandomCandy();
                GameObject newCandy = Instantiate(go, SpawnPositions[column], Quaternion.identity)
                    as GameObject;
                if (_content != null)
                {
                    newCandy.transform.SetParent(_content.transform);
                }

                newCandy.GetComponent<Shape>().Assign(go.GetComponent<Shape>().Type, item.Row, item.Column);

                if (Constants.Rows - item.Row > newCandyInfo.MaxDistance)
                    newCandyInfo.MaxDistance = Constants.Rows - item.Row;

                shapes[item.Row, item.Column] = newCandy;
                newCandyInfo.AddCandy(newCandy);
            }
        }
        return newCandyInfo;
    }

    /// <summary>
    /// Animates gameobjects to their new position
    /// </summary>
    /// <param name="movedGameObjects"></param>
    private void MoveAndAnimate(IEnumerable<GameObject> movedGameObjects, int distance)
    {
        float duration = Constants.MoveAnimationMinDuration * Mathf.Max(1, distance);
        foreach (var item in movedGameObjects)
        {
            if (item == null) continue;
            Shape shape = item.GetComponent<Shape>();
            if (shape == null) continue;
            Go.killAllTweensWithTarget(item.transform);
            item.transform.positionTo(duration, GetCellWorldPosition(shape.Row, shape.Column));
        }
    }

    // Crossing runs and overlapping bonus clears share one projectile and count each tile once.
    private static void AddMatchGroup(List<HashSet<GameObject>> groups, MatchesInfo match)
    {
        var tiles = new HashSet<GameObject>(match.MatchedCandy);
        if (tiles.Count == 0) return;
        for (int i = groups.Count - 1; i >= 0; i--)
        {
            if (!tiles.Overlaps(groups[i])) continue;
            tiles.UnionWith(groups[i]);
            groups.RemoveAt(i);
        }
        groups.Add(tiles);
    }

    /// <summary>
    /// Get a random candy
    /// </summary>
    /// <returns></returns>
    private GameObject GetRandomCandy()
    {
        return CandyPrefabs[Random.Range(0, CandyPrefabs.Length)];
    }

    private void InitializeVariables()
    {
        score = 0;
        ShowScore();
    }

    private void IncreaseScore(int amount)
    {
        score += amount;
        ShowScore();
    }

    private void ShowScore()
    {
        ScoreText.text = "Score: " + score.ToString();
    }

    /// <summary>
    /// Gets the specified Bonus for the specific type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private GameObject GetBonusFromType(typeShape type)
    {        
        foreach (var item in BonusPrefabs)
        {
            if (item.GetComponent<Shape>().typeShape == type)
                return item;
        }
        throw new System.Exception("Wrong type");
    }

    /// <summary>
    /// Starts the coroutines, keeping a reference to stop later
    /// </summary>
    private void StartCheckForPotentialMatches()
    {
        StopCheckForPotentialMatches();
        //get a reference to stop it later
        CheckPotentialMatchesCoroutine = CheckPotentialMatches();
        StartCoroutine(CheckPotentialMatchesCoroutine);
    }

    /// <summary>
    /// Stops the coroutines
    /// </summary>
    private void StopCheckForPotentialMatches()
    {
        if (AnimatePotentialMatchesCoroutine != null)
            StopCoroutine(AnimatePotentialMatchesCoroutine);
        if (CheckPotentialMatchesCoroutine != null)
            StopCoroutine(CheckPotentialMatchesCoroutine);
        ResetOpacityOnPotentialMatches();
    }

    /// <summary>
    /// Resets the opacity on potential matches (probably user dragged something?)
    /// </summary>
    private void ResetOpacityOnPotentialMatches()
    {
        if (potentialMatches != null)
            foreach (var item in potentialMatches)
            {
                if (item == null) break;

                Color c = item.GetComponent<SpriteRenderer>().color;
                c.a = 1.0f;
                item.GetComponent<SpriteRenderer>().color = c;
            }
    }

    /// <summary>
    /// Finds potential matches
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckPotentialMatches()
    {
        yield return new WaitForSeconds(Constants.WaitBeforePotentialMatchesCheck);
        potentialMatches = Utilities.GetPotentialMatches(shapes);
        if (potentialMatches != null)
        {
            while (true)
            {

                AnimatePotentialMatchesCoroutine = Utilities.AnimatePotentialMatches(potentialMatches);
                StartCoroutine(AnimatePotentialMatchesCoroutine);
                yield return new WaitForSeconds(Constants.WaitBeforePotentialMatchesCheck);
            }
        }
    }

    /// <summary>
    /// Gets a specific candy or Bonus based on the premade level information.
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    private GameObject GetSpecificCandyOrBonusForPremadeLevel(string info)
    {
        var tokens = info.Split('_');

        if (tokens.Count() == 1)
        {
            foreach (var item in CandyPrefabs)
            {
                if (item.GetComponent<Shape>().Type.Contains(tokens[0].Trim()))
                    return item;
            }

        }
        else if (tokens.Count() == 2 && tokens[1].Trim() == "B")
        {
            foreach (var item in BonusPrefabs)
            {
                if (item.name.Contains(tokens[0].Trim()))
                    return item;
            }
        }

        throw new System.Exception("Wrong type, check your premade level");
    }



}
