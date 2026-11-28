using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// KAN-33 : mini-jeu de mélange avant la cuisson.
// Quand on clique sur « Préparer » (ou Espace), il faut tourner la louche :
// maintenir le clic et faire des cercles autour du chaudron.
// Réussi → la cuisson démarre (barre de préparation du GameManager).
// Trop lent → le mélange déborde, on peut réessayer (ou le chaudron est vidé si « Vider Si Rate » est coché).
// L'interface est construite toute seule au lancement.
public class MiniJeuCuisson : MonoBehaviour
{
    [Header("Réglages")]
    [Tooltip("Nombre de tours de louche à faire autour du chaudron.")]
    [Range(1, 6)] public int toursRequis = 3;
    [Tooltip("Temps maximum pour faire les tours (en secondes). Pas de limite pendant la démo.")]
    [Min(2f)] public float tempsLimite = 8f;
    [Tooltip("Coché : en cas d'échec, le chaudron est vidé (ingrédients perdus). Décoché : on peut simplement réessayer.")]
    public bool viderSiRate = false;
    [Tooltip("Vide = trouvé automatiquement dans la scène.")]
    public Chaudron chaudron;

    public bool EnCours { get; private set; }

    // Palette de la DA
    static readonly Color Parchemin = Hex("#EBD6AB");
    static readonly Color Encre = Hex("#251C0D");
    static readonly Color Prune = Hex("#361C40");
    static readonly Color Magenta = Hex("#BF4794");
    static readonly Color Laiton = Hex("#C88F60");
    static readonly Color Bougie = Hex("#FAE4B1");

    Font police;
    Camera cam;
    GameObject racine;
    RectTransform zoneCanvas;
    RectTransform barreTours;
    RectTransform barreTemps;
    RectTransform curseur;
    Text titre;
    Text consigne;
    Text texteTemps;

    float angleTotal;
    float dernierAngle;
    bool angleConnu;
    float tempsRestant;
    bool termine;

    void Start()
    {
        police = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cam = Camera.main;
        if (chaudron == null) chaudron = FindFirstObjectByType<Chaudron>();
        Construire();
        racine.SetActive(false);
    }

    // Appelé par le bouton « Préparer » de la cuisine
    public void Demarrer()
    {
        GameManager gm = GameManager.Instance;
        if (EnCours || gm == null) return;
        if (!gm.PeutPreparer)
        {
            gm.LancerPreparation();   // le GameManager affiche pourquoi on ne peut pas préparer
            return;
        }

        EnCours = true;
        termine = false;
        angleTotal = 0f;
        angleConnu = false;
        tempsRestant = tempsLimite;
        titre.text = "Tourne la louche !";
        consigne.text = $"Maintiens le clic et fais {toursRequis} tours autour du chaudron";
        racine.SetActive(true);
        MettreAJourBarres(0f);
    }

    void Update()
    {
        if (!EnCours || termine) return;

        Mouse souris = Mouse.current;
        if (souris == null) return;
        Vector2 position = souris.position.ReadValue();
        bool appuye = souris.leftButton.isPressed;

        // La louche suit la souris quand on appuie
        curseur.gameObject.SetActive(appuye);
        if (appuye && RectTransformUtility.ScreenPointToLocalPointInRectangle(zoneCanvas, position, null, out Vector2 local))
            curseur.anchoredPosition = local;

        // On additionne l'angle parcouru autour du centre du chaudron
        if (appuye)
        {
            Vector2 centre = chaudron != null && cam != null
                ? (Vector2)cam.WorldToScreenPoint(chaudron.transform.position)
                : new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 d = position - centre;
            if (d.sqrMagnitude > 400f)   // trop près du centre, l'angle n'a pas de sens
            {
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                if (angleConnu) angleTotal += Mathf.DeltaAngle(dernierAngle, angle);
                dernierAngle = angle;
                angleConnu = true;
            }
        }
        else angleConnu = false;

        float progres = Mathf.Clamp01(Mathf.Abs(angleTotal) / (360f * toursRequis));

        bool sansLimite = GameManager.Instance != null && GameManager.Instance.DemoActive;
        if (!sansLimite) tempsRestant -= Time.deltaTime;
        texteTemps.text = sansLimite ? "Prends ton temps" : $"{Mathf.CeilToInt(Mathf.Max(0f, tempsRestant))} s";

        MettreAJourBarres(progres);

        if (progres >= 1f) StartCoroutine(Finir(true));
        else if (!sansLimite && tempsRestant <= 0f) StartCoroutine(Finir(false));
    }

