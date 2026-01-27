using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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
