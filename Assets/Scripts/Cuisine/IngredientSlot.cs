using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Un bac d'ingrédient (ou un bocal) : on le glisse-dépose dans le chaudron.
// Avec un sprite de bac : le bac est affiché, l'ingrédient est dessiné dedans,
// et c'est l'image de l'ingrédient qui suit la souris pendant le glisser.
// Besoin dans la scène : un EventSystem + un Physics 2D Raycaster sur la caméra.
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class IngredientSlot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public IngredientData ingredient;

    [Tooltip("Sprite du bac (vue de dessus). Vide = l'ingrédient est affiché seul, comme avant.")]
    public Sprite spriteBac;
    [Tooltip("Taille de l'ingrédient dans le bac (1 = toute la largeur du bac).")]
    [Range(0.3f, 1f)] public float tailleDansBac = 0.8f;

    SpriteRenderer renduIngredient; // l'ingrédient dessiné dans le bac

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

    public void Initialiser(IngredientData data, Sprite bac = null)
    {
        ingredient = data;
        spriteBac = bac;
        AppliquerVisuel();
    }

    void AppliquerVisuel()
    {
        bool aUnSprite = ingredient.sprite != null;
        Sprite spriteIngredient = aUnSprite ? ingredient.sprite : SpriteParDefaut();
        couleurNormale = aUnSprite ? Color.white : CouleurDuType(ingredient.type);

        if (spriteBac != null)
        {
            // Le bac porte le collider ; l'ingrédient est un enfant dessiné par-dessus
            rendu.sprite = spriteBac;
            if (renduIngredient == null)
            {
                var enfant = new GameObject("Ingredient");
                enfant.transform.SetParent(transform, false);
                renduIngredient = enfant.AddComponent<SpriteRenderer>();
            }
            renduIngredient.sprite = spriteIngredient;
            renduIngredient.sortingLayerID = rendu.sortingLayerID;
            renduIngredient.sortingOrder = rendu.sortingOrder + 1;
            // Mise à l'échelle pour tenir dans le bac
            Vector2 tBac = spriteBac.bounds.size, tIng = spriteIngredient.bounds.size;
            float echelle = tailleDansBac * Mathf.Min(tBac.x / tIng.x, tBac.y / tIng.y);
            renduIngredient.transform.localScale = Vector3.one * echelle;
            renduIngredient.transform.localPosition = spriteBac.bounds.center - spriteIngredient.bounds.center * echelle;
        }
        else
        {
            rendu.sprite = spriteIngredient;
        }

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

    // Bac (ou bocal) grisé quand le stock est vide
    void MettreAJourDisponibilite()
    {
        if (rendu == null || ingredient == null) return;
        Color grise = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        bool dispo = EstDisponible();
        if (renduIngredient != null)
        {
            rendu.color = dispo ? Color.white : grise;
            renduIngredient.color = dispo ? couleurNormale : grise;
        }
        else
        {
            rendu.color = dispo ? couleurNormale : grise;
        }
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
        // C'est l'ingrédient (pas le bac) qui suit la souris
        SpriteRenderer source = renduIngredient != null ? renduIngredient : rendu;
        sr.sprite = source.sprite;
        sr.color = couleurNormale;
        sr.sortingOrder = 100;
        copieDrag.transform.localScale = source.transform.lossyScale * 1.1f;
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

        // KAN-36 : lâché sur la planche à découper
        PlancheADecouper planche = cible != null ? cible.GetComponentInParent<PlancheADecouper>() : null;
        if (planche != null)
        {
            if (planche.PeutRecevoir(ingredient) && StockManager.Instance.Retirer(ingredient))
                planche.Poser(ingredient);
            else
                StartCoroutine(Secouer());
            return;
        }

        Chaudron chaudron = cible != null ? cible.GetComponentInParent<Chaudron>() : null;
        if (chaudron == null) return; // lâché ailleurs : rien ne se passe

        // KAN-36 : à partir du niveau de découpe, certains ingrédients doivent d'abord être coupés
        if (PlancheADecouper.DoitEtreCoupe(ingredient))
        {
            PlancheADecouper.Instance.SignalerACouper(ingredient);
            StartCoroutine(Secouer());
            return;
        }

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
            case TypeIngredient.Secret: return Hex("#FAE4B1"); // Bougie
            case TypeIngredient.Magique: return Hex("#693287"); // Violet magique
            case TypeIngredient.Remede: return Hex("#597CB0"); // Bleu brume
            default: return Hex("#C88F60"); // Laiton
        }
    }

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out Color c);
        return c;
    }
}
