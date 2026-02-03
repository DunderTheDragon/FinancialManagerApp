using FinancialManagerApp.Models.Revolut;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FinancialManagerApp.Services
{
    public class RevolutService
    {
        // Sandbox: https://sandbox-b2b.revolut.com/api/1.0
        // Production: https://b2b.revolut.com/api/1.0
        private const string API_URL = "https://sandbox-b2b.revolut.com/api/1.0";
        private const string TOKEN_ENDPOINT = "https://sandbox-b2b.revolut.com/api/1.0/auth/token";

        private const string JWT_AUDIENCE = "https://revolut.com";

        // Twoja domena podana w panelu Revolut (Redirect URI)
        private const string JWT_ISSUER = "www.google.com";

        private readonly HttpClient _httpClient;

        public RevolutService()
        {
            _httpClient = new HttpClient();
        }


        // Dodaj tę metodę do klasy RevolutService
        public async Task<string> ExchangeAuthCodeForRefreshToken(string authCode, string clientId, string privateKeyPem)
        {
            // 1. Generujemy JWT (tak samo jak przy pobieraniu tokena dostępu)
            string clientAssertion = GenerateJwt(clientId, privateKeyPem);

            // 2. Budujemy zapytanie o wymianę kodu na tokeny
            // WAŻNE: Dodajemy redirect_uri, który musi być identyczny z tym użytym w żądaniu autoryzacji
            const string redirectUri = "https://www.google.com/";
            var requestBody = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authCode),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
                new KeyValuePair<string, string>("client_assertion", clientAssertion)
            };

            var request = new HttpRequestMessage(HttpMethod.Post, TOKEN_ENDPOINT)
            {
                Content = new FormUrlEncodedContent(requestBody)
            };

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Logowanie dla debugowania
            System.Diagnostics.Debug.WriteLine($"Token Exchange Response Status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"Token Exchange Response: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                // Próbujemy sparsować błąd jako JSON dla lepszego komunikatu
                try
                {
                    dynamic errorResponse = JsonConvert.DeserializeObject(responseContent);
                    string errorMessage = errorResponse?.error_description ?? errorResponse?.message ?? errorResponse?.error ?? responseContent;
                    throw new Exception($"Błąd wymiany kodu na token: {errorMessage}");
                }
                catch
                {
                    throw new Exception($"Błąd wymiany kodu na token: {responseContent}");
                }
            }

            dynamic tokenResponse = JsonConvert.DeserializeObject(responseContent);

            // Zwracamy Refresh Token, który zapiszesz w bazie
            return tokenResponse.refresh_token;
        }

        public async Task<List<RevolutAccountDto>> GetAccountsAsync(string clientId, string privateKeyPem, string refreshToken)
        {
            string accessToken = await AuthenticateAsync(clientId, privateKeyPem, refreshToken);

            var request = new HttpRequestMessage(HttpMethod.Get, $"{API_URL}/accounts");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<RevolutAccountDto>>(json);
        }

        public async Task<List<RevolutTransactionDto>> GetTransactionsForWalletAsync(
            string clientId, string privateKeyPem, string refreshToken, string accountId, DateTime? fromDate = null)
        {
            string accessToken = await AuthenticateAsync(clientId, privateKeyPem, refreshToken);

            string from = fromDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
            // Dodajemy count=1000, aby pobrać więcej niż domyślne 100
            string url = $"{API_URL}/transactions?account={accountId}&from={from}&count=1000";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<RevolutTransactionDto>>(json);
        }

        private async Task<string> AuthenticateAsync(string clientId, string privateKeyPem, string refreshToken)
        {
            // 1. Generujemy JWT (Client Assertion)
            string clientAssertion = GenerateJwt(clientId, privateKeyPem);

            // 2. Budujemy zapytanie o Access Token używając Refresh Tokena
            var requestBody = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
                new KeyValuePair<string, string>("client_assertion", clientAssertion)
            };

            var request = new HttpRequestMessage(HttpMethod.Post, TOKEN_ENDPOINT)
            {
                Content = new FormUrlEncodedContent(requestBody)
            };

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Błąd autoryzacji Revolut ({response.StatusCode}): {responseContent}");
            }

            dynamic tokenResponse = JsonConvert.DeserializeObject(responseContent);
            return tokenResponse.access_token;
        }

        private string GenerateJwt(string clientId, string privateKeyPem)
        {
            // Czyszczenie klucza z nagłówków PEM, jeśli user je wkleił
            var cleanKey = privateKeyPem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();

            RSA rsa = RSA.Create();
            try
            {
                rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(cleanKey), out _);
                
                // WAŻNE: RsaSecurityKey przejmuje własność RSA, więc nie używamy using
                // RsaSecurityKey będzie odpowiedzialny za dispose RSA
                var securityKey = new RsaSecurityKey(rsa);
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "iss", JWT_ISSUER },
                    { "sub", clientId },
                    { "aud", JWT_AUDIENCE },
                    { "exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() }, // Krótki czas życia JWT
                    { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                    { "jti", Guid.NewGuid().ToString() }
                };

                var secToken = new JwtSecurityToken(header, payload);
                string token = new JwtSecurityTokenHandler().WriteToken(secToken);
                
                // RsaSecurityKey przejmuje własność, więc nie usuwamy RSA tutaj
                return token;
            }
            catch (Exception ex) when (!(ex is FormatException))
            {
                // Jeśli to nie błąd formatu, zwalniamy zasoby
                rsa?.Dispose();
                throw new Exception("Nieprawidłowy format klucza prywatnego. Upewnij się, że to format PKCS#8 Base64.", ex);
            }
            catch
            {
                rsa?.Dispose();
                throw new Exception("Nieprawidłowy format klucza prywatnego. Upewnij się, że to format PKCS#8 Base64.");
            }
        }
    }
}