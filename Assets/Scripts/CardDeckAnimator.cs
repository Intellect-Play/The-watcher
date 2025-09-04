// CardDeckAnimator.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDeckAnimator : MonoBehaviour
{
    [Header("Card Setup")]
    public GameObject    cardPrefab;
    public int           cardCount        = 52;
    public float         stackOffsetY     = .02f;
    public float         zOffsetPerCard   = .001f;

    [Header("Simple Shuffle (no exaggeration)")]
    [Tooltip("How high each card lifts (local Y).")]
    public float         riseHeight       = 0.20f;
    [Tooltip("Time for a card to rise up.")]
    public float         upTime           = 0.15f;
    [Tooltip("Hold time at the peak before falling back.")]
    public float         holdTime         = 0.05f;
    [Tooltip("Time for a card to return back to the deck.")]
    public float         downTime         = 0.15f;
    [Tooltip("Delay between cards starting their rise.")]
    public float         perCardDelay     = 0.04f;

    [Header("Flip Animation (Top Card)")]
    public Sprite        defaultCardBack;
    public float         flipTime         = 0.25f;
    public float         preShuffleDelay  = 0.05f;
    public float         postShuffleDelay = 0.10f;

    [Header("Shuffle Settings")]
    [Tooltip("Seconds between automatic shuffles")]
    public float         shuffleInterval  = 5f;

    [Header("Cards to Fire")]
    public CardSO[]      cards;
    public UIManager     uiManager;
    public SpriteRenderer activeCard;

    [Header("Active Card Reveal (top card visual)")]
    [Tooltip("Sorting order for deck sprites (should match your deck cards).")]
    public int           deckSortingOrder       = 202;
    [Tooltip("Sorting order for the visible 'top card' sprite so it stays above the deck.")]
    public int           activeCardSortingOrder = 250;
    [Tooltip("World-space offset applied to the active card when snapping to the deck top.")]
    public Vector3       activeCardWorldOffset  = new Vector3(0f, 0.06f, 0f);

    private List<GameObject> _cards         = new List<GameObject>();
    private List<Vector3>    _origPositions = new List<Vector3>(); // local positions
    private CardSO           _currentCard;
    private bool             _paused;
    private float            _activeCardOrigScaleX;
    private float            _currentShuffleInterval;
    private bool             _isShuffling = false; // prevent overlaps

    void Start()
    {
        if (activeCard != null)
        {
            _activeCardOrigScaleX   = activeCard.transform.localScale.x;
            activeCard.sortingOrder = activeCardSortingOrder; // always above deck
            activeCard.sprite       = defaultCardBack;        // start with back
        }

        for (int i = 0; i < cardCount; i++)
        {
            var c = Instantiate(cardPrefab, transform);
            c.transform.localScale    = Vector3.one * 0.25f;
            c.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            c.transform.localPosition = new Vector3(0, i * stackOffsetY, i * zOffsetPerCard);

            var childRenderers = c.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in childRenderers)
                r.sortingOrder = deckSortingOrder;

            _cards.Add(c);
            _origPositions.Add(c.transform.localPosition);
        }

        PositionActiveCardAtDeckTop();

        _currentShuffleInterval = shuffleInterval;
        InvokeRepeating(nameof(ShuffleAndDeal), 0f, _currentShuffleInterval);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        CancelAllCardTweens();
        _isShuffling = false;
    }

    public void SetDeck(CardSO[] newCards)
    {
        cards = newCards;
        StopAllCoroutines();
        CancelInvoke(nameof(ShuffleAndDeal));
        _isShuffling = false;
        InvokeRepeating(nameof(ShuffleAndDeal), 0f, _currentShuffleInterval);
    }

    public void PauseFiring()  => _paused = true;
    public void ResumeFiring() => _paused = false;

    public void ShuffleAndDeal()
    {
        if (_paused) return;
        if (_isShuffling) return; // no overlap
        if (uiManager != null && uiManager.roguelikeBoard.activeSelf)
            return;

        _isShuffling = true;

        StopAllCoroutines();
        CancelAllCardTweens();

        // Choose next card (do NOT set top sprite yet; keep deck showing backs during shuffle)
        _currentCard = (cards != null && cards.Length > 0)
            ? cards[Random.Range(0, cards.Length)]
            : null;

        StartCoroutine(FlipThenShuffle());
    }

    private void CancelAllCardTweens()
    {
        foreach (var c in _cards)
            if (c != null) LeanTween.cancel(c);
        if (activeCard != null) LeanTween.cancel(activeCard.gameObject);
    }

    private IEnumerator FlipThenShuffle()
    {
        // Flip-out (close) top card; we’ll show default back during shuffle
        if (activeCard != null)
        {
            LeanTween.cancel(activeCard.gameObject);
            LeanTween.scaleX(activeCard.gameObject, 0f, flipTime).setEaseInOutQuad();
        }

        yield return new WaitForSeconds(flipTime);

        if (activeCard != null)
        {
            activeCard.sprite = defaultCardBack;
            // Hide during shuffle to avoid flicker
            var ls = activeCard.transform.localScale;
            activeCard.transform.localScale = new Vector3(0f, ls.y, ls.z);
            activeCard.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(preShuffleDelay);

        // Simple raise-and-lower shuffle
        yield return StartCoroutine(SimpleShuffleRoutine());

        yield return new WaitForSeconds(postShuffleDelay);

        // Reveal the chosen card with a normal flip at deck top
        if (activeCard != null)
        {
            activeCard.gameObject.SetActive(true);
            LeanTween.cancel(activeCard.gameObject);

            PositionActiveCardAtDeckTop();
            activeCard.sortingOrder = activeCardSortingOrder;

            // Start from scaleX = 0 (closed)
            var ls = activeCard.transform.localScale;
            activeCard.transform.localScale = new Vector3(0f, ls.y, ls.z);

            if (_currentCard != null)
                activeCard.sprite = _currentCard.artwork;

            bool flipDone = false;
            LeanTween.scaleX(activeCard.gameObject, _activeCardOrigScaleX, flipTime)
                     .setEaseInOutQuad()
                     .setOnComplete(() => flipDone = true);

            // Wait for flip-in to complete before firing
            yield return new WaitUntil(() => flipDone);
        }

        // Fire after reveal
        if (!_paused && _currentCard != null)
        {
            Bullet.Fire(_currentCard, transform.position);
        }

        _isShuffling = false;
    }

    // 🔥 Fixed Shuffle Routine: cards now dip downward then rise back
    private IEnumerator SimpleShuffleRoutine()
    {
        if (uiManager != null && uiManager.roguelikeBoard.activeSelf)
            yield break;

        int n = _cards.Count;
        if (n == 0) yield break;

        for (int i = 0; i < n; i++)
        {
            if (uiManager != null && uiManager.roguelikeBoard.activeSelf)
                yield break;

            var c    = _cards[i];
            var orig = _origPositions[i];

            // 👇 Instead of going UP (orig.y + riseHeight), go DOWN
            Vector3 dipPos = new Vector3(orig.x, orig.y - riseHeight, orig.z);

            LeanTween.cancel(c);

            // Down
            LeanTween.moveLocal(c, dipPos, upTime).setEase(LeanTweenType.easeOutQuad);

            // Back up to original
            LeanTween.delayedCall(c, upTime + Mathf.Max(0f, holdTime), () =>
            {
                LeanTween.moveLocal(c, orig, downTime).setEase(LeanTweenType.easeInQuad);
            });

            yield return new WaitForSeconds(perCardDelay);
        }

        // Optional: randomize order so different card leads next time
        FisherYatesShuffle(_cards);
    }

    private void PositionActiveCardAtDeckTop()
    {
        if (activeCard == null) return;

        Vector3 deckTopWorld = GetDeckTopWorld();
        activeCard.transform.position = deckTopWorld + activeCardWorldOffset;
        activeCard.transform.rotation = transform.rotation; // match deck rotation (if any)
        activeCard.sortingOrder       = activeCardSortingOrder;
    }

    private Vector3 GetDeckTopWorld()
    {
        int topIndex = Mathf.Clamp(_cards.Count - 1, 0, _origPositions.Count - 1);
        Vector3 topLocal = (topIndex >= 0 && topIndex < _origPositions.Count)
            ? _origPositions[topIndex]
            : Vector3.zero;
        return transform.TransformPoint(topLocal);
    }

    private void FisherYatesShuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    public void ReduceShuffleDelay(int percent)
    {
        shuffleInterval *= 1f - (percent / 100f);
        CancelInvoke(nameof(ShuffleAndDeal));
        InvokeRepeating(nameof(ShuffleAndDeal), shuffleInterval, shuffleInterval);
    }
}
