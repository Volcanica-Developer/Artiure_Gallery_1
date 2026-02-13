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

[Serializable]
public class SendPriceOnRequestRequest
{
    public string artistEmail;
    public string artistName;
    public string userEmail;
    public string artworkTitle;
    public string exhibitionName;
}

[Serializable]
public class SendPriceOnRequestResponse
{
    public bool res;
}

public class APIManager : MonoBehaviour
{
    private static string CartUrl => AppConfig.CartUrl;
    private static string FavouriteUrl => AppConfig.FavouriteUrl;
    private static string SendPriceOnRequestUrl => AppConfig.SendPriceOnRequestUrl;

    private void Start()
    {

    }

    #region Public button-friendly API

    // These four methods are intended to be wired directly to UI Buttons via the Inspector.
    // They currently forward to the existing "hardcoded" implementations, so you can
    // easily replace the builders later without changing the button wiring.

    private const string UserIdFallback = "776c6aea-2a1a-48f9-81b6-5c0bf6ae586f";

    /// <summary>
    /// User ID sent with cart/favourite requests. Uses exhibition ID from ArtworkManagerNew when available.
    /// </summary>
    private static string GetUserId()
    {
        var artworkManager = FindFirstObjectByType<ArtworkManagerNew>();
        if (artworkManager != null)
        {
            string id = artworkManager.GetExhibitionId();
            if (!string.IsNullOrEmpty(id))
                return id;
        }
        return UserIdFallback;
    }

    /// <summary>
    /// Adds the given painting to the cart using the selected size (dropdown index).
    /// On success, the callback receives true.
    /// </summary>
    /// <param name="selectedPriceIndex">Index into painting.price (e.g. from sizes dropdown).</param>
    public void AddToCart(PaintingConfigNew painting, int selectedPriceIndex, Action<bool> successCallback)
    {
        if (painting == null)
        {
            successCallback?.Invoke(false);
            return;
        }
        var requestData = BuildCartRequestFromPainting(painting, selectedPriceIndex);
        if (requestData == null)
        {
            Debug.LogWarning("APIManager: Could not build cart request (no price data).");
            successCallback?.Invoke(false);
            return;
        }
        string json = JsonUtility.ToJson(requestData);
        StartCoroutine(PostCart(json, successCallback));
    }

    /// <summary>
    /// Removes the given painting from the cart using the selected size (dropdown index).
    /// On success, the callback receives true.
    /// </summary>
    /// <param name="selectedPriceIndex">Index into painting.price (e.g. from sizes dropdown).</param>
    public void RemoveFromCart(PaintingConfigNew painting, int selectedPriceIndex, Action<bool> successCallback)
    {
        if (painting == null)
        {
            successCallback?.Invoke(false);
            return;
        }
        var requestData = BuildDeleteCartRequestFromPainting(painting, selectedPriceIndex);
        if (requestData == null)
        {
            successCallback?.Invoke(false);
            return;
        }
        string json = JsonUtility.ToJson(requestData);
        StartCoroutine(DeleteCartItem(json, successCallback));
    }

    /// <summary>
    /// Adds the given painting to favourites using the selected size (dropdown index).
    /// On success, the callback receives true.
    /// </summary>
    /// <param name="selectedPriceIndex">Index into painting.price (e.g. from sizes dropdown).</param>
    public void AddToFavourite(PaintingConfigNew painting, int selectedPriceIndex, Action<bool> successCallback)
    {
        if (painting == null)
        {
            successCallback?.Invoke(false);
            return;
        }
        var requestData = BuildFavouriteRequestFromPainting(painting, selectedPriceIndex);
        if (requestData == null)
        {
            successCallback?.Invoke(false);
            return;
        }
        string json = JsonUtility.ToJson(requestData);
        StartCoroutine(PostFavourite(json, successCallback));
    }