    void MettreAJourBarres(float progres)
    {
        barreTours.localScale = new Vector3(progres, 1f, 1f);
        barreTemps.localScale = new Vector3(Mathf.Clamp01(tempsRestant / tempsLimite), 1f, 1f);
    }

    IEnumerator Finir(bool reussi)
    {
        termine = true;
        curseur.gameObject.SetActive(false);
        titre.text = reussi ? "Mélange réussi !" : "Trop lent... le mélange a débordé !";
        consigne.text = reussi ? "Ça cuit..." : (viderSiRate ? "Les ingrédients sont perdus." : "Réessaie : clique sur Préparer.");
        if (!reussi) Debug.Log("[Mini-jeu] Mélange raté (temps écoulé)");

        yield return new WaitForSeconds(reussi ? 0.6f : 1.5f);

        racine.SetActive(false);
        EnCours = false;

        if (reussi)
        {
            if (GameManager.Instance != null) GameManager.Instance.LancerPreparation();   // la cuisson
        }
        else if (viderSiRate && chaudron != null)
        {
            chaudron.Vider();
        }
    }

    // ---------- Interface ----------
    void Construire()
    {
        var canvasGO = new GameObject("Canvas_MiniJeu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;   // sous le grimoire (100)
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        racine = new GameObject("MiniJeu", typeof(RectTransform));
        racine.transform.SetParent(canvasGO.transform, false);
        zoneCanvas = (RectTransform)racine.transform;
        Etirer(zoneCanvas);
        Transform r = racine.transform;

        // Voile léger : empêche de cliquer ailleurs (bocaux, boutons) pendant le mélange
        var voile = CreerImage(r, "Voile", new Color(0f, 0f, 0f, 0.15f));
        Etirer((RectTransform)voile.transform);

        // Panneau en haut au centre
        var cadre = CreerImage(r, "Cadre", Prune);
        Placer(cadre.transform, new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(760, 190));
        var panneau = CreerImage(cadre.transform, "Panneau", Parchemin);
        Placer(panneau.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(740, 170));
        Transform p = panneau.transform;

        titre = Texte(p, 40, Encre, FontStyle.Bold);
        Placer(titre.transform, new Vector2(0.5f, 1f), new Vector2(0, -32), new Vector2(720, 50));
        consigne = Texte(p, 24, Encre, FontStyle.Normal);
        Placer(consigne.transform, new Vector2(0.5f, 1f), new Vector2(0, -72), new Vector2(720, 34));

        barreTours = Barre(p, new Vector2(-60, -112), Magenta);   // tours de louche
        barreTemps = Barre(p, new Vector2(-60, -146), Laiton);    // temps restant

        texteTemps = Texte(p, 22, Encre, FontStyle.Normal);
        Placer(texteTemps.transform, new Vector2(0.5f, 1f), new Vector2(285, -146), new Vector2(130, 30));
        var texteTours = Texte(p, 22, Encre, FontStyle.Normal);
        texteTours.text = "Tours";
        Placer(texteTours.transform, new Vector2(0.5f, 1f), new Vector2(285, -112), new Vector2(130, 30));

        // La « louche » qui suit la souris
        var louche = CreerImage(r, "Louche", Bougie);
        louche.raycastTarget = false;
        curseur = (RectTransform)louche.transform;
        curseur.anchorMin = curseur.anchorMax = new Vector2(0.5f, 0.5f);
        curseur.sizeDelta = new Vector2(26, 26);
        curseur.gameObject.SetActive(false);
    }

    RectTransform Barre(Transform parent, Vector2 position, Color couleur)
    {
        var fond = CreerImage(parent, "FondBarre", new Color(0.15f, 0.1f, 0.05f, 0.35f));
        Placer(fond.transform, new Vector2(0.5f, 1f), position, new Vector2(520, 22));
        var plein = CreerImage(fond.transform, "Barre", couleur);
        var rt = (RectTransform)plein.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    Image CreerImage(Transform parent, string nom, Color couleur)
    {
        var go = new GameObject(nom, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = couleur;
        return img;
    }

    Text Texte(Transform parent, int taille, Color couleur, FontStyle style)
    {
        var go = new GameObject("Texte", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = police;
        t.fontSize = taille;
        t.fontStyle = style;
        t.color = couleur;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void Placer(Transform t, Vector2 ancre, Vector2 position, Vector2 taille)
    {
        var r = (RectTransform)t;
        r.anchorMin = r.anchorMax = ancre;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = position;
        r.sizeDelta = taille;
    }

    static void Etirer(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out Color c);
        return c;
    }
}
