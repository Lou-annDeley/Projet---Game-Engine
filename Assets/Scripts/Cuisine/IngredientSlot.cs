using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Un bocal sur l'étagère : on le glisse-dépose dans le chaudron.
// Besoin dans la scène : un EventSystem + un Physics 2D Raycaster sur la caméra.
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class IngredientSlot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public IngredientData ingredient;

    SpriteRenderer rendu;
    Camera cam;
    GameObject copieDrag;       // l'image qui suit la souris pendant le glisser
    Color couleurNormale = Color.white;
    Vector3 tailleDeBase;
    bool abonne;

    static Sprite spriteParDefaut;

    void Awake()
    {
        rendu = GetComponent<SpriteRenderer>();
        cam = Camera.main;
        tailleDeBase = transform.localScale;
    }

    void Start()
    {
        tailleDeBase = transform.localScale;
        if (ingredient != null && rendu.sprite == null) AppliquerVisuel();
        if (StockManager.Instance != null && !abonne)
        {
            StockManager.Instance.StockChange += SurStockChange;
            abonne = true;
        }
        MettreAJourDisponibilite();
    }

    void OnDestroy()
    {
        if (abonne && StockManager.Instance != null) StockManager.Instance.StockChange -= SurStockChange;
    }

    public void Initialiser(IngredientData data)
    {
        ingredient = data;
        AppliquerVisuel();
    }

    void AppliquerVisuel()
    {
        bool aUnSprite = ingredient.sprite != null;
        rendu.sprite = aUnSprite ? ingredient.sprite : SpriteParDefaut();
        couleurNormale = aUnSprite ? Color.white : CouleurDuType(ingredient.type);

        var col = GetComponent<BoxCollider2D>();
        col.size = rendu.sprite.bounds.size;
        col.offset = rendu.sprite.bounds.center;
        MettreAJourDisponibilite();
    }

    void SurStockChange(IngredientData ing, int quantite)
    {
        if (ing == ingredient) MettreAJourDisponibilite();
    }

    bool EstDisponible()
    {
        return ingredient != null && StockManager.Instance != null && StockManager.Instance.Quantite(ingredient) > 0;
    }

    // Bocal grisé quand le stock est vide
    void MettreAJourDisponibilite()
    {
        if (rendu == null || ingredient == null) return;
        rendu.color = EstDisponible() ? couleurNormale : new Color(0.35f, 0.35f, 0.35f, 0.6f);
    }

    // ---------- Survol ----------
    public void OnPointerEnter(PointerEventData e)
    {
        if (EstDisponible()) transform.localScale = tailleDeBase * 1.1f;
    }

    public void OnPointerExit(PointerEventData e)
    {
        transform.localScale = tailleDeBase;
    }

    // ---------- Glisser-déposer ----------
    public void OnBeginDrag(PointerEventData e)
    {
        if (!EstDisponible()) { e.pointerDrag = null; return; }

        copieDrag = new GameObject("Drag_" + ingredient.id);
        var sr = copieDrag.AddComponent<SpriteRenderer>();
        sr.sprite = rendu.sprite;
        sr.color = couleurNormale;
        sr.sortingOrder = 100;
        copieDrag.transform.localScale = transform.lossyScale * 1.1f;
        SuivreSouris(e);
    }

    public void OnDrag(PointerEventData e)
    {
        SuivreSouris(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (copieDrag == null) return;
        Destroy(copieDrag);

        GameObject cible = e.pointerCurrentRaycast.gameObject;
        Chaudron chaudron = cible != null ? cible.GetComponentInParent<Chaudron>() : null;
        if (chaudron == null) return; // lâché ailleurs : rien ne se passe

        if (chaudron.PeutAjouter && StockManager.Instance.Retirer(ingredient))
            chaudron.AjouterIngredient(ingredient);
        else
            StartCoroutine(Secouer()); // chaudron plein ou plus de stock
    }

    void SuivreSouris(PointerEventData e)
    {
        if (copieDrag == null || cam == null) return;
        Vector3 p = cam.ScreenToWorldPoint(e.position);
        p.z = 0f;
        copieDrag.transform.position = p;
    }

    IEnumerator Secouer()
    {
        Vector3 depart = transform.localPosition;
        for (float t = 0; t < 0.25f; t += Time.deltaTime)
        {
            transform.localPosition = depart + Vector3.right * Mathf.Sin(t * 60f) * 0.08f;
            yield return null;
        }
        transform.localPosition = depart;
    }

    // ---------- Visuels provisoires (en attendant les sprites de Lou-Ann) ----------
    static Sprite SpriteParDefaut()
    {
        if (spriteParDefaut == null)
            spriteParDefaut = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return spriteParDefaut;
    }

    // Couleurs de la palette de la DA
    static Color CouleurDuType(TypeIngredient type)
    {
        switch (type)
        {
            case TypeIngredient.Toxique: return Hex("#6BA027"); // Vert poison
            case TypeIngredient.Secret:  return Hex("#FAE4B1"); // Bougie
            case TypeIngredient.Magique: return Hex("#693287"); // Violet magique
            case TypeIngredient.Remede:  return Hex("#597CB0"); // Bleu brume
            default:                     return Hex("#C88F60"); // Laiton
        }
    }

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out Color c);
        return c;
    }
}
