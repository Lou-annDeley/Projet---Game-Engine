using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Le grimoire consultable en cuisine : un ruban « Grimoire » ouvre un livre
// avec 3 onglets (Soupes, Potions, Remèdes). L'interface est construite toute seule
// au lancement (couleurs de la DA). Les recettes pas encore débloquées sont en « ??? ».
// Besoin dans la scène : un EventSystem (déjà présent dans Cuisine).
public class Grimoire : MonoBehaviour
{
    [Tooltip("Rempli automatiquement par le menu 3Witches > Créer les recettes de l'Excel.")]
    public List<RecetteData> recettes = new List<RecetteData>();
    [Tooltip("Rempli automatiquement par le menu 3Witches > Créer les recettes de l'Excel.")]
    public List<RemedeData> remedes = new List<RemedeData>();
    [Tooltip("Afficher les recettes pas encore débloquées en « ??? (niveau X) ».")]
    public bool montrerVerrouillees = true;

    enum Onglet { Soupes, Potions, Remedes }

    // Palette de la DA
    static readonly Color Parchemin = Hex("#EBD6AB");
    static readonly Color ParcheminFonce = Hex("#D9BF8C");
    static readonly Color Encre = Hex("#251C0D");
    static readonly Color Lavande = Hex("#9B7DA0");
    static readonly Color Laiton = Hex("#C88F60");
    static readonly Color Magenta = Hex("#BF4794");
    static readonly Color Violet = Hex("#693287");
    static readonly Color BleuBrume = Hex("#597CB0");
    static readonly Color Prune = Hex("#361C40");

    Font police;
    GameObject livre;
    RectTransform liste;
    Text titrePage;
    Text detail;
    Onglet ongletActuel = Onglet.Soupes;
    readonly List<GameObject> entrees = new List<GameObject>();

    void Start()
    {
        police = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Construire();
        livre.SetActive(false);
    }

    public void Ouvrir() { livre.SetActive(true); AfficherOnglet(ongletActuel); }
    public void Fermer() { livre.SetActive(false); }
    public void Basculer() { if (livre.activeSelf) Fermer(); else Ouvrir(); }

