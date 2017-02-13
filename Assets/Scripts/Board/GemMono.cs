﻿using System.Collections;

namespace Board
{
    using UnityEngine;

    using Information;

    using UnityEngine.UI;

    [RequireComponent(typeof(Image))]
    public class GemMono : MonoBehaviour, IComponent
    {
        [SerializeField]
        private Image m_Image;
        [SerializeField]
        private RectTransform m_RectTransform;

        [SerializeField]
        private Gem m_Gem;

        private Vector3 m_PositionOffset;
        private float m_ReducePositionOffsetTime = 1f;
        private Coroutine m_ReducePositionOffsetCoroutine;

        private Vector3 m_CurrentPosition;
        private float m_MoveToPositionTime = 1f;
        private Coroutine m_MoveToPositionCoroutine;

        private Coroutine m_UpdatePositionCoroutine;

        public Gem gem
        {
            get { return m_Gem; }
            private set { m_Gem = value; }
        }

        public Vector3 positionOffset
        {
            get { return m_PositionOffset; }
            set
            {
                m_PositionOffset = value;

                if (m_ReducePositionOffsetCoroutine != null)
                    StopCoroutine(m_ReducePositionOffsetCoroutine);
                if (m_UpdatePositionCoroutine == null)
                    m_UpdatePositionCoroutine = StartCoroutine(UpdatePosition());

                m_ReducePositionOffsetCoroutine = StartCoroutine(ReducePositionOffset());
            }
        }

        public Grid grid { get { return m_Gem.grid; } }
        public GridMono gridMono { get { return grid.GetComponent<GridMono>(); } }

        public Vector2 position { get { return m_Gem.position; } set { m_Gem.position = value; } }
        public GemType gemType { get { return m_Gem.gemType; } set { m_Gem.gemType = value; } }

        public Row row { get { return m_Gem.row; } }
        public GridCollectionMono rowMono
        {
            get { return m_Gem.row.GetComponent<GridCollectionMono>(); }
        }

        public Column column { get { return m_Gem.column; } }
        public GridCollectionMono columnMono
        {
            get { return m_Gem.column.GetComponent<GridCollectionMono>(); }
        }

        private void Start()
        {
            // Now that everything should be initialized, we can show the gem visually
            m_Image.enabled = true;

            m_CurrentPosition =
                new Vector2(
                    m_Gem.position.x
                    * (gridMono.rectTransform.rect.width
                    / (grid.size.x - 1)),

                    gridMono.rectTransform.rect.height);

            m_RectTransform.anchoredPosition = m_CurrentPosition;

            OnPositionChange(new PositionChangeInformation { gem = gem, newPosition = gem.position });
        }

        private void OnTypeChange(TypeChangeInformation typeChangeInfo)
        {
            // This should not happen because we only subscribe to the gem we care about but still...
            if (typeChangeInfo.gem != gem)
                return;

            var spriteIndex = (int)typeChangeInfo.newType;

            m_Image.sprite = CombatManager.self.gemSprites[spriteIndex];
        }
        private void OnPositionChange(PositionChangeInformation positionChangeInfo)
        {
            if (m_MoveToPositionCoroutine != null)
                StopCoroutine(m_MoveToPositionCoroutine);

            m_MoveToPositionCoroutine =
                StartCoroutine(
                    MoveToPosition(
                        new Vector2(
                            positionChangeInfo.newPosition.x
                            * (gridMono.rectTransform.rect.width
                            / (grid.size.x - 1)),

                            positionChangeInfo.newPosition.y
                            * (gridMono.rectTransform.rect.height
                            / (grid.size.y - 1)))));

            name = "Gem " + positionChangeInfo.newPosition;
        }

        private void OnMatch(MatchInformation matchInfo)
        {
            //TODO: Check if this gem matched and play an animation if so. Delete afterwards
        }
        private void OnGridChange(GridChangeInformation gridChangeInfo)
        {
            //TODO: Check to see if this gem was changed in the grid
        }

