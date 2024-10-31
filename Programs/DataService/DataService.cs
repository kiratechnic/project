using GServer;
using GService;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;



namespace DataService
{
    internal class DataService
    {

        static RSA _publicKey = RSA.Create();
        static void ExportKeys(string publicKeyPath)
        {
            // Экспорт публичного ключа
            var publicKeyBytes = _publicKey.ExportRSAPublicKey();
            using (var fs = new FileStream(publicKeyPath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(publicKeyBytes, 0, publicKeyBytes.Length);
            }
            Console.WriteLine($"Публичный ключ экспортирован в {publicKeyPath}");
        }
        static void ImportKeys(string publicKeyPath)
        {
            // Импорт публичного ключа
            var publicKeyBytes = new byte[512];
            using (var fs = new FileStream(publicKeyPath, FileMode.Open, FileAccess.Read))
            {
                fs.Read(publicKeyBytes, 0, publicKeyBytes.Length);
                _publicKey.ImportRSAPublicKey(publicKeyBytes, out _);
                Console.WriteLine($"Публичный ключ импортирован из {publicKeyPath}");
            }
        }

        static void Main()
        {
            ImportKeys("../../../publicKey.pem");


            #region service create
            string hostname = "127.0.0.1";

            Service service = new Service("DataService", hostname, 8890);

            service.WebServer = new Server(hostname, 9000);

            service.WebServer._RouteManager.Add(Route.BuildDynamicRoute(GServer.HttpMethod.GET, new Regex("^/data/([a-zA-Z0-9_]{2,16})/read$"), data_read));
            service.WebServer._RouteManager.Add(Route.BuildDynamicRoute(GServer.HttpMethod.GET, new Regex("^/data/([a-zA-Z0-9_]{2,16})/create$"), data_create));
            service.RoutesRequired.Add("/user/token/key");

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
        static HttpResponse data_read(HttpRequest req)
        {


            return new HttpResponse(req, 200, null);
        }
        static HttpResponse data_create(HttpRequest req)
        {


            return new HttpResponse(req, 201, null);
        }
        static void user_token_key(HttpRequest req)
        {

        }
    }
}
