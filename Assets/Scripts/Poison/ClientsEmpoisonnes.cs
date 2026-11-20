using System;
using System.Collections.Generic;
using UnityEngine;

public enum ResultatRemede { Gueri, PlusMalade, Mort }

// Le dossier médical d'un client empoisonné
[Serializable]
public class DossierEmpoisonne
{
    public string clientId;
    public string nomClient;
    public RemedeData remede;   // le remède qu'il demandera en revenant
    public int gravite = 1;     // 1 = malade, 2 = très malade...
}

// Garde la liste des clients empoisonnés, le remède qu'ils vont demander,
// et décide du résultat quand on leur sert un remède.
// À mettre sur le même objet que le StockManager.
//
// Pour Syrine (clients) :
//  - client servi avec un plat empoisonné : ClientsEmpoisonnes.Instance.Empoisonner(id, nom, ingredientToxique)
//  - client qui revient : if (ClientsEmpoisonnes.Instance.EstMalade(id, out var dossier)) → il affiche dossier.remede.anomalie
//    et commande dossier.remede au lieu d'une recette
//  - remède servi : var resultat = ClientsEmpoisonnes.Instance.Soigner(id, chaudron.Contenu)
//  - événement ClientMort → faire apparaître le fantôme (KAN-57)
public class ClientsEmpoisonnes : MonoBehaviour
{
    public static ClientsEmpoisonnes Instance { get; private set; }

    [Tooltip("Les 4 remèdes (Assets/Data/Remedes). Rempli automatiquement par le menu 3Witches > Créer les remèdes.")]
    public List<RemedeData> remedes = new List<RemedeData>();

    [Tooltip("Gravité à laquelle le client meurt (1 = juste empoisonné). Avec 3 : il meurt au 2e remède raté.")]
    public int graviteMortelle = 3;

    [Header("Suivi pendant le jeu (ne pas remplir)")]
    public List<DossierEmpoisonne> malades = new List<DossierEmpoisonne>();
    [Tooltip("Total de clients empoisonnés : sert pour les deux fins (KAN-55).")]
    public int totalEmpoisonnes;
    public int totalMorts;

    public event Action<DossierEmpoisonne> ClientEmpoisonne;
    public event Action<DossierEmpoisonne, ResultatRemede> RemedeServi;
    public event Action<DossierEmpoisonne> ClientMort; // → fantôme

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public RemedeData RemedePour(IngredientData poison)
    {
        return remedes.Find(r => r != null && r.causePoison == poison);
    }

    public DossierEmpoisonne Trouver(string clientId)
    {
        return malades.Find(d => d.clientId == clientId);
    }

    public bool EstMalade(string clientId, out DossierEmpoisonne dossier)
    {
        dossier = Trouver(clientId);
        return dossier != null;
    }

    // À appeler quand un client reçoit un plat empoisonné
    public DossierEmpoisonne Empoisonner(string clientId, string nomClient, IngredientData poison)
    {
        RemedeData remede = RemedePour(poison);
        if (remede == null)
        {
            Debug.LogWarning($"[Empoisonnés] Aucun remède ne soigne « {(poison != null ? poison.nom : "?")} ». Vérifie la liste des remèdes.");
            return null;
        }

        totalEmpoisonnes++;
        DossierEmpoisonne dossier = Trouver(clientId);
        if (dossier == null)
        {
            dossier = new DossierEmpoisonne { clientId = clientId, nomClient = nomClient, remede = remede, gravite = 1 };
            malades.Add(dossier);
        }
        else
        {
            dossier.gravite++;        // déjà malade et empoisonné à nouveau
            dossier.remede = remede;
        }

        Debug.Log($"[Empoisonnés] {nomClient} est empoisonné(e) ({poison.nom}). Il/elle reviendra avec : {remede.anomalie}, " +
                  $"et demandera « {remede.nom} » = {remede.ListeIngredients()}");
        ClientEmpoisonne?.Invoke(dossier);

        if (dossier.gravite >= graviteMortelle) Mourir(dossier);
        return dossier;
    }

    // À appeler quand on sert un remède à un client malade
    public ResultatRemede Soigner(string clientId, IReadOnlyList<IngredientData> contenu)
    {
        DossierEmpoisonne dossier = Trouver(clientId);
        if (dossier == null)
        {
            Debug.LogWarning($"[Empoisonnés] Le client « {clientId} » n'est pas malade.");
            return ResultatRemede.Gueri;
        }

        ResultatRemede resultat;
        if (dossier.remede.EstBienPrepare(contenu))
        {
            resultat = ResultatRemede.Gueri;
            malades.Remove(dossier);
            Debug.Log($"[Empoisonnés] {dossier.nomClient} est GUÉRI(E) et repart content(e) !");
        }
        else if (dossier.remede.siRate == SiRemedeRate.Meurt || dossier.gravite + 1 >= graviteMortelle)
        {
            resultat = ResultatRemede.Mort;
            Mourir(dossier);
        }
        else
        {
            resultat = ResultatRemede.PlusMalade;
            dossier.gravite++;
            Debug.Log($"[Empoisonnés] Remède raté : {dossier.nomClient} est ENCORE PLUS MALADE (gravité {dossier.gravite}). Il/elle reviendra.");
        }

        RemedeServi?.Invoke(dossier, resultat);
        return resultat;
    }

    void Mourir(DossierEmpoisonne dossier)
    {
        malades.Remove(dossier);
        totalMorts++;
        Debug.Log($"[Empoisonnés] {dossier.nomClient} est MORT(E)... son fantôme va hanter la boutique.");
        ClientMort?.Invoke(dossier);
    }
}
