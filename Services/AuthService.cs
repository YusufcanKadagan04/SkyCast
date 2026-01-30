using System.Linq;
using SkyCast.Data;

namespace SkyCast
{
    public class AuthService
    {
        public string RegisterUser(string username, string password)
        {
            using (var db = new AppDbContext())
            {
                if (db.Users.Any(u => u.Username == username))
                {
                    return "Username already exists.";
                }

                var newUser = new User
                {
                    Username = username,
                    PasswordHash = SecurityHelper.HashPassword(password),
                    DefaultCity = "Istanbul",
                    IsMetric = true
                };

                db.Users.Add(newUser);
                db.SaveChanges();
                return "Success";
            }
        }

        public User LoginUser(string username, string password)
        {
            using (var db = new AppDbContext())
            {
                string hashedPassword = SecurityHelper.HashPassword(password);

                var user = db.Users.FirstOrDefault(u => u.Username == username && u.PasswordHash == hashedPassword);
                return user;
            }
        }
    }
}