        private IEnumerator MoveToPosition(Vector2 newPosition)
        {
            var deltaTime = 0f;
            while (deltaTime < m_MoveToPositionTime)
            {
                m_CurrentPosition =
                    Vector2.Lerp(
                        m_CurrentPosition,
                        newPosition,
                        deltaTime / m_MoveToPositionTime);

                deltaTime += Time.deltaTime;

                if (m_UpdatePositionCoroutine == null)
                    m_UpdatePositionCoroutine = StartCoroutine(UpdatePosition());

                yield return null;
            }

            if (m_UpdatePositionCoroutine == null)
                m_UpdatePositionCoroutine = StartCoroutine(UpdatePosition());

            m_MoveToPositionCoroutine = null;
        }

        private IEnumerator ReducePositionOffset()
        {
            yield return null;

            while (m_PositionOffset != Vector3.zero)
            {
                m_PositionOffset =
                    Vector2.MoveTowards(m_PositionOffset, Vector2.zero, 100f * Time.deltaTime);

                if (m_UpdatePositionCoroutine == null)
                    m_UpdatePositionCoroutine = StartCoroutine(UpdatePosition());

                yield return null;
            }

            if (m_UpdatePositionCoroutine == null)
                m_UpdatePositionCoroutine = StartCoroutine(UpdatePosition());

            m_ReducePositionOffsetCoroutine = null;
        }

        private IEnumerator UpdatePosition()
        {
            yield return new WaitForEndOfFrame();

            m_RectTransform.anchoredPosition = m_CurrentPosition + m_PositionOffset;

            m_UpdatePositionCoroutine = null;
        }

        public static void Init()
        {
            Gem.onCreate.AddListener(OnGemCreate);
        }

        private static void OnGemCreate(Gem newGem)
        {
            var newGameObject = new GameObject();
            var newGemMono = newGameObject.AddComponent<GemMono>();

            newGemMono.m_Image = newGameObject.GetComponent<Image>();
            // Hide the gem visually for now
            newGemMono.m_Image.enabled = false;

            newGemMono.m_RectTransform = newGameObject.GetComponent<RectTransform>();

            newGemMono.m_RectTransform.anchorMin = Vector2.zero;
            newGemMono.m_RectTransform.anchorMax = Vector2.zero;
            newGemMono.m_RectTransform.sizeDelta = Vector2.zero;

            newGemMono.m_RectTransform.anchoredPosition = Vector2.zero;

            newGemMono.m_CurrentPosition = newGemMono.m_RectTransform.anchoredPosition;

            newGemMono.gem = newGem;
            newGem.components.Add(newGemMono);

            // Subscribe to the relevant events
            newGemMono.gem.onTypeChange.AddListener(newGemMono.OnTypeChange);
            newGemMono.gem.onPositionChange.AddListener(newGemMono.OnPositionChange);

            var gridMono = newGemMono.gridMono;

            gridMono.grid.onMatch.AddListener(newGemMono.OnMatch);
            gridMono.grid.onGridChange.AddListener(newGemMono.OnGridChange);

            // Parent the object before moving it
            newGameObject.transform.SetParent(gridMono.transform);

            newGemMono.OnPositionChange(
                new PositionChangeInformation
                {
                    gem = newGemMono.gem,
                    newPosition = newGemMono.gem.position,
                });
            newGemMono.OnTypeChange(
                new TypeChangeInformation
                {
                    gem = newGemMono.gem,
                    newType = newGemMono.gem.gemType,
                });

            gridMono.onGridResize.AddListener(
                empty => newGemMono.OnPositionChange(
                    new PositionChangeInformation
                    {
                        gem = newGemMono.gem,
                        newPosition = newGemMono.gem.position
                    }));

            newGemMono.m_Image.SetNativeSize();
        }
    }
}
