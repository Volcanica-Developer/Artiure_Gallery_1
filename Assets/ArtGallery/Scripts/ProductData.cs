using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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

