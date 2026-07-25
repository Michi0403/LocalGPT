//-----------------------------------------------------------------------
// <copyright file="HttpExtensions.cs" company="https://github.com/Michi0403/TacosPortalOpen as love for blazor WASM and monolithes">
//     Author: Michael Fleischer
//     Copyright (c) https://github.com/Michi0403/TacosPortalOpen as love for blazor WASM and monolithes. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using LocalGPT.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TacosCore.Extensions
{
    public static class HttpExtensions
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            PropertyNamingPolicy = null,
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false,
            IncludeFields = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() },
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString

        };

        public static void CreateWASMClient(this IHttpClientFactory clientFactory, NavigationManager navigationManager, ILogger logger,
           out HttpClient? httpClient)
        {
            try
            {
                httpClient = clientFactory.CreateClient("WasmClient");

                if (httpClient.BaseAddress == null)
                {
                    httpClient.BaseAddress = new Uri(navigationManager.BaseUri);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateClient {ex.ToString()}");
                httpClient = null;
            }
        }
    }
}