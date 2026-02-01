using FinancialManagerApp.Models.Revolut;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FinancialManagerApp.Services
{
    public class RevolutService
    {
        private const string SANDBOX_URL = "https://sandbox-b2b.revolut.com/api/1.0";
        private const string TOKEN_ENDPOINT = "https://sandbox-b2b.revolut.com/api/1.0/auth/token";

        // Klient HTTP do wysyłania zapytań
        private readonly HttpClient _httpClient;

        public RevolutService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<RevolutAccountDto>> GetAccountsAsync(string clientId, string privateKeyPem)
        {
            // 1. Zdobądź Access Token
            string accessToken = await AuthenticateAsync(clientId, privateKeyPem);

            // 2. Pobierz Konta
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"{SANDBOX_URL}/accounts");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<RevolutAccountDto>>(json);
        }

        public async Task<List<RevolutTransactionDto>> GetTransactionsForWalletAsync(
            string clientId,
            string privateKeyPem,
            string accountId,
            DateTime? fromDate = null)
        {
            string accessToken = await AuthenticateAsync(clientId, privateKeyPem);
            return await GetTransactionsAsync(accessToken, accountId, fromDate);
        }

        public async Task<List<RevolutTransactionDto>> GetTransactionsAsync(
            string accessToken,
            string accountId,
            DateTime? fromDate = null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            string from = fromDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddDays(-90).ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"{SANDBOX_URL}/transactions?from={from}&account={accountId}");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<RevolutTransactionDto>>(json);
        }

        // --- SEKCJA UWIERZYTELNIANIA (JWT) ---

        private async Task<string> AuthenticateAsync(string clientId, string privateKeyPem)
        {
            // A. Wygeneruj JWT (Client Assertion)
            // UWAGA: Tutaj musisz podać domenę, którą wpisałeś w Revolut jako Redirect URI!
            // Jeśli wpisałeś w Revolut "https://www.google.com", to tu wpisz "www.google.com".
            string redirectUriDomain = "www.google.com"; // <--- ZMIEŃ TO NA SWOJĄ DOMENĘ Z REVOLUT

            string clientAssertion = GenerateJwt(clientId, privateKeyPem, redirectUriDomain);

            // B. Wymień JWT na Access Token
            var requestBody = new List<KeyValuePair<string, string>>
            {
                // POPRAWKA: Revolut wymaga "client_credentials", a JWT podajemy jako client_assertion
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId), // Czasami wymagane redundatnie
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
                // To pozwoli nam zobaczyć dokładny błąd w oknie aplikacji
                throw new Exception($"Błąd autoryzacji Revolut ({response.StatusCode}): {responseContent}");
            }

            dynamic tokenResponse = JsonConvert.DeserializeObject(responseContent);
            return tokenResponse.access_token;
        }

        private string GenerateJwt(string clientId, string privateKeyPem, string issuerDomain)
        {
            var cleanKey = privateKeyPem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();

            using (var rsa = RSA.Create())
            {
                try
                {
                    rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(cleanKey), out _);
                }
                catch (FormatException)
                {
                    var pemBytes = Encoding.UTF8.GetBytes(privateKeyPem);
                    rsa.ImportFromPem(privateKeyPem);
                }

                var securityKey = new RsaSecurityKey(rsa);
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "iss", issuerDomain }, // POPRAWKA: To musi być domena z Redirect URI (np. www.google.com)
                    { "sub", clientId },     // Subject = Client ID
                    { "aud", "https://revolut.com" }, // POPRAWKA: Revolut oczekuje tego Audience
                    { "exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() },
                    { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                    { "jti", Guid.NewGuid().ToString() }
                };

                var secToken = new JwtSecurityToken(header, payload);
                return new JwtSecurityTokenHandler().WriteToken(secToken);
            }
        }
    }
}