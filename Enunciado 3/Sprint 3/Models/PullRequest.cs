using CsvHelper.Configuration.Attributes;

namespace Lab03S03.Models
{
    public class PullRequest
    {
        [Name("pr_id")]
        public string PrId { get; set; } = string.Empty;

        [Name("repository", "repo")]
        public string Repo { get; set; } = string.Empty;

        [Name("state", "status")]
        public string Status { get; set; } = string.Empty;

        [Name("changed_files", "files_changed")]
        public int FilesChanged { get; set; }

        [Name("additions", "lines_added")]
        public int LinesAdded { get; set; }

        [Name("deletions", "lines_removed")]
        public int LinesRemoved { get; set; }

        [Name("analysis_time_hours", "analysis_time_h")]
        public double AnalysisTimeH { get; set; }

        [Name("description_length", "body_length")]
        public int BodyLength { get; set; }

        [Name("participants_count", "participants")]
        public int Participants { get; set; }

        [Name("comments_count", "comments")]
        public int Comments { get; set; }

        [Name("reviews_count", "review_count")]
        public int ReviewCount { get; set; }

    }
}
