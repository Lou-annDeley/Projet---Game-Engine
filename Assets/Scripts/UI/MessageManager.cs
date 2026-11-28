using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Message du manager : au début du niveau où l'option empoisonner se débloque,
// une lettre du manager propose de payer double chaque potion empoisonnée.
// Le jeu est en pause tant que la lettre est ouverte. À mettre dans la scène Comptoir.
// Le prix double lui-même est appliqué par le GameManager (réglage dans reglages_poison).
public class MessageManager : MonoBehaviour
{
    [Header("Texte de la lettre")]
    public string titre = "Une lettre du manager";
    [TextArea(6, 12)]
    public string message =
        "Ma chère sorcière,\n\n" +
        "Les affaires vont mal, et certains clients me doivent beaucoup trop d'argent...\n\n" +
        "Si, par mégarde, une de tes potions était « assaisonnée » avec un de ces ingrédients verts, " +
        "je te la paierais le double. Personne n'en saura rien.\n\n" +
        "C'est toi qui vois.";
    public string signature = "— Le Manager";
    public string texteBouton = "Compris...";

    // Une seule fois par lancement du jeu pour chaque niveau
    static readonly HashSet<int> dejaVus = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reinitialiser() { dejaVus.Clear(); }

    // Palette de la DA
    static readonly Color Parchemin = Hex("#EBD6AB");
    static readonly Color Encre = Hex("#251C0D");
    static readonly Color Prune = Hex("#361C40");
    static readonly Color VertPoison = Hex("#6BA027");

    Font police;
    GameObject racine;
    float ancienTimeScale = 1f;

    IEnumerator Start()
    {
        police = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Construire();
        racine.SetActive(false);

        yield return null;   // on laisse le GameManager lancer le niveau

        bool demo = GameManager.Instance != null && GameManager.Instance.DemoActive;
        int niveau = Progression.NiveauActuel;
        if (!demo && niveau == PoisonManager.NiveauDeblocage && dejaVus.Add(niveau))
            Afficher();
    }

    public void Afficher()
    {
        if (racine.activeSelf) return;
        racine.SetActive(true);
        ancienTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;   // pause : le temps et la patience des clients s'arrêtent
    }

    public void Fermer()
    {
        racine.SetActive(false);
        Time.timeScale = ancienTimeScale;
    }

    void OnDestroy()
    {
        if (racine != null && racine.activeSelf) Time.timeScale = 1f;
    }

    [ContextMenu("Test : afficher le message")]
    void TestAfficher() { Afficher(); }

    // ---------- Interface ----------
    void Construire()
    {
        var canvasGO = new GameObject("Canvas_MessageManager", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        racine = new GameObject("Lettre", typeof(RectTransform));
        racine.transform.SetParent(canvasGO.transform, false);
        Etirer((RectTransform)racine.transform);
        Transform r = racine.transform;

        var voile = CreerImage(r, "Voile", new Color(0f, 0f, 0f, 0.65f));
        Etirer((RectTransform)voile.transform);

        var cadre = CreerImage(r, "Cadre", Prune);
        Placer(cadre.transform, Vector2.zero, new Vector2(940, 640));
        var papier = CreerImage(cadre.transform, "Papier", Parchemin);
        Placer(papier.transform, Vector2.zero, new Vector2(910, 610));
        Transform p = papier.transform;

        var t = CreerTexte(p, titre, 44, Prune, FontStyle.Bold, TextAnchor.MiddleCenter);
        Placer(t.transform, new Vector2(0, 250), new Vector2(820, 60));

        var m = CreerTexte(p, message, 28, Encre, FontStyle.Italic, TextAnchor.UpperLeft);
        Placer(m.transform, new Vector2(0, 10), new Vector2(800, 380));

        var s = CreerTexte(p, signature, 30, VertPoison, FontStyle.Bold, TextAnchor.MiddleRight);
        Placer(s.transform, new Vector2(0, -200), new Vector2(800, 44));

        var bouton = CreerImage(p, "Bouton", Prune);
        Placer(bouton.transform, new Vector2(0, -262), new Vector2(260, 64));
        var b = bouton.gameObject.AddComponent<Button>();
        b.targetGraphic = bouton;
        b.onClick.AddListener(Fermer);
        var tb = CreerTexte(bouton.transform, texteBouton, 30, Parchemin, FontStyle.Bold, TextAnchor.MiddleCenter);
        Etirer((RectTransform)tb.transform);
    }

    Image CreerImage(Transform parent, string nom, Color couleur)
    {
        var go = new GameObject(nom, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = couleur;
        return img;
    }

    Text CreerTexte(Transform parent, string contenu, int taille, Color couleur, FontStyle style, TextAnchor ancre)
    {
        var go = new GameObject("Texte", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = police;
        t.text = contenu;
        t.fontSize = taille;
        t.fontStyle = style;
        t.color = couleur;
        t.alignment = ancre;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void Placer(Transform t, Vector2 position, Vector2 taille)
    {
        var r = (RectTransform)t;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
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
