using UnityEngine;
using UnityEngine.InputSystem;

// Touches de test pour aller vite pendant le développement (idée d'Ali sur KAN-27).
// Elles ne marchent que dans l'éditeur Unity, pas dans le jeu final.
//   E : faire entrer un client tout de suite
//   A : mettre dans le bol la préparation parfaite pour le client sélectionné
//   T : finir la journée (plus aucun client n'entre)
public class DebugTouches : MonoBehaviour
{
#if UNITY_EDITOR
    void Update()
    {
        Keyboard clavier = Keyboard.current;
        GameManager gm = GameManager.Instance;
        if (clavier == null || gm == null || !gm.NiveauEnCours) return;

        if (clavier.eKey.wasPressedThisFrame) gm.TestNouveauClient();
        if (clavier.aKey.wasPressedThisFrame) gm.TestPreparationParfaite();
        if (clavier.tKey.wasPressedThisFrame) gm.TestFinDuTemps();
    }
#endif
}
