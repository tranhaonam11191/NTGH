using System;
using System.Collections.Generic;
using UnityEngine;

namespace NTGH.Boosters
{
    /// <summary>
    /// Hammer Booster: taps any single tile on the board to destroy it immediately,
    /// triggering bonus chains, damage, collapse, and cascades.
    /// </summary>
    public class HammerBooster : BoosterBase
    {
        public override BoosterType BoosterType => BoosterType.Hammer;
        public override bool RequiresTarget => true;

        public override void OnTargetingUpdate(ShapesManager board, Func<bool> tryConsumeCharge)
        {
            if (board == null || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (!board.TryGetClickedValidTile(out GameObject targetGo))
            {
                return;
            }

            if (tryConsumeCharge != null && !tryConsumeCharge())
            {
                board.CancelBoosterMode();
                return;
            }

            var targets = new List<GameObject> { targetGo };
            board.StartBoosterCoroutine(
                board.ClearTilesAndCascadeCoroutine(targets, BoosterType.Hammer),
                BoosterType.Hammer);
        }
    }
}
