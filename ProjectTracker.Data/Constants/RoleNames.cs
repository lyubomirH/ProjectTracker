namespace ProjectTracker.Data.Constants
{
    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string ProjectManager = "ProjectManager";
        public const string Developer = "Developer";
        public const string Tester = "Tester";
        public const string Viewer = "Viewer";

        public static readonly string[] AllRoles =
        {
            Admin,
            ProjectManager,
            Developer,
            Tester,
            Viewer
        };

        public static readonly string[] ManagementRoles =
        {
            Admin,
            ProjectManager
        };

        public static readonly string[] TeamRoles =
        {
            Developer,
            Tester,
            Viewer
        };
    }
}