    /// <summary>
    /// Removes the given painting from favourites using the selected size (dropdown index).
    /// On success, the callback receives true.
    /// </summary>
    /// <param name="selectedPriceIndex">Index into painting.price (e.g. from sizes dropdown).</param>
    public void RemoveFromFavourite(PaintingConfigNew painting, int selectedPriceIndex, Action<bool> successCallback)
    {
        if (painting == null)
        {
            successCallback?.Invoke(false);
            return;
        }
        var requestData = BuildDeleteFavouriteRequestFromPainting(painting, selectedPriceIndex);
        if (requestData == null)
        {
            successCallback?.Invoke(false);
            return;
        }
        string json = JsonUtility.ToJson(requestData);
        StartCoroutine(DeleteFavourite(json, successCallback));
    }

    /// <summary>
    /// Sends a "price on request" inquiry for an artwork. Artist/user/exhibition data typically come from getExhibitionFromId.
    /// </summary>
    public void SendPriceOnRequest(string artistEmail, string artistName, string userEmail, string artworkTitle, string exhibitionName, Action<bool> successCallback)
    {
        var requestData = new SendPriceOnRequestRequest
        {
            artistEmail = artistEmail ?? string.Empty,
            artistName = artistName ?? string.Empty,
            userEmail = userEmail ?? string.Empty,
            artworkTitle = artworkTitle ?? string.Empty,
            exhibitionName = exhibitionName ?? string.Empty
        };
        string json = JsonUtility.ToJson(requestData);
        StartCoroutine(PostSendPriceOnRequest(json, successCallback));
    }

