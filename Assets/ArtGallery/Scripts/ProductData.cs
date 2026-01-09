using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Root class for products JSON array
/// </summary>
[Serializable]
public class ProductsData
{
    [JsonProperty("products")]
    public List<ProductData> products;
}

/// <summary>
/// Parsed representation of a size string like "12x18".
/// </summary>
[Serializable]
public class ProductSize
{
    public float width;   // X or length component
    public float height;  // Y component
    public string raw;    // Original string (e.g. "12x18")
}

/// <summary>
/// Represents a single product from the products.json file
/// </summary>
[Serializable]
public class ProductData
{
    [JsonProperty("_id")]
    public string id;
    
    [JsonProperty("image_number")]
    public long imageNumber;
    
    [JsonProperty("mainImage")]
    public MainImageData mainImage;
    
    [JsonProperty("name")]
    public string name;
    
    [JsonProperty("shortDescription")]
    public string shortDescription;
    
    [JsonProperty("description")]
    public string description;
    
    [JsonProperty("category")]
    public string category;
    
    [JsonProperty("subCategory")]
    public string subCategory;
    
    [JsonProperty("medium")]
    public string medium;
    
    [JsonProperty("ratio")]
    public string ratio;
    
    [JsonProperty("slug")]
    public string slug;

    /// <summary>
    /// Raw available size strings from JSON, e.g. ["12x9", "16x12"].
    /// </summary>
    [JsonProperty("availableSizes")]
    public List<string> availableSizes;

    /// <summary>
    /// Parsed numeric sizes derived from availableSizes.
    /// Not serialized back to JSON.
    /// </summary>
    [JsonIgnore]
    public List<ProductSize> parsedAvailableSizes = new List<ProductSize>();

    /// <summary>
    /// Fills parsedAvailableSizes by splitting each size string on 'x' and parsing to floats.
    /// </summary>
    public void ParseAvailableSizes()
    {
        parsedAvailableSizes.Clear();

        if (availableSizes == null || availableSizes.Count == 0)
            return;

        foreach (var sizeStr in availableSizes)
        {
            if (string.IsNullOrWhiteSpace(sizeStr))
                continue;

            var lower = sizeStr.ToLowerInvariant();
            var parts = lower.Split('x');
            if (parts.Length != 2)
                continue;

            if (float.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var w) &&
                float.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var h))
            {
                parsedAvailableSizes.Add(new ProductSize
                {
                    width = w,
                    height = h,
                    raw = sizeStr
                });
            }
        }
    }
}

/// <summary>
/// Represents the main image data structure
/// </summary>
[Serializable]
public class MainImageData
{
    [JsonProperty("image_id")]
    public string imageId;
    
    [JsonProperty("src")]
    public string src; // This is the image URL
    
    [JsonProperty("alt")]
    public string alt;
    
    [JsonProperty("key")]
    public string key;
    
    [JsonProperty("_id")]
    public string id;
}



