using System;
using System.Collections.Generic;
using UnityEngine;

namespace NTGH.Boosters
{
    /// <summary>
    /// Line Blast Booster: taps any tile on the board to clear its entire row and column,
    /// chaining any bonus tiles and triggering damage, collapse, and cascades.
    /// </summary>
    public class LineBlastBooster : BoosterBase
    {
        public override BoosterType BoosterType => BoosterType.LineBlast;
        public override bool RequiresTarget => true;

        public override void OnTargetingUpdate(ShapesManager board, Func<bool> tryConsumeCharge)
        {
            if (board == null || board.shapes == null || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (!board.TryGetClickedValidTile(out GameObject targetGo))
            {
                return;
            }

            Shape targetShape = targetGo.GetComponent<Shape>();
            if (targetShape == null)
            {
                return;
            }

            if (tryConsumeCharge != null && !tryConsumeCharge())
            {
                board.CancelBoosterMode();
                return;
            }

            var targets = new List<GameObject>();
            targets.AddRange(board.shapes.GetRow(targetShape.Row));
            targets.AddRange(board.shapes.GetColumn(targetShape.Column));

            board.StartBoosterCoroutine(
                board.ClearTilesAndCascadeCoroutine(targets, BoosterType.LineBlast),
                BoosterType.LineBlast);
        }
    }
}
