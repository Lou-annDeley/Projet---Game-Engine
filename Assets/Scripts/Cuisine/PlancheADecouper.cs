using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// KAN-36 : planche à découper.
// À partir de « Niveau Decoupe », les ingrédients de la liste doivent être coupés avant le chaudron :
// on les glisse sur la planche, on clique plusieurs fois (coups de couteau), puis on glisse
// les morceaux dans le chaudron. Avant ce niveau, la planche est cachée et rien ne change.
// Besoin dans la scène : EventSystem + Physics 2D Raycaster sur la caméra (déjà là en Cuisine).
[RequireComponent(typeof(BoxCollider2D))]
public class PlancheADecouper : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static PlancheADecouper Instance { get; private set; }

    [Header("Règles")]
    [Tooltip("Niveau à partir duquel ces ingrédients doivent être coupés avant le chaudron.")]
    [Range(1, 10)] public int niveauDecoupe = 3;
    [Tooltip("Les ingrédients à couper. Avant le niveau de découpe, ils vont directement dans le chaudron.")]
    public List<IngredientData> ingredientsADecouper = new List<IngredientData>();
    [Tooltip("Nombre de clics de couteau pour couper un ingrédient.")]
    [Range(1, 10)] public int coupsDeCouteau = 4;

    [Header("Visuels (optionnels : sinon formes provisoires)")]
    [Tooltip("Dessin de la planche (vue de dessus). Vide = rectangle en bois provisoire.")]
    public Sprite spritePlanche;
    [Tooltip("Dessin du couteau, lame vers la droite. Vide = couteau provisoire.")]
    public Sprite spriteCouteau;
    [Tooltip("Taille de la planche en unités.")]
    public Vector2 taillePlanche = new Vector2(2.6f, 1.6f);

    // Ce qui est sur la planche reste en mémoire quand on va au comptoir et qu'on revient
    static IngredientData surLaPlanche;
    static int coupsFaits;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reinitialiser() { surLaPlanche = null; coupsFaits = 0; }

    public static bool DecoupeActive =>
        Instance != null && Instance.isActiveAndEnabled && Progression.NiveauActuel >= Instance.niveauDecoupe;

    // Utilisé par les bacs : cet ingrédient doit-il passer par la planche ?
    public static bool DoitEtreCoupe(IngredientData ing) =>
        ing != null && DecoupeActive && Instance.ingredientsADecouper.Contains(ing);

    bool EstCoupe => surLaPlanche != null && coupsFaits >= coupsDeCouteau;

    // Palette de la DA
    static readonly Color BoisRouge = Hex("#704941");
    static readonly Color BoisSombre = Hex("#403031");
    static readonly Color Bougie = Hex("#FAE4B1");
    static readonly Color Laiton = Hex("#C88F60");

    BoxCollider2D zone;
    GameObject visuels;
    SpriteRenderer renduIngredient;
    readonly List<SpriteRenderer> morceaux = new List<SpriteRenderer>();
    Transform couteau;
    Vector3 couteauRepos;
    TextMesh texte;
    Camera cam;
    GameObject copieDrag;
    float messageJusqua;
    string message;

    static Sprite spriteBlanc;

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
        zone = GetComponent<BoxCollider2D>();
        zone.size = taillePlanche;
        ConstruireVisuels();
    }

    void Start()
    {
        Progression.NiveauChange += SurNiveauChange;
        MettreAJour();
    }

    void OnDestroy()
    {
        Progression.NiveauChange -= SurNiveauChange;
        if (Instance == this) Instance = null;
    }

    void SurNiveauChange(int niveau)
    {
        surLaPlanche = null;   // nouveau niveau : planche propre
        coupsFaits = 0;
        MettreAJour();
    }

    void Update()
    {
        if (messageJusqua > 0f && Time.time > messageJusqua)
        {
            messageJusqua = 0f;
            MettreAJour();
        }
    }

    // ---------- Appelé par les bacs (IngredientSlot) ----------

    // Vérifie si on peut poser cet ingrédient (et explique pourquoi sinon)
    public bool PeutRecevoir(IngredientData ing)
    {
        if (!DecoupeActive) return false;
        if (!ingredientsADecouper.Contains(ing))
        {
            Signaler("Pas besoin de couper : mets-le directement dans le chaudron.");
            return false;
        }
        if (surLaPlanche != null)
        {
            Signaler("La planche est déjà occupée !");
            return false;
        }
        return true;
    }

    public void Poser(IngredientData ing)
    {
        surLaPlanche = ing;
        coupsFaits = 0;
        MettreAJour();
        StartCoroutine(Rebond(visuels.transform));
    }

    // Quelqu'un a essayé de mettre un ingrédient entier dans le chaudron
    public void SignalerACouper(IngredientData ing)
    {
        Signaler($"Coupe d'abord : {ing.nom} !\nGlisse-le sur la planche.");
        StartCoroutine(Rebond(visuels.transform));
    }

    void Signaler(string texteMessage)
    {
        message = texteMessage;
        messageJusqua = Time.time + 2.5f;
        MettreAJour();
    }

    // ---------- Couper (clics) ----------
    public void OnPointerClick(PointerEventData e)
    {
        if (!DecoupeActive || surLaPlanche == null || EstCoupe) return;
        coupsFaits++;
        StopCoroutine(nameof(CoupDeCouteau));
        StartCoroutine(nameof(CoupDeCouteau));
        if (EstCoupe) Debug.Log($"[Planche] {surLaPlanche.nom} est coupé(e)");
        MettreAJour();
    }

    IEnumerator CoupDeCouteau()
    {
        // Le couteau descend sur l'ingrédient puis remonte
        Vector3 cible = new Vector3(0.1f, 0.05f, 0f);
        for (float t = 0f; t < 0.16f; t += Time.deltaTime)
        {
            float k = Mathf.Sin(t / 0.16f * Mathf.PI);
            couteau.localPosition = Vector3.Lerp(couteauRepos, cible, k);
            couteau.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(-20f, -70f, k));
            if (renduIngredient.enabled)
                renduIngredient.transform.localScale = Vector3.Scale(tailleIngredient, new Vector3(1f + 0.08f * k, 1f - 0.12f * k, 1f));
            yield return null;
        }
        couteau.localPosition = couteauRepos;
        couteau.localEulerAngles = new Vector3(0, 0, -20f);
        renduIngredient.transform.localScale = tailleIngredient;
    }

    // ---------- Glisser les morceaux vers le chaudron ----------
    public void OnBeginDrag(PointerEventData e)
    {
        if (!EstCoupe) { e.pointerDrag = null; return; }
        copieDrag = new GameObject("Drag_Morceaux");
        foreach (var m in morceaux)
        {
            var sr = new GameObject("Morceau").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(copieDrag.transform, false);
            sr.sprite = m.sprite;
            sr.color = m.color;
            sr.sortingOrder = 100;
            sr.transform.localPosition = m.transform.localPosition;
            sr.transform.localRotation = m.transform.localRotation;
            sr.transform.localScale = m.transform.localScale;
        }
        foreach (var m in morceaux) m.enabled = false;
        SuivreSouris(e);
    }

    public void OnDrag(PointerEventData e) { SuivreSouris(e); }

    public void OnEndDrag(PointerEventData e)
    {
        if (copieDrag == null) return;
        Destroy(copieDrag);

        GameObject cible = e.pointerCurrentRaycast.gameObject;
        Chaudron chaudron = cible != null ? cible.GetComponentInParent<Chaudron>() : null;
        if (chaudron != null && chaudron.PeutAjouter && chaudron.AjouterIngredient(surLaPlanche))
        {
            surLaPlanche = null;
            coupsFaits = 0;
        }
        else if (chaudron != null)
        {
            Signaler("Le chaudron est plein !");
        }
        MettreAJour();
    }

    void SuivreSouris(PointerEventData e)
    {
        if (copieDrag == null || cam == null) return;
        Vector3 p = cam.ScreenToWorldPoint(e.position);
        p.z = 0f;
        copieDrag.transform.position = p;
    }

    // ---------- Affichage ----------
    Vector3 tailleIngredient = Vector3.one;

    void MettreAJour()
    {
        bool active = DecoupeActive;
        visuels.SetActive(active);
        zone.enabled = active;
        if (!active) return;

        Sprite s = surLaPlanche != null ? SpriteDe(surLaPlanche) : null;
        Color c = surLaPlanche != null && surLaPlanche.sprite == null ? Laiton : Color.white;

        // Ingrédient entier (pas encore coupé)
        renduIngredient.enabled = surLaPlanche != null && !EstCoupe;
        if (renduIngredient.enabled)
        {
            renduIngredient.sprite = s;
            renduIngredient.color = c;
            float echelle = Ajuster(s, taillePlanche.y * 0.75f);
            tailleIngredient = Vector3.one * echelle;
            renduIngredient.transform.localScale = tailleIngredient;
        }

        // Morceaux (coupé)
        foreach (var m in morceaux)
        {
            m.enabled = EstCoupe;
            if (!EstCoupe) continue;
            m.sprite = s;
            m.color = c;
            m.transform.localScale = Vector3.one * Ajuster(s, taillePlanche.y * 0.38f);
        }

        // Texte sous la planche
        if (messageJusqua > 0f) texte.text = message;
        else if (surLaPlanche == null) texte.text = "Planche à découper";
        else if (!EstCoupe) texte.text = $"Clique pour couper : {coupsFaits} / {coupsDeCouteau}";
        else texte.text = "Coupé ! Glisse les morceaux\ndans le chaudron";
    }

    static float Ajuster(Sprite s, float hauteurVoulue)
    {
        if (s == null) return 1f;
        Vector2 t = s.bounds.size;
        return hauteurVoulue / Mathf.Max(t.x, t.y);
    }

    static Sprite SpriteDe(IngredientData ing) => ing.sprite != null ? ing.sprite : SpriteBlanc();

    void ConstruireVisuels()
    {
        visuels = new GameObject("Visuels");
        visuels.transform.SetParent(transform, false);
        Transform v = visuels.transform;

        // Planche (bordure + bois), ou le dessin de Lou-Ann
        if (spritePlanche != null)
        {
            var p = Rendu(v, "Planche", spritePlanche, Color.white, 1);
            Vector2 t = spritePlanche.bounds.size;
            p.transform.localScale = new Vector3(taillePlanche.x / t.x, taillePlanche.y / t.y, 1f);
        }
        else
        {
            var bord = Rendu(v, "Bordure", SpriteBlanc(), BoisSombre, 1);
            bord.transform.localScale = new Vector3(taillePlanche.x + 0.12f, taillePlanche.y + 0.12f, 1f);
            var bois = Rendu(v, "Bois", SpriteBlanc(), BoisRouge, 2);
            bois.transform.localScale = new Vector3(taillePlanche.x, taillePlanche.y, 1f);
        }

        // Ingrédient entier
        renduIngredient = Rendu(v, "Ingredient", null, Color.white, 3);
        renduIngredient.transform.localPosition = new Vector3(-0.2f, 0f, 0f);

        // 3 morceaux quand c'est coupé
        Vector3[] positions = { new Vector3(-0.6f, -0.15f, 0), new Vector3(-0.05f, 0.2f, 0), new Vector3(0.5f, -0.1f, 0) };
        float[] angles = { 15f, -10f, 30f };
        for (int i = 0; i < 3; i++)
        {
            var m = Rendu(v, "Morceau" + i, null, Color.white, 3);
            m.transform.localPosition = positions[i];
            m.transform.localEulerAngles = new Vector3(0, 0, angles[i]);
            m.enabled = false;
            morceaux.Add(m);
        }

        // Couteau (au repos en haut à droite de la planche)
        var couteauGO = new GameObject("Couteau");
        couteau = couteauGO.transform;
        couteau.SetParent(v, false);
        couteauRepos = new Vector3(taillePlanche.x * 0.32f, taillePlanche.y * 0.3f, 0f);
        couteau.localPosition = couteauRepos;
        couteau.localEulerAngles = new Vector3(0, 0, -20f);
        if (spriteCouteau != null)
        {
            var c = Rendu(couteau, "Dessin", spriteCouteau, Color.white, 5);
            c.transform.localScale = Vector3.one * Ajuster(spriteCouteau, 1.4f);
        }
        else
        {
            var lame = Rendu(couteau, "Lame", SpriteBlanc(), new Color(0.85f, 0.85f, 0.88f), 5);
            lame.transform.localScale = new Vector3(1.0f, 0.2f, 1f);
            lame.transform.localPosition = new Vector3(0.25f, 0f, 0f);
            var manche = Rendu(couteau, "Manche", SpriteBlanc(), BoisSombre, 5);
            manche.transform.localScale = new Vector3(0.5f, 0.24f, 1f);
            manche.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
        }

        // Texte sous la planche
        texte = ArticleBoutique.CreerTexte(v, new Vector3(0f, -taillePlanche.y / 2f - 0.12f, 0f), 0.03f, TextAnchor.UpperCenter);
        texte.color = Bougie;
    }

    static SpriteRenderer Rendu(Transform parent, string nom, Sprite sprite, Color couleur, int ordre)
    {
        var go = new GameObject(nom);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = couleur;
        sr.sortingOrder = ordre;
        return sr;
    }

    IEnumerator Rebond(Transform t)
    {
        for (float k = 0f; k < 0.2f; k += Time.deltaTime)
        {
            t.localScale = Vector3.one * (1f + Mathf.Sin(k / 0.2f * Mathf.PI) * 0.06f);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    static Sprite SpriteBlanc()
    {
        if (spriteBlanc == null)
            spriteBlanc = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return spriteBlanc;
    }

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out Color c);
        return c;
    }
}
