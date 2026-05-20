namespace ProjectTracker.Data.Constants
{
    public static class DataConstants
    {
        public static class Project
        {
            public const int MinNameLength = 3;
            public const int MaxNameLength = 100;
            public const int MaxDescriptionLength = 500;
        }

        public static class WorkItem
        {
            public const int MinTitleLength = 3;
            public const int MaxTitleLength = 200;
            public const int MaxDescriptionLength = 2000;
            public const int MinEstimatedHours = 1;
            public const int MaxEstimatedHours = 500;
        }

        public static class Comment
        {
            public const int MinContentLength = 1;
            public const int MaxContentLength = 1000;
        }

        public static class User
        {
            public const int MinFirstNameLength = 2;
            public const int MaxFirstNameLength = 50;
            public const int MinLastNameLength = 2;
            public const int MaxLastNameLength = 50;
            public const int MaxBioLength = 500;
            public const int MaxJobTitleLength = 100;
            public const int MaxDepartmentLength = 100;
        }

        public static class Pagination
        {
            public const int DefaultPageSize = 10;
            public const int MaxPageSize = 50;
        }
    }
}