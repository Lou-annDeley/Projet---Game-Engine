using System.Collections.Generic;
using UnityEngine;

// Tous les niveaux dans l'ordre de la carte, et tous les clients du village.
[CreateAssetMenu(fileName = "liste_niveaux", menuName = "3Witches/Liste des niveaux", order = 4)]
public class ListeNiveaux : ScriptableObject
{
    public List<NiveauData> niveaux = new List<NiveauData>();
    public List<ClientData> clients = new List<ClientData>();

    public NiveauData Suivant(NiveauData niveau)
    {
        int i = niveaux.IndexOf(niveau);
        return i >= 0 && i + 1 < niveaux.Count ? niveaux[i + 1] : null;
    }
}