    // ---------- Construction de l'interface ----------
    void Construire()
    {
        var canvasGO = new GameObject("Canvas_Grimoire", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Transform c = canvasGO.transform;

        // Ruban « Grimoire » en bas à droite (toujours visible)
        var ruban = CreerBouton(c, "Grimoire", Lavande, Laiton, 40, Basculer);
        Placer(ruban.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 40), new Vector2(280, 90));

        // Le livre (caché au départ)
        livre = new GameObject("Livre", typeof(RectTransform));
        livre.transform.SetParent(c, false);
        Etirer(livre.transform, 0);
        Transform l = livre.transform;

        var voile = CreerImage(l, "Voile", new Color(0f, 0f, 0f, 0.6f)); // assombrit et bloque les clics derrière
        Etirer(voile.transform, 0);

        var couverture = CreerImage(l, "Couverture", Prune);
        Placer(couverture.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1540, 860));
        var pages = CreerImage(l, "Pages", Parchemin);
        Placer(pages.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1500, 820));
        var reliure = CreerImage(l, "Reliure", Prune);
        Placer(reliure.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(8, 820));

        // Onglets en tissu au-dessus du livre
        string[] noms = { "Soupes", "Potions", "Remèdes" };
        Color[] couleurs = { Magenta, Violet, BleuBrume };
        for (int i = 0; i < 3; i++)
        {
            Onglet o = (Onglet)i;
            var b = CreerBouton(l, noms[i], couleurs[i], Parchemin, 34, () => AfficherOnglet(o));
            Placer(b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(-520 + i * 260, 400), new Vector2(240, 70));
        }

        // Bouton fermer
        var fermer = CreerBouton(l, "X", Prune, Parchemin, 36, Fermer);
        Placer(fermer.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(705, 335), new Vector2(60, 60));

        // Page de gauche : titre + liste
        titrePage = CreerTexte(l, "", 48, Encre, TextAnchor.MiddleCenter);
        titrePage.fontStyle = FontStyle.Bold;
        Placer(titrePage.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-375, 310), new Vector2(620, 70));

        var listeGO = new GameObject("Liste", typeof(RectTransform));
        listeGO.transform.SetParent(l, false);
        liste = (RectTransform)listeGO.transform;
        Placer(liste, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(-375, 250), new Vector2(600, 600));

        // Page de droite : détail de la recette
        detail = CreerTexte(l, "", 32, Encre, TextAnchor.UpperLeft);
        Placer(detail.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(375, -40), new Vector2(620, 700));
    }

    // ---------- Contenu ----------
    void AfficherOnglet(Onglet onglet)
    {
        ongletActuel = onglet;
        foreach (var e in entrees) Destroy(e);
        entrees.Clear();

        titrePage.text = onglet == Onglet.Soupes ? "Soupes" : onglet == Onglet.Potions ? "Potions" : "Remèdes";

        var elements = new List<(string nom, int niveau, Func<string> texte)>();
        if (onglet == Onglet.Remedes)
        {
            foreach (var r in remedes)
                if (r != null) { var rr = r; elements.Add((rr.nom, rr.niveauDeblocage, () => DetailRemede(rr))); }
        }
        else
        {
            var cat = onglet == Onglet.Soupes ? CategorieRecette.Soupe : CategorieRecette.Potion;
            foreach (var r in recettes)
                if (r != null && r.categorie == cat) { var rr = r; elements.Add((rr.nom, rr.niveauDeblocage, () => DetailRecette(rr))); }
        }

        int place = 0;
        foreach (var el in elements)
        {
            bool debloque = el.niveau <= Progression.NiveauActuel;
            if (!debloque && !montrerVerrouillees) continue;

            string label = debloque ? el.nom : $"???   (niveau {el.niveau})";
            Func<string> texte = el.texte;
            var b = CreerBouton(liste, label, ParcheminFonce, Encre, 32, () => detail.text = texte(), TextAnchor.MiddleLeft);
            b.interactable = debloque;
            Placer(b.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -place * 75), new Vector2(600, 62));
            entrees.Add(b.gameObject);
            place++;
        }

        detail.text = place == 0 ? "Rien ici pour l'instant." : "<i>Choisis une recette dans la page de gauche.</i>";
    }

    string DetailRecette(RecetteData r)
    {
        return $"<b><size=46>{r.nom}</size></b>\n<i>{(r.categorie == CategorieRecette.Soupe ? "Soupe" : "Potion")}</i>\n\n" +
               $"<b>Ingrédients :</b>\n{Puces(r.ingredients)}\n\n" +
               $"Temps de préparation : {r.tempsPreparation} s\nPrix de vente : {r.prixVente} pièces";
    }

    string DetailRemede(RemedeData r)
    {
        string cause = r.causePoison != null ? $" <i>(causé par : {r.causePoison.nom})</i>" : "";
        string avertissement = r.siRate == SiRemedeRate.Meurt
            ? "Attention : s'il est raté, le client n'y survivra pas..."
            : "S'il est raté, le client sera encore plus malade.";
        return $"<b><size=46>{r.nom}</size></b>\n<i>Remède</i>\n\n" +
               $"<b>Soigne :</b> {r.anomalie}{cause}\n\n" +
               $"<b>Ingrédients :</b>\n{Puces(r.ingredients)}\n\n" +
               $"Temps de préparation : {r.tempsPreparation} s\nPrix de vente : {r.prixVente} pièces\n\n<i>{avertissement}</i>";
    }

    static string Puces(List<IngredientData> ingredients)
    {
        return string.Join("\n", ingredients.ConvertAll(i => "   -  " + (i != null ? i.nom : "?")));
    }

    // ---------- Outils UI ----------
    Image CreerImage(Transform parent, string nom, Color couleur)
    {
        var go = new GameObject(nom, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = couleur;
        return img;
    }

    Text CreerTexte(Transform parent, string contenu, int taille, Color couleur, TextAnchor ancre)
    {
        var go = new GameObject("Texte", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = police;
        t.fontSize = taille;
        t.color = couleur;
        t.alignment = ancre;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.text = contenu;
        return t;
    }

    Button CreerBouton(Transform parent, string label, Color fond, Color couleurTexte, int taille, Action action, TextAnchor ancre = TextAnchor.MiddleCenter)
    {
        var img = CreerImage(parent, "Bouton_" + label, fond);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        if (action != null) b.onClick.AddListener(() => action());
        var t = CreerTexte(img.transform, label, taille, couleurTexte, ancre);
        Etirer(t.transform, ancre == TextAnchor.MiddleLeft ? 20 : 0);
        return b;
    }

    static void Placer(Transform t, Vector2 ancre, Vector2 pivot, Vector2 position, Vector2 taille)
    {
        var r = (RectTransform)t;
        r.anchorMin = ancre;
        r.anchorMax = ancre;
        r.pivot = pivot;
        r.anchoredPosition = position;
        r.sizeDelta = taille;
    }

    static void Etirer(Transform t, float marge)
    {
        var r = (RectTransform)t;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(marge, 0);
        r.offsetMax = new Vector2(-marge, 0);
    }

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out Color c);
        return c;
    }
}
