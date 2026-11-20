using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Un ingrédient à vendre dans la Boutique : un clic = acheter 1 unité.
// Besoin dans la scène : un EventSystem + un Physics 2D Raycaster sur la caméra.
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class ArticleBoutique : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public IngredientData ingredient;

    SpriteRenderer rendu;
    TextMesh texte;
    Color couleurNormale = Color.white;
    Vector3 tailleDeBase;
    bool abonne;

    static Sprite spriteParDefaut;

    void Awake()
    {
        rendu = GetComponent<SpriteRenderer>();
        tailleDeBase = transform.localScale;
    }

    public void Initialiser(IngredientData data)
    {
        ingredient = data;
        tailleDeBase = transform.localScale;

        bool aUnSprite = ingredient.sprite != null;
        rendu.sprite = aUnSprite ? ingredient.sprite : SpriteParDefaut();
        couleurNormale = aUnSprite ? Color.white : CouleurDuType(ingredient.type);
        var col = GetComponent<BoxCollider2D>();
        col.size = rendu.sprite.bounds.size;
        col.offset = rendu.sprite.bounds.center;

        // Texte sous l'article : nom, prix, stock
        texte = CreerTexte(transform, new Vector3(0f, -rendu.sprite.bounds.extents.y - 0.1f, 0f), 0.032f, TextAnchor.UpperCenter);
        MettreAJour();
    }

    void Start()
    {
        if (StockManager.Instance != null) StockManager.Instance.StockChange += SurStockChange;
        if (ArgentManager.Instance != null) ArgentManager.Instance.ArgentChange += SurArgentChange;
        abonne = true;
        MettreAJour();
    }

    void OnDestroy()
    {
        if (!abonne) return;
        if (StockManager.Instance != null) StockManager.Instance.StockChange -= SurStockChange;
        if (ArgentManager.Instance != null) ArgentManager.Instance.ArgentChange -= SurArgentChange;
    }

    void SurStockChange(IngredientData ing, int quantite) { if (ing == ingredient) MettreAJour(); }
    void SurArgentChange(int pieces) { MettreAJour(); }

    bool PeutAcheter()
    {
        return ArgentManager.Instance != null && ArgentManager.Instance.PeutPayer(ingredient.prixAchat);
    }

    void MettreAJour()
    {
        if (ingredient == null || texte == null) return;
        int stock = StockManager.Instance != null ? StockManager.Instance.Quantite(ingredient) : 0;
        texte.text = $"{CouperNom(ingredient.nom)}\n{ingredient.prixAchat} pièces\nStock : {stock}";
        // Grisé si on n'a pas assez d'argent
        rendu.color = PeutAcheter() ? couleurNormale : new Color(0.35f, 0.35f, 0.35f, 0.6f);
    }

    // ---------- Clic = achat ----------
    public void OnPointerClick(PointerEventData e)
    {
        if (ingredient == null || StockManager.Instance == null || ArgentManager.Instance == null) return;

        if (ArgentManager.Instance.Depenser(ingredient.prixAchat))
        {
            StockManager.Instance.Ajouter(ingredient);
            Debug.Log($"[Boutique] +1 {ingredient.nom} (stock {StockManager.Instance.Quantite(ingredient)}) - il reste {ArgentManager.Instance.Pieces} pièces");
            StopAllCoroutines();
            StartCoroutine(Rebond());
        }
        else
        {
            Debug.Log($"[Boutique] Pas assez de pièces pour {ingredient.nom} ({ingredient.prixAchat} pièces, tu en as {ArgentManager.Instance.Pieces})");
            StopAllCoroutines();
            StartCoroutine(Secouer());
        }
    }

    public void OnPointerEnter(PointerEventData e) { transform.localScale = tailleDeBase * 1.1f; }
    public void OnPointerExit(PointerEventData e) { transform.localScale = tailleDeBase; }

    IEnumerator Rebond()
    {
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            transform.localScale = tailleDeBase * (1f + Mathf.Sin(t / 0.2f * Mathf.PI) * 0.2f);
            yield return null;
        }
        transform.localScale = tailleDeBase;
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

    // ---------- Outils ----------
    // Crée un texte simple (en attendant la vraie UI de Lou-Ann)
    public static TextMesh CreerTexte(Transform parent, Vector3 positionLocale, float taille, TextAnchor ancre)
    {
        var go = new GameObject("Texte");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = positionLocale;
        var tm = go.AddComponent<TextMesh>();
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.fontSize = 100; // grand + petite échelle = texte net
        tm.characterSize = taille;
        tm.anchor = ancre;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = tm.font.material;
        mr.sortingOrder = 10;
        return tm;
    }

    // Coupe les noms trop longs en deux lignes (au niveau d'un espace près du milieu)
    static string CouperNom(string nom, int max = 14)
    {
        if (string.IsNullOrEmpty(nom) || nom.Length <= max) return nom;
        int milieu = nom.Length / 2;
        int gauche = nom.LastIndexOf(' ', milieu);
        int droite = nom.IndexOf(' ', milieu);
        int coupe;
        if (gauche < 0) coupe = droite;
        else if (droite < 0) coupe = gauche;
        else coupe = (milieu - gauche <= droite - milieu) ? gauche : droite;
        if (coupe < 0) return nom;
        return nom.Substring(0, coupe) + "\n" + nom.Substring(coupe + 1);
    }

    static Sprite SpriteParDefaut()
    {
        if (spriteParDefaut == null)
            spriteParDefaut = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return spriteParDefaut;
    }

    static Color CouleurDuType(TypeIngredient type)
    {
        switch (type)
        {
            case TypeIngredient.Toxique: return Hex("#6BA027");
            case TypeIngredient.Secret: return Hex("#FAE4B1");
            case TypeIngredient.Magique: return Hex("#693287");
            case TypeIngredient.Remede: return Hex("#597CB0");
            default: return Hex("#C88F60");
        }
    }

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out Color c);
        return c;
    }
}