    private IEnumerator PostSendPriceOnRequest(string json, Action<bool> successCallback)
    {
        using (var request = new UnityWebRequest(SendPriceOnRequestUrl, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"Sending price-on-request (POST): {json}");
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"Price-on-request POST failed: {request.error}\n{request.downloadHandler.text}");
                successCallback?.Invoke(false);
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Price-on-request response: {responseText}");
                var response = JsonUtility.FromJson<SendPriceOnRequestResponse>(responseText);
                bool success = response != null && response.res;
                successCallback?.Invoke(success);
            }
        }
    }

    #endregion

    private AddToCartRequest BuildCartRequestFromPainting(PaintingConfigNew painting, int selectedPriceIndex)
    {
        var priceEntry = GetPriceEntryAt(painting, selectedPriceIndex);
        if (priceEntry == null) return null;

        var region = priceEntry.region;
        string imageId = painting.mainImage != null ? painting.mainImage.image_id : null;
        if (string.IsNullOrEmpty(imageId)) imageId = string.Empty;

        return new AddToCartRequest
        {
            //userId = GetUserId(),
            userId = "29c3f3de-9f7c-4272-93f9-a832fc02ebdd",
            item = new CartItem
            {
                id = painting._id ?? string.Empty,
                name = painting.name ?? string.Empty,
                price = priceEntry.finalPrice > 0 ? priceEntry.finalPrice.ToString() : priceEntry.price.ToString(),
                size = priceEntry.size ?? string.Empty,
                quantity = 1,
                currency = region != null ? region.currency : "INR",
                sku = priceEntry.sku ?? string.Empty,
                subCategory = painting.subCategory ?? string.Empty,
                paperType = string.Empty,
                isLimitedEdition = false,
                frames = new CartItemFrames { size = string.Empty, price = null },
                image = new CartItemImage { image_id = imageId },
                region = region != null
                    ? new CartItemRegion
                    {
                        id = region.id ?? string.Empty,
                        name = region.name ?? string.Empty,
                        currency = region.currency ?? string.Empty,
                        countryCode = region.countryCode ?? string.Empty
                    }
                    : new CartItemRegion { id = string.Empty, name = string.Empty, currency = "INR", countryCode = "IN" }
            }
        };
    }

    private DeleteCartItemRequest BuildDeleteCartRequestFromPainting(PaintingConfigNew painting, int selectedPriceIndex)
    {
        var priceEntry = GetPriceEntryAt(painting, selectedPriceIndex);
        if (priceEntry == null || string.IsNullOrEmpty(priceEntry.sku)) return null;
        return new DeleteCartItemRequest { userId = GetUserId(), sku = priceEntry.sku };
    }

    /// <summary>
    /// Returns the price entry at the given index (e.g. selected size). Clamps to valid range; returns null if no prices.
    /// </summary>
    private static PriceConfigNew GetPriceEntryAt(PaintingConfigNew painting, int selectedPriceIndex)
    {
        if (painting?.price == null || painting.price.Count == 0) return null;
        int index = selectedPriceIndex < 0 ? 0 : (selectedPriceIndex >= painting.price.Count ? painting.price.Count - 1 : selectedPriceIndex);
        return painting.price[index];
    }

    private IEnumerator PostCart(string json, Action<bool> successCallback)
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
                successCallback?.Invoke(false);
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Cart POST response: {responseText}");
                var response = JsonUtility.FromJson<AddToCartResponse>(responseText);
                bool success = response != null && response.success && response.cart != null;
                if (success)
                    Debug.Log($"Cart updated. Cart ID: {response.cart.id}, items count: {response.cart.items?.Length ?? 0}");
                else
                    Debug.LogWarning("Cart POST response deserialized but appears invalid.");
                successCallback?.Invoke(success);
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

    private FavouriteRequest BuildFavouriteRequestFromPainting(PaintingConfigNew painting, int selectedPriceIndex)
    {
        var priceEntry = GetPriceEntryAt(painting, selectedPriceIndex);
        if (priceEntry == null) return null;
        string imageId = painting.mainImage != null ? painting.mainImage.image_id : null;
        if (string.IsNullOrEmpty(imageId)) imageId = string.Empty;
        return new FavouriteRequest
        {
            userId = GetUserId(),
            item = new FavouriteItemRequest
            {
                name = painting.name ?? string.Empty,
                price = priceEntry.finalPrice > 0 ? priceEntry.finalPrice : priceEntry.price,
                size = priceEntry.size ?? string.Empty,
                quantity = 1,
                currency = priceEntry.region != null ? priceEntry.region.currency : "INR",
                sku = priceEntry.sku ?? string.Empty,
                slug = painting.slug ?? string.Empty,
                image = imageId
            }
        };
    }

    private DeleteCartItemRequest BuildDeleteFavouriteRequestFromPainting(PaintingConfigNew painting, int selectedPriceIndex)
    {
        var priceEntry = GetPriceEntryAt(painting, selectedPriceIndex);
        if (priceEntry == null || string.IsNullOrEmpty(priceEntry.sku)) return null;
        return new DeleteCartItemRequest { userId = GetUserId(), sku = priceEntry.sku };
    }

    private IEnumerator PostFavourite(string json, Action<bool> successCallback)
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
                successCallback?.Invoke(false);
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Favourite POST response: {responseText}");
                var response = JsonUtility.FromJson<FavouriteResponse>(responseText);
                bool success = response != null && response.success && response.favourite != null;
                if (success)
                    Debug.Log($"Favourite updated. Favourite ID: {response.favourite.id}, items count: {response.favourite.items?.Length ?? 0}");
                else
                    Debug.LogWarning("Favourite POST response deserialized but appears invalid.");
                successCallback?.Invoke(success);
            }
        }
    }

    private IEnumerator DeleteFavourite(string json, Action<bool> successCallback)
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
                successCallback?.Invoke(false);
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Favourite DELETE response: {responseText}");
                var response = JsonUtility.FromJson<FavouriteResponse>(responseText);
                bool success = response != null && response.success && response.favourite != null;
                if (success)
                    Debug.Log($"Favourite item removed. Message: {response.message}, remaining favourite items: {response.favourite.items?.Length ?? 0}");
                else
                    Debug.LogWarning("Favourite DELETE response deserialized but appears invalid.");
                successCallback?.Invoke(success);
            }
        }
    }

    #endregion

    private IEnumerator DeleteCartItem(string json, Action<bool> successCallback)
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
                successCallback?.Invoke(false);
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Cart DELETE response: {responseText}");
                var response = JsonUtility.FromJson<DeleteCartItemResponse>(responseText);
                bool success = response != null && response.success && response.cart != null;
                if (success)
                    Debug.Log($"Item removed. Message: {response.message}, remaining items: {response.cart.items?.Length ?? 0}");
                else
                    Debug.LogWarning("Cart DELETE response deserialized but appears invalid.");
                successCallback?.Invoke(success);
            }
        }
    }
}
