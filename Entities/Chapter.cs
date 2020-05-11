using System.Collections.Generic;

namespace EFCoreSQLiteFTS.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public ICollection<Chapter> Chapters { get; set; }
    }

    public class Chapter
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Text { get; set; }

        public User User { get; set; }
        public int UserId { get; set; }
    }
}