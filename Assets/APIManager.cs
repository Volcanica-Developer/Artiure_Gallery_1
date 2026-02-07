using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class CartItemRegion
{
    public string id;
    public string name;
    public string currency;
    public string countryCode;
}

[Serializable]
public class CartItemImage
{
    public string image_id;
}

[Serializable]
public class CartItemFrames
{
    public string size;
    public string price; // null in JSON will map to null here
}

[Serializable]
public class CartItem
{
    public string id;
    public string name;
    public string price;
    public string size;
    public int quantity;
    public string currency;
    public string sku;
    public string subCategory;
    public string paperType;
    public bool isLimitedEdition;
    public CartItemFrames frames;
    public CartItemImage image;
    public CartItemRegion region;
}

[Serializable]
public class AddToCartRequest
{
    public string userId;
    public CartItem item;
}

[Serializable]
public class DeleteCartItemRequest
{
    public string userId;
    public string sku;
}

[Serializable]
public class Cart
{
    public string _id;
    public string id;
    public string userId;
    public CartItem[] items;
    public string createdAt;
    public string updatedAt;
    public int __v;
}

[Serializable]
public class AddToCartResponse
{
    public bool success;
    public Cart cart;
}

[Serializable]
public class DeleteCartItemResponse
{
    public bool success;
    public string message;
    public Cart cart;
}

public class APIManager : MonoBehaviour
{
    private const string CartUrl = "https://stg.artiure.com/api/user/cart";
    private const string FavouriteUrl = "https://stg.artiure.com/api/user/favourite";

    [SerializeField] private Button favoriteBtn;
    [SerializeField] private Button cartBtn;

    private void Start()
    {

    }

    #region Public button-friendly API

    // These four methods are intended to be wired directly to UI Buttons via the Inspector.
    // They currently forward to the existing "hardcoded" implementations, so you can
    // easily replace the builders later without changing the button wiring.

    /// <summary>
    /// Adds the configured item to the cart. Wire this to an "Add to Cart" button.
    /// </summary>
    public void AddToCart()
    {
        AddHardcodedItemToCart();
    }

    /// <summary>
    /// Removes the configured item from the cart. Wire this to a "Remove from Cart" button.
    /// </summary>
    public void RemoveFromCart()
    {
        RemoveHardcodedItemFromCart();
    }

    /// <summary>
    /// Adds the configured item to favourites. Wire this to an "Add to Favourite" button.
    /// </summary>
    public void AddToFavourite()
    {
        AddHardcodedItemToFavourite();
    }

    /// <summary>
    /// Removes the configured item from favourites. Wire this to a "Remove from Favourite" button.
    /// </summary>
    public void RemoveFromFavourite()
    {
        RemoveHardcodedItemFromFavourite();
    }

    #endregion

    // Call this from another script, a UI Button, or manually in the Inspector
    public void AddHardcodedItemToCart()
    {
        var requestData = BuildHardcodedRequest();
        string json = JsonUtility.ToJson(requestData);
        StartCoroutine(PostCart(json));
    }

    private AddToCartRequest BuildHardcodedRequest()
    {
        return new AddToCartRequest
        {
            userId = "776c6aea-2a1a-48f9-81b6-5c0bf6ae586f",
            item = new CartItem
            {
                id = "688687880ecb6eeca432f62e",
                name = "Opera vs. Jazz?",
                price = "2135.11",
                size = "12x18",
                quantity = 1,
                currency = "INR",
                sku = "1753635420502-POSTERS-undefined-12x18",
                subCategory = "Posters",
                paperType = string.Empty,
                isLimitedEdition = false,
                frames = new CartItemFrames
                {
                    size = "NaNxNaN (inches)",
                    price = null
                },
                image = new CartItemImage
                {
                    image_id = "1753635420502-PS_1"
                },
                region = new CartItemRegion
                {
                    id = "67ef7e0dcdce447e3e183ad4",
                    name = "India",
                    currency = "INR",
                    countryCode = "IN"
                }
            }
        };
    }

