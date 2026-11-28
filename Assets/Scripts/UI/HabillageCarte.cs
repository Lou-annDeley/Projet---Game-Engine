using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Habillage de la carte des niveaux (KAN-71).
// Au chargement de la scène MenuCarte, remplace les formes provisoires de la carte de Syrine
// par l'illustration et les médaillons de Lou-Ann. Aucune scène ni code de Syrine à modifier :
// le script de Syrine (CarteNiveaux) continue de colorer les médaillons selon l'état du niveau.
// Images dans Assets/Resources/Carte/ : carte_fond, medaillon, etoile, chiffre_1 à chiffre_8.
public static class HabillageCarte
{
    const string NomScene = "MenuCarte";
    const string NomCanvas = "Proto_Canvas";

    // Les 8 clairières du chemin, en pixels depuis le centre de la carte (960 x 1080)
    static readonly Vector2[] positions =
    {
        new Vector2(-166, -478), new Vector2(189, -364), new Vector2(-177, -252), new Vector2(195, -127),
        new Vector2(-166, -10), new Vector2(200, 108), new Vector2(-170, 228), new Vector2(200, 352)
    };
    // Cacher les étiquettes de nom à côté des niveaux (le nom s'affiche déjà dans le panneau de droite)
    const bool cacherEtiquettes = true;

    static readonly Vector2 tailleMedaillon = new Vector2(150, 94);
    const float hauteurChiffre = 34f;
    static readonly Vector2 decalageCentre = new Vector2(-6, 17);   // la face du médaillon est au-dessus de son épaisseur
    const float tailleEtoile = 24f;

    // Formes provisoires du prototype, cachées une fois la carte illustrée
    static readonly string[] aCacher = { "Ciel_", "Lune", "Halo_Lune", "Tombe", "Citrouille", "Tige", "Chemin" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Installer()
    {
        SceneManager.sceneLoaded -= SurScene;
        SceneManager.sceneLoaded += SurScene;
        SurScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void SurScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != NomScene) return;
        GameObject canvas = GameObject.Find(NomCanvas);
        Transform carte = canvas != null ? canvas.transform.Find("Carte") : null;
        if (carte == null) { Debug.LogWarning("[Carte] Objet Proto_Canvas/Carte introuvable."); return; }

        Sprite fond = Charger("carte_fond"), medaillon = Charger("medaillon"),
               etoile = Charger("etoile");

        // 1. Cacher les formes provisoires
        foreach (Transform enfant in carte)
            foreach (string debut in aCacher)
                if (enfant.name.StartsWith(debut)) { enfant.gameObject.SetActive(false); break; }

        // 2. Illustration en fond de carte
        if (fond != null && carte.Find("Illustration") == null)
        {
            var go = new GameObject("Illustration", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(carte, false);
            go.transform.SetAsFirstSibling();
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(960, 1080);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = fond;
            img.raycastTarget = false;
        }

        // 3. Médaillons des niveaux, posés sur les clairières
        for (int i = 0; i < positions.Length; i++)
        {
            Transform noeud = carte.Find("Niveau_" + (i + 1));
            if (noeud == null) continue;

            var rt = (RectTransform)noeud;
            rt.anchoredPosition = positions[i];
            rt.sizeDelta = tailleMedaillon;
            var anneau = noeud.GetComponent<Image>();
            if (anneau != null && medaillon != null) { anneau.sprite = medaillon; anneau.type = Image.Type.Simple; }

            // Le centre coloré de Syrine est masqué : on voit la pierre du médaillon
            Transform f = noeud.Find("Fond");
            if (f != null) { var img = f.GetComponent<Image>(); if (img != null) img.enabled = false; }

            // Chiffre gravé dans la pierre (l'image prend la couleur que Syrine donne au texte)
            Transform numero = noeud.Find("Numero");
            Sprite chiffre = Charger("chiffre_" + (i + 1));
            if (numero != null && chiffre != null && noeud.Find("ChiffreGrave") == null)
            {
                var texte = numero.GetComponent<Text>();
                if (texte != null) texte.enabled = false;

                var go = new GameObject("ChiffreGrave", typeof(RectTransform), typeof(Image), typeof(CouleurDuTexte));
                go.transform.SetParent(noeud, false);
                var rc = (RectTransform)go.transform;
                rc.anchoredPosition = decalageCentre;
                rc.sizeDelta = new Vector2(hauteurChiffre * chiffre.rect.width / chiffre.rect.height, hauteurChiffre);
                var img = go.GetComponent<Image>();
                img.sprite = chiffre;
                img.raycastTarget = false;
                go.GetComponent<CouleurDuTexte>().source = texte;
            }

            if (cacherEtiquettes)
            {
                Transform etiquette = noeud.Find("Etiquette");
                if (etiquette != null) etiquette.gameObject.SetActive(false);
            }

            for (int e = 1; e <= 3; e++)
            {
                Transform et = noeud.Find("Etoile_" + e);
                if (et == null) continue;
                var re = (RectTransform)et;
                re.sizeDelta = new Vector2(tailleEtoile, tailleEtoile);
                re.anchoredPosition = new Vector2((e - 2) * tailleEtoile * 1.2f, 66f);
                var img = et.GetComponent<Image>();
                if (img != null && etoile != null) img.sprite = etoile;
            }
        }
    }

    static Sprite Charger(string nom)
    {
        var s = Resources.Load<Sprite>("Carte/" + nom);
        if (s == null) Debug.LogWarning("[Carte] Image introuvable : Assets/Resources/Carte/" + nom + ".png");
        return s;
    }
}

