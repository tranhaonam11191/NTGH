using System;
using System.Collections;
using UnityEngine;

namespace NTGH.Boosters
{
    /// <summary>
    /// Free Swap Booster: swaps any 2 adjacent tiles without requiring a match-3 (no undo).
    /// Supports both tap-tap and drag between adjacent tiles.
    /// </summary>
    public class FreeSwapBooster : BoosterBase
    {
        public override BoosterType BoosterType => BoosterType.FreeSwap;
        public override bool RequiresTarget => true;

        private GameObject m_firstGo;
        private Vector3 m_firstBaseScale = Vector3.one;
        private Color m_firstBaseColor = Color.white;
        private bool m_dragging;

        public override void OnTargetingStarted(ShapesManager board)
        {
            ClearSelection();
        }

        public override void OnTargetingCancelled(ShapesManager board)
        {
            ClearSelection();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearSelection();
        }

        public override void OnTargetingUpdate(ShapesManager board, Func<bool> tryConsumeCharge)
        {
            if (board == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                if (!board.TryGetClickedValidTile(out GameObject clickedGo))
                {
                    return;
                }

                if (m_firstGo == null)
                {
                    SelectFirst(clickedGo);
                    m_dragging = true;
                    return;
                }

                if (m_firstGo == clickedGo)
                {
                    ClearSelection();
                    return;
                }

                Shape firstShape = m_firstGo.GetComponent<Shape>();
                Shape clickedShape = clickedGo.GetComponent<Shape>();
                if (firstShape != null && clickedShape != null &&
                    Utilities.AreVerticalOrHorizontalNeighbors(firstShape, clickedShape))
                {
                    CommitSwap(board, m_firstGo, clickedGo, tryConsumeCharge);
                }
                else
                {
                    SelectFirst(clickedGo);
                    m_dragging = true;
                }
            }
            else if (m_dragging && Input.GetMouseButton(0) && m_firstGo != null)
            {
                if (!board.TryGetClickedValidTile(out GameObject hoveredGo) || hoveredGo == m_firstGo)
                {
                    return;
                }

                Shape firstShape = m_firstGo.GetComponent<Shape>();
                Shape secondShape = hoveredGo.GetComponent<Shape>();
                if (firstShape != null && secondShape != null &&
                    Utilities.AreVerticalOrHorizontalNeighbors(firstShape, secondShape))
                {
                    CommitSwap(board, m_firstGo, hoveredGo, tryConsumeCharge);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                m_dragging = false;
            }
        }

        private void CommitSwap(ShapesManager board, GameObject firstGo, GameObject secondGo, Func<bool> tryConsumeCharge)
        {
            ClearSelection();

            if (firstGo == null || secondGo == null)
            {
                return;
            }

            if (tryConsumeCharge != null && !tryConsumeCharge())
            {
                board.CancelBoosterMode();
                return;
            }

            board.FixSortingLayer(firstGo, secondGo);
            board.StartBoosterCoroutine(ExecuteFreeSwapCoroutine(board, firstGo, secondGo), BoosterType.FreeSwap);
        }

        private IEnumerator ExecuteFreeSwapCoroutine(ShapesManager board, GameObject firstGo, GameObject secondGo)
        {
            if (firstGo == null || secondGo == null || board.shapes == null)
            {
                yield break;
            }

            board.shapes.Swap(firstGo, secondGo);

            Shape s1 = firstGo.GetComponent<Shape>();
            Shape s2 = secondGo.GetComponent<Shape>();
            Vector2 target1 = board.GetCellWorldPosition(s1.Row, s1.Column);
            Vector2 target2 = board.GetCellWorldPosition(s2.Row, s2.Column);

            Go.killAllTweensWithTarget(firstGo.transform);
            Go.killAllTweensWithTarget(secondGo.transform);
            firstGo.transform.positionTo(Constants.AnimationDuration, target1);
            secondGo.transform.positionTo(Constants.AnimationDuration, target2);
            yield return new WaitForSeconds(Constants.AnimationDuration);

            board.SnapBoardPositions();
            board.NotifyBoosterExecuted(BoosterType.FreeSwap);

            // FreeSwap never undoes! If a match was formed, resolve & cascade normally.
            yield return board.ResolveSwapMatchesCoroutine(firstGo, secondGo);
        }

        private void SelectFirst(GameObject targetGo)
        {
            ClearSelection();
            if (targetGo == null) return;

            m_firstGo = targetGo;
            m_firstBaseScale = targetGo.transform.localScale;
            SpriteRenderer sr = targetGo.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                m_firstBaseColor = sr.color;
                sr.color = new Color(0.55f, 1f, 1f, 1f);
            }
            targetGo.transform.localScale = m_firstBaseScale * 1.12f;
        }

        private void ClearSelection()
        {
            m_dragging = false;
            if (m_firstGo != null)
            {
                m_firstGo.transform.localScale = m_firstBaseScale;
                SpriteRenderer sr = m_firstGo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = m_firstBaseColor;
                }
                m_firstGo = null;
            }
        }
    }
}
