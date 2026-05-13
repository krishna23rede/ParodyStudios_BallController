using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class Controller : MonoBehaviour
{
    [Header("References")]
    public RectTransform meterRect;
    public RectTransform indicatorRect;
 
    [Header("Motion Settings")]
    public float speed = 300f;
 
    [Header("Runtime Coloured Zones")]
    public bool createZonesAtRuntime = true;
 
    private bool  _running = true;
    private float _t       = 0f;
    private float _halfH;
 
    private static readonly Color ColRed    = new Color(0.90f, 0.10f, 0.10f);
    private static readonly Color ColYellow = new Color(1.00f, 0.75f, 0.00f);
    private static readonly Color ColGreen  = new Color(0.20f, 0.85f, 0.20f);
    private static readonly Color ColBlue   = new Color(0.15f, 0.50f, 0.95f);
 
    void Start()
    {
        if (meterRect == null)
            meterRect = GetComponent<RectTransform>();

        // FORCE proper anchor setup
        indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRect.pivot = new Vector2(0.5f, 0.5f);

        Debug.Log("Click to bowl!");

        if (createZonesAtRuntime)
            StartCoroutine(BuildZonesNextFrame());
        else
            CacheHalfH();
    }
 
    private void CacheHalfH()
    {
        _halfH = (meterRect.rect.height - indicatorRect.rect.height) * 0.5f;
    }
 
    private IEnumerator BuildZonesNextFrame()
    {
        yield return null;  // wait for Unity layout pass
        CacheHalfH();
        BuildZones();
    }
 
    void Update()
    {
        if (!_running || _halfH == 0f) return;
 
        _t += Time.deltaTime * speed / (2f * _halfH);
        _t  = Mathf.Repeat(_t, 1f);
 
        float pingPong = Mathf.PingPong(_t * 2f, 1f);
        float posY = Mathf.Lerp(-_halfH, _halfH, pingPong);
 
        SetIndicatorY(posY);
    }
 
    public void OnPointerClick()
    {
        if (!_running)
        {
            // Tap again to re-bowl
            _running = true;
            Debug.Log("Click to bowl!");
            return;
        }
 
        _running = false;
        float result = SampleDelivery();
 
        Debug.Log($"[ControlMeter] Delivery → {result:0.0}%");
 
        StartCoroutine(FlashIndicator());
    }
    public float SampleDelivery()
    {
        float norm = Mathf.Clamp01(
            (indicatorRect.anchoredPosition.y + _halfH) / (2f * _halfH));

        // Distance from center
        float distance = Mathf.Abs(norm - 0.5f);

        float percentage = distance switch
        {
            < 0.10f => Mathf.Lerp(100f, 70f,
                        distance / 0.10f),

            < 0.25f => Mathf.Lerp(70f, 40f,
                        (distance - 0.10f) / 0.15f),

            < 0.35f => Mathf.Lerp(40f, 0f,
                        (distance - 0.25f) / 0.10f),

            _ => 0f
        };

        return percentage;
    }
 
    public float SampleValue() => SampleDelivery();
 
    private void SetIndicatorY(float localY)
    {
        Vector2 pos   = indicatorRect.anchoredPosition;
        pos.y         = localY;
        indicatorRect.anchoredPosition = pos;
    }
 
    private IEnumerator FlashIndicator()
    {
        Image img = indicatorRect.GetComponent<Image>();
        if (img == null) yield break;
        Color orig = img.color;
        img.color  = Color.white;
        yield return new WaitForSeconds(0.08f);
        img.color  = orig;
    }
    private void BuildZones()
    {
        foreach (Transform child in meterRect)
            if (child != indicatorRect)
                Destroy(child.gameObject);
 
        // Must sum to 1.0
        (Color col, float frac)[] bands =
        {
            (ColRed,    0.15f),  // bottom red
            (ColYellow, 0.10f),
            (ColGreen,  0.15f),
            (ColBlue,   0.20f),  // centre blue
            (ColGreen,  0.15f),
            (ColYellow, 0.10f),
            (ColRed,    0.15f),  // top red
        };
 
        float cursor = 0f;
        int   idx    = 0;
        foreach (var (col, frac) in bands)
        {
            GameObject go = new GameObject($"Zone_{idx++}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(meterRect, false);
 
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, cursor);
            rt.anchorMax = new Vector2(1f, cursor + frac);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
 
            go.GetComponent<Image>().color = col;
            cursor += frac;
        }
 
        indicatorRect.SetAsLastSibling();
    }
}