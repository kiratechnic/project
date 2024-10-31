using GServer;
using GService;

using System;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;


using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UserService
{
    //
    //(https?:\/\/)?([a-zA-Z0-9\-]+\.[a-zA-Z]{2,})
    //(\/[^\s]*)?
    //(\?[^\s]*)?
    //
    class UserService
    {
        #region RSA
        static RSA _privateKey = RSA.Create();
        static RSA _publicKey = RSA.Create();
        static void ExportKeys(string privateKeyPath, string publicKeyPath)
        {
            // Экспорт приватного ключа
            var privateKeyBytes = _privateKey.ExportRSAPrivateKey();
            using (var fs = new FileStream(privateKeyPath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(privateKeyBytes, 0, privateKeyBytes.Length);
            }
            Console.WriteLine($"Приватный ключ экспортирован в {privateKeyPath}");

            // Экспорт публичного ключа
            var publicKeyBytes = _publicKey.ExportRSAPublicKey();
            using (var fs = new FileStream(publicKeyPath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(publicKeyBytes, 0, publicKeyBytes.Length);
            }
            Console.WriteLine($"Публичный ключ экспортирован в {publicKeyPath}");
        }
        static void ImportKeys(string privateKeyPath, string publicKeyPath)
        {
            // Импорт приватного ключа
            var privateKeyBytes = new byte[2048];
            using (var fs = new FileStream(privateKeyPath, FileMode.Open, FileAccess.Read))
            {
                fs.Read(privateKeyBytes, 0, privateKeyBytes.Length);
                _privateKey.ImportRSAPrivateKey(privateKeyBytes, out _);
                Console.WriteLine($"Приватный ключ импортирован из {privateKeyPath}");
            }

            // Импорт публичного ключа
            var publicKeyBytes = new byte[512];
            using (var fs = new FileStream(publicKeyPath, FileMode.Open, FileAccess.Read))
            {
                fs.Read(publicKeyBytes, 0, publicKeyBytes.Length);
                _publicKey.ImportRSAPublicKey(publicKeyBytes, out _);
                Console.WriteLine($"Публичный ключ импортирован из {publicKeyPath}");
            }
        }
        #endregion

        static string DBhostname = "localhost";
        static string DBport = ":5432";

        static AppDbContext context;
        static void Main()
        {
            #region context-Database

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql($"Host={DBhostname}{DBport};Database=test;Username=;Password=");


            context = new AppDbContext(optionsBuilder.Options);

            context.Database.EnsureCreated();

            #endregion

            #region RSA-Key-ganerete

            //_privateKey = RSA.Create(2048);
            //_publicKey = RSA.Create();
            //_publicKey.ImportRSAPublicKey(_privateKey.ExportRSAPublicKey(), out _);

            //ExportKeys("privateKey.pem", "publicKey.pem");
            ImportKeys("../../../privateKey.pem", "../../../publicKey.pem");

            #endregion

            #region service create
            string hostname = "127.0.0.1";

            Service service = new Service("UserService", hostname, 8890);

            //инициализация и запуск внешнего веб сервера
            service.WebServer = new Server(hostname, 9000);

            service.WebServer._RouteManager.Add(Route.BuildStaticRoute(HttpMethod.GET, "/", defaultMethod));

            service.WebServer._RouteManager.Add(Route.BuildParameterizedRoute(HttpMethod.GET, new Regex("/user/identification"), user_identification, new string[] { "login", "password" }));
            service.WebServer._RouteManager.Add(Route.BuildParameterizedRoute(HttpMethod.GET, new Regex("/user/registration"), user_registration, new string[] { "login", "password" }));
            service.WebServer._RouteManager.Add(Route.BuildStaticRoute(HttpMethod.POST, "/user/authentication", user_authentication));
            service.WebServer._RouteManager.Add(Route.BuildStaticRoute(HttpMethod.GET, "/user/token/key", user_token_key));

            service.WebServer._RouteManager.Add(Route.BuildDynamicRoute(GServer.HttpMethod.POST, new Regex("^/data/([a-zA-Z0-9_]{2,16})/read$"), data_read));
            service.WebServer._RouteManager.Add(Route.BuildDynamicRoute(GServer.HttpMethod.POST, new Regex("^/data/([a-zA-Z0-9_]{2,16})/create$"), data_create));


            service.UpdateListRoutesSupported();
            #endregion

            #region start web server

            CancellationTokenSource _TokenSource = new CancellationTokenSource();
            CancellationToken _Token = _TokenSource.Token;

            Console.WriteLine(Service.SerializeObject(service));

            service.WebServer.SatartServer(_Token);

            #endregion

            Console.ReadLine();
            _Token.ThrowIfCancellationRequested();
        }

        static HttpResponse defaultMethod(HttpRequest req)
        {
            string path = @"..\..\..\index.html"; // Замените на нужный путь

            try
            {
                string content = "";

                if (System.IO.File.Exists(path))
                {
                    content = System.IO.File.ReadAllText(path);

                    return new HttpResponse(req, 200, null, "text/html", Encoding.UTF8.GetBytes(content));
                }
                return new HttpResponse(req, 500, null);
            }
            catch (Exception ex)
            {
                return new HttpResponse(req, 500, null);
            }


        }


        #region handlers user service

        static HttpResponse user_registration(HttpRequest req)
        {
            try
            {
                if (req.QueryStringEntries.ContainsKey("login") && req.QueryStringEntries.ContainsKey("password"))
                {
                    if (DataService.AddUser(context, req.QueryStringEntries["login"], req.QueryStringEntries["password"]) != -1)
                        return new HttpResponse(req, 201, null);
                    return new HttpResponse(req, 201, null);
                }
                return new HttpResponse(req, 409, null);
            }
            catch (Exception)
            {
                return new HttpResponse(req, 400, null);
            }
        }
        static HttpResponse user_identification(HttpRequest req)
        {
            try
            {
                if (req.QueryStringEntries.ContainsKey("login") && req.QueryStringEntries.ContainsKey("password"))
                {
                    int? validatedUserId = DataService.ValidateUser(context, req.QueryStringEntries["login"], req.QueryStringEntries["password"]);

                    if (validatedUserId != null)
                    {
                        var newAccessToken = GenerateJwtToken(validatedUserId.ToString(), req.QueryStringEntries["login"], TimeSpan.FromMinutes(10), _privateKey);
                        var newRefreshToken = GenerateJwtToken(validatedUserId.ToString(), req.QueryStringEntries["login"], TimeSpan.FromDays(30), _privateKey);
                        return new HttpResponse(req, 200, null, req.ContentType, Encoding.UTF8.GetBytes(newAccessToken + ", " + newRefreshToken));
                    }
                }
                return new HttpResponse(req, 400, null);
            }
            catch (Exception)
            {
                return new HttpResponse(req, 400, null);
            }
        }
        static HttpResponse user_authentication(HttpRequest req)
        {
            try
            {
                if (req.Data != null)
                {
                    string refreshToken = Encoding.UTF8.GetString(req.Data.ToArray());

                    var handler = new JwtSecurityTokenHandler();
                    if (handler.CanReadToken(refreshToken))
                    {

                        if (ValidateAccessToken(refreshToken, _publicKey))
                        {
                            var jwtToken = handler.ReadJwtToken(refreshToken);

                            string guid = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId).Value.ToString();
                            string login = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub).Value.ToString();


                            var newAccessToken = GenerateJwtToken(guid, login, TimeSpan.FromMinutes(10), _privateKey);
                            var newRefreshToken = GenerateJwtToken(guid, login, TimeSpan.FromDays(30), _privateKey);
                            return new HttpResponse(req, 201, null, req.ContentType, Encoding.UTF8.GetBytes(newAccessToken + ", " + newRefreshToken));
                        }
                        return new HttpResponse(req, 401, null);
                    }
                }
            }
            catch (Exception)
            {
                return new HttpResponse(req, 400, null);
            }
            return new HttpResponse(req, 400, null);
        }
        private static string GenerateJwtToken(string userGuid, string username, TimeSpan expiration, RSA key)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.NameId, userGuid.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var credentials = new SigningCredentials(new RsaSecurityKey(key), SecurityAlgorithms.RsaSha256);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(expiration),
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public static bool ValidateAccessToken(string token, RSA _publicKey)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(_publicKey),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero // Убираем временной запас
                }, out SecurityToken validatedToken);

                return true; // Токен валиден
            }
            catch
            {
                return false; // Токен недействителен
            }

        }
        static HttpResponse user_token_key(HttpRequest req)
        {
            return new HttpResponse(req, 200, null, req.ContentType, _privateKey.ExportRSAPublicKey());
        }
        #endregion

        #region handlers data service

        static HttpResponse data_read(HttpRequest req)
        {
            if (req.QueryStringEntries != null
                && req.QueryStringEntries.ContainsKey("path") && req.QueryStringEntries["path"].Contains("../") == false
                && req.Data != null)
            {
                try
                {
                    string refreshToken = Encoding.UTF8.GetString(req.Data.ToArray());

                    var handler = new JwtSecurityTokenHandler();
                    if (ValidateAccessToken(refreshToken, _publicKey))
                    {
                        var jwtToken = handler.ReadJwtToken(refreshToken);

                        string login = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub).Value.ToString();

                        string path = @"" + login + @"\" + req.QueryStringEntries["path"];
                        Console.WriteLine(path);

                        if (System.IO.File.Exists(path))
                        {
                            return new HttpResponse(req, 200, null, req.ContentType, Encoding.UTF8.GetBytes(Path.GetFileName(path)));
                            //return new HttpResponse(req, 200, null, MimeTypes.GetFromExtension(Path.GetExtension(path)), System.IO.File.ReadAllBytes(path));
                        }
                        else if (System.IO.Directory.Exists(path))
                        {
                            string resp = $"{req.QueryStringEntries["path"]}\n";
                            string[] files = Directory.GetFiles(path);
                            string[] directories = Directory.GetDirectories(path);

                            foreach (var directory in directories)
                            {
                                resp += $"\t{Path.GetFileName(directory)}\n";
                            }
                            foreach (var file in files)
                            {
                                resp += $"\t{Path.GetFileName(file)}\n";
                            }
                            return new HttpResponse(req, 200, null, req.ContentType, Encoding.UTF8.GetBytes(resp));
                        }
                        return new HttpResponse(req, 400, null, req.ContentType, Encoding.UTF8.GetBytes("файл(дериктория) не найден(а)"));
                    }
                    return new HttpResponse(req, 403, null, req.ContentType, Encoding.UTF8.GetBytes("пользователь не прошел верификацию"));
                }
                catch (Exception)
                {
                    return new HttpResponse(req, 400, null, req.ContentType, Encoding.UTF8.GetBytes("ошибка"));
                }
            }
            else if (req.Data != null)
            {
                try
                {
                    string refreshToken = Encoding.UTF8.GetString(req.Data.ToArray());

                    var handler = new JwtSecurityTokenHandler();
                    if (ValidateAccessToken(refreshToken, _publicKey))
                    {
                        var jwtToken = handler.ReadJwtToken(refreshToken);

                        string login = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub).Value.ToString();

                        string path = @"" + login + @"\";
                        Console.WriteLine("\tclient`s path" + path);

                        if (System.IO.Directory.Exists(path))
                        {
                            string resp = PrintDirectoryTree(path);
                            return new HttpResponse(req, 200, null, req.ContentType, Encoding.UTF8.GetBytes(resp));
                        }
                        return new HttpResponse(req, 400, null, req.ContentType, Encoding.UTF8.GetBytes("файл(дериктория) не найден(а)"));
                    }
                    return new HttpResponse(req, 403, null, req.ContentType, Encoding.UTF8.GetBytes("пользователь не прошел верификацию"));
                }
                catch (Exception)
                {
                    return new HttpResponse(req, 400, null, req.ContentType, Encoding.UTF8.GetBytes("ошибка"));
                }
            }
            return new HttpResponse(req, 400, null, req.ContentType, Encoding.UTF8.GetBytes("400 Bad Reaqust"));
        }

        static HttpResponse data_create(HttpRequest req)
        {
            if (req.QueryStringEntries != null
                && req.QueryStringEntries.ContainsKey("isdirectory")
                && req.QueryStringEntries.ContainsKey("path") && req.QueryStringEntries["path"].Contains("../") == false
                && req.Data != null)
            {
                try
                {
                    string refreshToken = Encoding.UTF8.GetString(req.Data.ToArray());

                    var handler = new JwtSecurityTokenHandler();
                    if (ValidateAccessToken(refreshToken, _publicKey))
                    {
                        var jwtToken = handler.ReadJwtToken(refreshToken);

                        string login = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub).Value.ToString();

                        string path = @"" + login + @"\" + req.QueryStringEntries["path"];

                        try
                        {
                            if (!System.IO.Directory.Exists(path) || !System.IO.File.Exists(path))
                            {
                                if (req.QueryStringEntries["isdirectory"] == "true")
                                {
                                    System.IO.Directory.CreateDirectory(path);

                                    DataService.AddFile(context, login, System.IO.Path.GetFileName(path), path, true);

                                    return new HttpResponse(req, 201, null);
                                }
                                else if (req.QueryStringEntries["isdirectory"] == "false")
                                {
                                    using (var fileStream = System.IO.File.Create(path))
                                    {
                                        byte[] info = new UTF8Encoding(true).GetBytes("Начальное содержимое файла.");
                                        fileStream.Write(info, 0, info.Length);
                                    }

                                    DataService.AddFile(context, login, System.IO.Path.GetFileName(path), path, false);

                                    return new HttpResponse(req, 201, null);
                                }
                            }
                            return new HttpResponse(req, 400, null, req.ContentType, Encoding.UTF8.GetBytes("такой(ая) фаел(дериктория) уже существует"));
                        }
                        catch (Exception)
                        {
                            return new HttpResponse(req, 400, null, req.ContentType, Encoding.UTF8.GetBytes("ошибка при создании"));
                        }



                    }
                    return new HttpResponse(req, 403, null, req.ContentType, Encoding.UTF8.GetBytes("пользователь не прошел верификацию"));
                }
                catch (Exception ex)
                {
                    return new HttpResponse(req, 400, null, req.ContentType, Encoding.UTF8.GetBytes("ошибка"));
                }
            }
            return new HttpResponse(req, 400, null, req.ContentType, Encoding.UTF8.GetBytes("неверные параметры"));
        }



        static string PrintDirectoryTree(string path, string indent = "")
        {
            StringBuilder result = new StringBuilder();

            // Получаем все файлы в текущей директории
            var files = Directory.GetFiles(path);
            // Получаем все подкаталоги в текущей директории
            var directories = Directory.GetDirectories(path);

            // Сначала выводим файлы
            foreach (var file in files)
            {
                result.AppendLine($"{indent}├── {Path.GetFileName(file)}");
            }

            // Затем выводим директории
            foreach (var directory in directories)
            {
                result.AppendLine($"{indent}└── {Path.GetFileName(directory)}");
                // Рекурсивно вызываем метод для вывода содержимого поддиректории
                result.Append(PrintDirectoryTree(directory, indent + "    "));
            }

            return result.ToString();
        }


        #endregion
    }
}