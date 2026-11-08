using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BlazorWasmClient.Components.Auth;
using BlazorWasmClient.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PolicySearchService.Api.Queries;

namespace BlazorWasmClient.Components.PolicySearch;

public class PolicySearchClient
{
    private readonly CustomAuthenticationStateProvider customAuthenticationStateProvider;
    private readonly HttpClient httpClient;
    private readonly string baseUrl;
    private const string And = "%20AND%20";

    public PolicySearchClient
    (
        AuthenticationStateProvider customAuthenticationStateProvider, 
        HttpClient httpClient,
        IConfiguration configuration
    )
    {
        this.customAuthenticationStateProvider = (CustomAuthenticationStateProvider)customAuthenticationStateProvider;
        this.httpClient = httpClient;
        this.baseUrl = configuration["Services:ApiGateway"];
    }

    public async Task<ApiResponse<FindPolicyResult>> SearchPolicies(string policyNumber, string policyHolder)
    {
        try
        {
            var token = await customAuthenticationStateProvider.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var queryString = string.Empty;
            if (!string.IsNullOrWhiteSpace(policyNumber))
            {
                queryString += policyNumber;
            }

            if (!string.IsNullOrWhiteSpace(policyHolder))
            {
                queryString += ((queryString.Length>0 ? And : string.Empty) + policyHolder);
            }
            
            var response = await httpClient.GetAsync($"{baseUrl}/api/PolicySearch?q={queryString}");
            if (response.IsSuccessStatusCode)
            {
                var productsString = await response.Content.ReadAsStringAsync();
                return ApiResponse<FindPolicyResult>.Success(JsonConvert.DeserializeObject<FindPolicyResult>(productsString));
            }
            return ApiResponse<FindPolicyResult>.Error($"Lỗi tìm kiếm: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return ApiResponse<FindPolicyResult>.Error(ex.Message);
        }
    }
}