    private IEnumerator PostCart(string json)
    {
        using (var request = new UnityWebRequest(CartUrl, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"Sending cart request (POST): {json}");
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"Cart POST request failed: {request.error}\\n{request.downloadHandler.text}");
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Cart POST response: {responseText}");

                var response = JsonUtility.FromJson<AddToCartResponse>(responseText);
                if (response != null && response.success && response.cart != null && response.cart.items != null)
                {
                    Debug.Log($"Cart updated. Cart ID: {response.cart.id}, items count: {response.cart.items.Length}");
                }
                else
                {
                    Debug.LogWarning("Cart POST response deserialized but appears invalid.");
                }
            }
        }
    }

    #region Favourite API

    [Serializable]
    public class FavouriteItemRequest
    {
        public string name;
        public double price;
        public string size;
        public int quantity;
        public string currency;
        public string sku;
        public string slug;
        public string image;
    }

    [Serializable]
    public class FavouriteRequest
    {
        public string userId;
        public FavouriteItemRequest item;
    }

    [Serializable]
    public class FavouriteItemResponse
    {
        public string name;
        public double price;
        public string size;
        public int quantity;
        public string currency;
        public string sku;
        public string slug;
        public string image;
    }

    [Serializable]
    public class FavouriteData
    {
        public string _id;
        public string id;
        public string userId;
        public FavouriteItemResponse[] items;
        public string createdAt;
        public string updatedAt;
        public int __v;
    }

    [Serializable]
    public class FavouriteResponse
    {
        public bool success;
        public string message;
        public FavouriteData favourite;
    }

    // Call this from another script, a UI Button, or manually in the Inspector
    public void AddHardcodedItemToFavourite()
    {
        var requestData = BuildHardcodedFavouriteRequest();
        string json = JsonUtility.ToJson(requestData);
        StartCoroutine(PostFavourite(json));
    }

    private FavouriteRequest BuildHardcodedFavouriteRequest()
    {
        return new FavouriteRequest
        {
            userId = "776c6aea-2a1a-48f9-81b6-5c0bf6ae586f",
            item = new FavouriteItemRequest
            {
                name = "Opera vs. Jazz?",
                price = 2135.11,
                size = "12x18",
                quantity = 1,
                currency = "INR",
                sku = "1753635420502-POSTERS-undefined-12x18",
                slug = "opera-vs-jazz",
                image = "1753635420502-PS_1"
            }
        };
    }

    private IEnumerator PostFavourite(string json)
    {
        using (var request = new UnityWebRequest(FavouriteUrl, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"Sending favourite request (POST): {json}");
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"Favourite POST request failed: {request.error}\\n{request.downloadHandler.text}");
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Favourite POST response: {responseText}");

                var response = JsonUtility.FromJson<FavouriteResponse>(responseText);
                if (response != null && response.success && response.favourite != null && response.favourite.items != null)
                {
                    Debug.Log($"Favourite updated. Favourite ID: {response.favourite.id}, items count: {response.favourite.items.Length}");
                }
                else
                {
                    Debug.LogWarning("Favourite POST response deserialized but appears invalid.");
                }
            }
        }
    }

    public void RemoveHardcodedItemFromFavourite()
    {
        var requestData = new DeleteCartItemRequest
        {
            userId = "776c6aea-2a1a-48f9-81b6-5c0bf6ae586f",
            sku = "1753635420502-POSTERS-undefined-12x18"
        };

        string json = JsonUtility.ToJson(requestData);
        StartCoroutine(DeleteFavourite(json));
    }

    private IEnumerator DeleteFavourite(string json)
    {
        using (var request = new UnityWebRequest(FavouriteUrl, UnityWebRequest.kHttpVerbDELETE))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"Sending favourite request (DELETE): {json}");
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"Favourite DELETE request failed: {request.error}\\n{request.downloadHandler.text}");
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Favourite DELETE response: {responseText}");

                var response = JsonUtility.FromJson<FavouriteResponse>(responseText);
                if (response != null && response.success && response.favourite != null && response.favourite.items != null)
                {
                    Debug.Log($"Favourite item removed. Message: {response.message}, remaining favourite items: {response.favourite.items.Length}");
                }
                else
                {
                    Debug.LogWarning("Favourite DELETE response deserialized but appears invalid.");
                }
            }
        }
    }

    #endregion

    // Call this from UI / other scripts to remove the hard-coded item from the cart
    public void RemoveHardcodedItemFromCart()
    {
        var requestData = new DeleteCartItemRequest
        {
            userId = "776c6aea-2a1a-48f9-81b6-5c0bf6ae586f",
            sku = "1753635420502-POSTERS-undefined-12x18"
        };

        string json = JsonUtility.ToJson(requestData);
        StartCoroutine(DeleteCartItem(json));
    }

    private IEnumerator DeleteCartItem(string json)
    {
        // UnityWebRequest supports a body with custom verb, so we use DELETE and send JSON
        using (var request = new UnityWebRequest(CartUrl, UnityWebRequest.kHttpVerbDELETE))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"Sending cart request (DELETE): {json}");
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"Cart DELETE request failed: {request.error}\\n{request.downloadHandler.text}");
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Cart DELETE response: {responseText}");

                var response = JsonUtility.FromJson<DeleteCartItemResponse>(responseText);
                if (response != null && response.success && response.cart != null && response.cart.items != null)
                {
                    Debug.Log($"Item removed. Message: {response.message}, remaining items: {response.cart.items.Length}");
                }
                else
                {
                    Debug.LogWarning("Cart DELETE response deserialized but appears invalid.");
                }
            }
        }
    }
}
