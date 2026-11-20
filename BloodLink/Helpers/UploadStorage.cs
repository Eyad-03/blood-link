namespace BloodLink.Helpers
{
    // Where uploaded files are saved: <project>/App_Data/uploads/...
    // App_Data is NOT inside wwwroot, so nobody can open the files with a link.
    // Only the admin API (GET /api/admin/documents/{id}) sends them back.
    public static class UploadStorage
    {
        public const long MaxBytes = 10 * 1024 * 1024;   // 10 MB

        // Allowed file types and the content type the browser needs to open them.
        public static readonly Dictionary<string, string> AllowedTypes = new()
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

        public static string Root(IWebHostEnvironment env)
        {
            return Path.Combine(env.ContentRootPath, "App_Data", "uploads");
        }

        // "org-5/doc-12.pdf" → full path on the disk
        public static string FullPath(IWebHostEnvironment env, string relativePath)
        {
            return Path.Combine(Root(env), relativePath);
        }

        // 1536000 → "1.5 MB"
        public static string SizeText(long? bytes)
        {
            if (bytes == null) return "";
            if (bytes < 1024 * 1024) return Math.Max(1, bytes.Value / 1024) + " KB";
            return Math.Round(bytes.Value / 1024.0 / 1024.0, 1) + " MB";
        }
    }
}
