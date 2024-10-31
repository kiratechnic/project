using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;


namespace UserService
{
    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Pass { get; set; }
    }

    public class File
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public int UserId { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<File> Files { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("user");
            modelBuilder.Entity<File>().ToTable("file");
        }
    }


    public static class DataService
    {
        // Добавление пользователя
        //public static int AddUser(AppDbContext context, string login, string pass)
        //{
        //    var user = new User { Login = login, Pass = pass };
        //    context.Users.Add(user);
        //    context.SaveChanges();
        //    return user.Id;
        //}
        public static int AddUser(AppDbContext context, string login, string pass)
        {
            // Проверка наличия пользователя с таким логином
            var existingUser = context.Users.FirstOrDefault(u => u.Login == login);
            if (existingUser != null)
            {
                // Пользователь с таким логином уже существует, возвращаем null или -1
                return -1; // Или можно вернуть -1 для обозначения ошибки
            }

            var user = new User { Login = login, Pass = pass };
            context.Users.Add(user);
            context.SaveChanges();
            return user.Id; // Возвращаем ID нового пользователя
        }

        // Добавление файла
        public static void AddFile(AppDbContext context, string login, string name, string path, bool isDirectory)
        {
            var user = context.Users.FirstOrDefault(u => u.Login == login);
            if (user != null)
            {
                var file = new File { Name = name, Path = path, IsDirectory = isDirectory, UserId = user.Id };
                context.Files.Add(file);
                context.SaveChanges();
            }
        }

        // Проверка логина и пароля
        public static int? ValidateUser(AppDbContext context, string login, string pass)
        {
            var user = context.Users.FirstOrDefault(u => u.Login == login && u.Pass == pass);
            return user?.Id;
        }

        // Вывод всех файлов пользователя
        public static string[] GetUserFiles(AppDbContext context, string login)
        {
            var user = context.Users.FirstOrDefault(u => u.Login == login);
            if (user != null)
            {
                return context.Files
                    .Where(f => f.UserId == user.Id)
                    .Select(f => f.Path + f.Name)
                    .ToArray();
            }
            return Array.Empty<string>();
        }
    }



    class Program2
    {
        static async Task Main(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost:5432;Database=test;Username=postgres;Password=postgres");

            using (var context = new AppDbContext(optionsBuilder.Options))
            {
                context.Database.EnsureCreated();

                // Пример добавления пользователя
                int userId = DataService.AddUser(context, "testuser", "password123");

                // Пример добавления файла
                DataService.AddFile(context, "testuser", "example.txt", "/files/", false);

                // Пример проверки логина и пароля
                int? validatedUserId = DataService.ValidateUser(context, "testuser", "password123");
                Console.WriteLine($"Validated User ID: {validatedUserId}");

                // Пример получения всех файлов пользователя
                var files = DataService.GetUserFiles(context, "testuser");
                Console.WriteLine("User Files:");
                foreach (var file in files)
                {
                    Console.WriteLine(file);
                }
            }
        }
    }

}
