using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NTGH.Boosters
{
    /// <summary>
    /// Shuffle Booster: immediately reshuffles all tiles on the board,
    /// guaranteeing no immediate pre-matches and at least one valid move when possible.
    /// </summary>
    public class ShuffleBooster : BoosterBase
    {
        public override BoosterType BoosterType => BoosterType.Shuffle;
        public override bool RequiresTarget => false;

        public override IEnumerator ExecuteImmediate(ShapesManager board)
        {
            if (board == null || board.shapes == null)
            {
                yield break;
            }

            var allTiles = new List<GameObject>(Constants.Rows * Constants.Columns);
            for (int row = 0; row < Constants.Rows; row++)
            {
                for (int col = 0; col < Constants.Columns; col++)
                {
                    if (board.shapes[row, col] != null)
                    {
                        allTiles.Add(board.shapes[row, col]);
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
                    int j = Random.Range(0, i + 1);
                    (candidate[i], candidate[j]) = (candidate[j], candidate[i]);
                }

                board.ApplyPermutationToArray(candidate);
                bool hasImmediate = board.shapes.HasImmediateMatch();
                bool hasPotential = Utilities.GetPotentialMatches(board.shapes) != null;

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
            board.ApplyPermutationToArray(chosen);

            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlaySfx("ClearSFX");
            }

            const float shuffleDuration = 0.32f;
            for (int row = 0; row < Constants.Rows; row++)
            {
                for (int col = 0; col < Constants.Columns; col++)
                {
                    GameObject tile = board.shapes[row, col];
                    if (tile == null) continue;
                    Go.killAllTweensWithTarget(tile.transform);
                    tile.transform.positionTo(shuffleDuration, board.GetCellWorldPosition(row, col));
                }
            }

            yield return new WaitForSeconds(shuffleDuration);
            board.SnapBoardPositions();
            board.NotifyBoosterExecuted(BoosterType.Shuffle);

            // Safety: if board had too few distinct colors and an immediate match still exists, resolve it cleanly.
            yield return board.ResolveAllBoardMatchesCoroutine();
        }
    }
}
