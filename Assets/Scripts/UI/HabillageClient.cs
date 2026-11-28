using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Habillage du client au comptoir (sprites de Lou-Ann).
// Quand un client a son image, cache entièrement le personnage provisoire en formes simples
// (capuche + tête) et affiche l'image plus grande. Aucune scène ni code de Syrine à modifier :
// s'ajoute tout seul sur le ComptoirController au chargement de la scène.
public class HabillageClient : MonoBehaviour
{
    [Tooltip("Agrandissement de l'image du client (1 = taille d'origine du prototype).")]
    public float echelle = 1.2f;
    [Tooltip("Décalage vertical en plus (unités du canvas, positif = plus haut).")]
    public float decalageY = 40f;

    ComptoirController comptoir;
    Vector2 tailleOrigine, positionOrigine;
    bool init;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Installer()
    {
        SceneManager.sceneLoaded -= SurScene;
        SceneManager.sceneLoaded += SurScene;
        SurScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void SurScene(Scene scene, LoadSceneMode mode)
    {
        foreach (var c in Object.FindObjectsByType<ComptoirController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.GetComponent<HabillageClient>() == null) c.gameObject.AddComponent<HabillageClient>();
    }

    void Awake() { comptoir = GetComponent<ComptoirController>(); }

    void LateUpdate()
    {
        if (comptoir == null || comptoir.spriteClient == null) return;
        var rt = comptoir.spriteClient.rectTransform;
        if (!init)
        {
            tailleOrigine = rt.sizeDelta;
            positionOrigine = rt.anchoredPosition;
            init = true;
        }

        bool aUneImage = comptoir.spriteClient.gameObject.activeSelf;

        // Personnage provisoire caché dès qu'une vraie image existe
        if (comptoir.capuche != null) comptoir.capuche.gameObject.SetActive(!aUneImage);
        if (comptoir.tete != null) comptoir.tete.gameObject.SetActive(!aUneImage);

        // Image plus grande, agrandie vers le haut (le bas reste caché par le comptoir)
        rt.sizeDelta = tailleOrigine * echelle;
        float bas = positionOrigine.y - tailleOrigine.y / 2f;
        rt.anchoredPosition = new Vector2(positionOrigine.x, bas + rt.sizeDelta.y / 2f + decalageY);
    }
}
