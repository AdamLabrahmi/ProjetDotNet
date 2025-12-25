namespace ProjetDotNet.Models.Enums
{
    //public enum RoleProjet
    //{
    //    CHEF,
    //    MEMBRE
    //}

    public enum RoleProjet
    {
        Administrateur, // Gère les paramètres du projet, workflows, composants
        Chef,           // Chef de projet / Project Manager
        Developpeur,    // Crée, modifie et résout les tickets
        Testeur,        // Vérifie et valide la qualité des tickets
        ProductOwner,   // Gère le backlog et priorise les tâches
        ScrumMaster,    // Suit les sprints et facilite les réunions
        Observateur,    // Peut consulter les tickets mais pas les modifier
        Contributeur    // Peut intervenir sur certaines tâches limitées
    }
}
