using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BlazorWasmClient.Components.Auth;
using BlazorWasmClient.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ProductService.Api.Queries.Dtos;

namespace BlazorWasmClient.Components.Products;

public class ProductsClient
{
    private readonly CustomAuthenticationStateProvider customAuthenticationStateProvider;
    private readonly HttpClient httpClient;
    private readonly string baseUrl;

    public ProductsClient
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

    public async Task<ApiResponse<IEnumerable<ProductDto>>> GetAllProducts()
    {
        try
        {
            var token = await customAuthenticationStateProvider.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            
            var response = await httpClient.GetAsync($"{baseUrl}/api/Products");
            if (response.IsSuccessStatusCode)
            {
                var productsString = await response.Content.ReadAsStringAsync();
                return ApiResponse<IEnumerable<ProductDto>>.Success
                (
                    JsonConvert.DeserializeObject<IEnumerable<ProductDto>>(productsString)
                );
            }
            return ApiResponse<IEnumerable<ProductDto>>.Error($"Lỗi khi tải danh sách sản phẩm ({response.StatusCode}). Vui lòng đăng nhập lại.");
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<ProductDto>>.Error($"Không thể tải sản phẩm: {ex.Message}");
        }
    }
    
    public async Task<ApiResponse<ProductDto>> GetProduct(string code)
    {
        try
        {
            var token = await customAuthenticationStateProvider.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            
            var response = await httpClient.GetAsync($"{baseUrl}/api/Products/{code}");
            if (response.IsSuccessStatusCode)
            {
                var productsString = await response.Content.ReadAsStringAsync();
                return ApiResponse<ProductDto>.Success
                (
                    JsonConvert.DeserializeObject<ProductDto>(productsString)
                );
            }
            return ApiResponse<ProductDto>.Error($"Lỗi khi tải chi tiết sản phẩm ({response.StatusCode}).");
        }
        catch (Exception ex)
        {
            return ApiResponse<ProductDto>.Error($"Không thể tải chi tiết sản phẩm: {ex.Message}");
        }
    }
}
