namespace ProjetDotNet.Models.ViewModels
{
    public record DashboardViewModel
    {
        // pour tous
        public int TotalUsers { get; init; }
        public int TotalProjects { get; init; }
        public int TotalTasks { get; init; }
        public int TotalTeams { get; init; }

        // pour utilisateur courant
        public int MyProjects { get; init; }
        public int MyTasks { get; init; }
        public int MyTeams { get; init; }

        // indicateur de portée
        public bool IsSiteAdmin { get; init; }
    }
}