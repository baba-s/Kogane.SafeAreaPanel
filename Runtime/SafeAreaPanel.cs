using System;
using UnityEngine;

namespace Kogane
{
    // https://trs-game-techblog.info/entry/ugui-safearea/
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent( typeof( RectTransform ) )]
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        [Serializable]
        public struct Margin
        {
            [SerializeField] private float m_left;
            [SerializeField] private float m_top;
            [SerializeField] private float m_right;
            [SerializeField] private float m_bottom;

            public float Left   => m_left;
            public float Top    => m_top;
            public float Right  => m_right;
            public float Bottom => m_bottom;

            public Margin
            (
                float left,
                float top,
                float right,
                float bottom
            )
            {
                m_left   = left;
                m_top    = top;
                m_right  = right;
                m_bottom = bottom;
            }
        }

        [Flags]
        public enum Edge
        {
            [InspectorName( "Left" )]   LEFT   = 1 << 1,
            [InspectorName( "Right" )]  RIGHT  = 1 << 2,
            [InspectorName( "Top" )]    TOP    = 1 << 3,
            [InspectorName( "Bottom" )] BOTTOM = 1 << 4,
        }

        [SerializeField] private Edge   m_controlEdges = ( Edge )~0;
        [SerializeField] private bool   m_isDisabledExecuteAlways;
        [SerializeField] private Margin m_minimumMargin;

        private Rect       m_currentSafeArea;
        private Vector2Int m_currentResolution;
        private Edge       m_currentControlEdges;

#if UNITY_EDITOR
        private DrivenRectTransformTracker m_drivenRectTransformTracker;
        private bool                       m_isUpdate;
#endif

        public Edge ControlEdges
        {
            get => m_controlEdges;
            set
            {
                m_controlEdges = value;
#if UNITY_EDITOR
                m_isUpdate = true;
#endif
            }
        }

        public bool IsDisabledExecuteAlways
        {
            get => m_isDisabledExecuteAlways;
            set
            {
                m_isDisabledExecuteAlways = value;
#if UNITY_EDITOR
                m_isUpdate = true;
#endif
            }
        }

        public Margin MinimumMargin
        {
            get => m_minimumMargin;
            set
            {
                m_minimumMargin = value;
#if UNITY_EDITOR
                m_isUpdate = true;
#endif
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if ( m_isDisabledExecuteAlways && !Application.isPlaying ) return;
#endif

            Apply( force: true );
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            m_drivenRectTransformTracker.Clear();
        }
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_isUpdate = true;
        }
#endif

        private void Update()
        {
#if UNITY_EDITOR
            if ( m_isUpdate )
            {
                m_isUpdate = false;
                Apply( true );
                return;
            }

            if ( m_isDisabledExecuteAlways && !Application.isPlaying ) return;
#endif

            Apply();
        }

        private void Apply( bool force = false )
        {
            var rectTransform = ( RectTransform )transform;
            var safeArea      = Screen.safeArea;
            var resolution    = new Vector2Int( Screen.width, Screen.height );

            if ( resolution.x == 0 || resolution.y == 0 ) return;

            if ( !force )
            {
                if ( rectTransform.anchorMax == Vector2.zero )
                {
                    // Do apply.
                    // ※Undoすると0になるので再適用させる
                }
                else if (
                    m_currentSafeArea == safeArea &&
                    m_currentResolution == resolution &&
                    m_currentControlEdges == m_controlEdges
                )
                {
                    return;
                }
            }

            m_currentSafeArea     = safeArea;
            m_currentResolution   = resolution;
            m_currentControlEdges = m_controlEdges;

#if UNITY_EDITOR
            m_drivenRectTransformTracker.Clear();
            m_drivenRectTransformTracker.Add
            (
                driver: this,
                rectTransform: rectTransform,
                drivenProperties: DrivenTransformProperties.AnchoredPosition |
                                  DrivenTransformProperties.SizeDelta |
                                  DrivenTransformProperties.AnchorMin |
                                  DrivenTransformProperties.AnchorMax
            );
#endif

            var resultSafeArea = safeArea;

            resultSafeArea.x    = Mathf.Max( safeArea.x, m_minimumMargin.Left );
            resultSafeArea.y    = Mathf.Max( safeArea.y, m_minimumMargin.Bottom );
            resultSafeArea.xMax = Mathf.Min( safeArea.xMax, resolution.x - m_minimumMargin.Right );
            resultSafeArea.yMax = Mathf.Min( safeArea.yMax, resolution.y - m_minimumMargin.Top );

            var normalizedMin = new Vector2( resultSafeArea.xMin / resolution.x, resultSafeArea.yMin / resolution.y );
            var normalizedMax = new Vector2( resultSafeArea.xMax / resolution.x, resultSafeArea.yMax / resolution.y );

            if ( ( m_controlEdges & Edge.LEFT ) == 0 )
            {
                normalizedMin.x = 0;
            }

            if ( ( m_controlEdges & Edge.RIGHT ) == 0 )
            {
                normalizedMax.x = 1;
            }

            if ( ( m_controlEdges & Edge.TOP ) == 0 )
            {
                normalizedMax.y = 1;
            }

            if ( ( m_controlEdges & Edge.BOTTOM ) == 0 )
            {
                normalizedMin.y = 0;
            }

            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta        = Vector2.zero;
            rectTransform.anchorMin        = normalizedMin;
            rectTransform.anchorMax        = normalizedMax;
        }
    }
}